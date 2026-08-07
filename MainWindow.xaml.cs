using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Forms = System.Windows.Forms;

namespace ToolBox;

public partial class MainWindow : Window
{
    readonly string _dataDir;
    List<ToolRecord> _tools = new();

    public MainWindow()
    {
        InitializeComponent();
        _dataDir = Storage.DefaultDir;
        _tools = Storage.Load(_dataDir, out var warning);
        if (warning != null)
            MessageBox.Show(this, warning + "\n\n数据目录：" + _dataDir, "数据文件提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        RefreshList();
    }

    // ---------- 列表与搜索 ----------

    void RefreshList()
    {
        string q = SearchBox.Text;
        CardList.ItemsSource = _tools.Where(t => Core.SearchMatches(t, q)).ToList();
        bool hasVisible = CardList.Items.Count > 0;
        EmptyText.Text = _tools.Count > 0
            ? "没有找到匹配的工具"
            : "还没有工具，点击右下角的 + 添加第一个程序";
        EmptyText.Visibility = hasVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = SearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.Visibility = SearchBox.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        RefreshList();
    }

    void Clear_Click(object sender, RoutedEventArgs e) => SearchBox.Clear();

    // ---------- 持久化 ----------

    void Save()
    {
        try
        {
            Storage.Save(_dataDir, _tools);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"保存失败：{ex.Message}\n\n更改仅在本次运行中保留。请检查 {_dataDir} 的写入权限。",
                "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- 添加 / 编辑 / 移除 ----------

    void Add_Click(object sender, RoutedEventArgs e)
    {
        using var ofd = new Forms.OpenFileDialog
        {
            Title = "选择要添加的程序",
            Filter = "程序 (*.exe)|*.exe",
            CheckFileExists = true,
        };
        if (ofd.ShowDialog(new Win32Window(this)) != Forms.DialogResult.OK) return;

        string path = Path.GetFullPath(ofd.FileName);
        var existing = _tools.FirstOrDefault(t => Core.IsSamePath(t.ExecutablePath, path));
        if (existing != null)
        {
            MessageBox.Show(this, $"“{existing.Name}”已使用该路径，正在打开它的编辑窗口。",
                "重复路径", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowEditor(existing, false);
            return;
        }

        var rec = Core.ReadFromExe(path);
        if (ShowEditor(rec, true) != null)
        {
            _tools.Add(rec);
            Save();
            RefreshList();
        }
    }

    ToolRecord? ShowEditor(ToolRecord record, bool isNew)
    {
        var dlg = new EditWindow(record, _tools, isNew) { Owner = this };
        return dlg.ShowDialog() == true ? record : null;
    }

    void EditRecord(ToolRecord r)
    {
        int i = _tools.IndexOf(r);
        if (ShowEditor(r, false) != null)
        {
            if (i >= 0) _tools[i] = r; // 替换以刷新卡片绑定
            Save();
            RefreshList();
        }
    }

    void RemoveRecord(ToolRecord r)
    {
        var res = MessageBox.Show(this,
            $"确定要从 ToolBox 移除“{r.Name}”吗？\n\n这只会删除目录记录，不会删除磁盘上的程序文件。",
            "移除工具", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;
        _tools.Remove(r);
        Save();
        RefreshList();
    }

    // ---------- 启动 ----------

    void Launch(ToolRecord r)
    {
        if (!File.Exists(r.ExecutablePath))
        {
            Relocate(r);
            return;
        }
        try
        {
            string workDir = string.IsNullOrWhiteSpace(r.WorkingDirectory) || !Directory.Exists(r.WorkingDirectory)
                ? Path.GetDirectoryName(r.ExecutablePath) ?? ""
                : r.WorkingDirectory;
            var psi = new ProcessStartInfo
            {
                FileName = r.ExecutablePath,
                WorkingDirectory = workDir,
                Arguments = r.Arguments,
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"启动失败：{ex.Message}\n\n请检查程序路径、工作目录或参数。",
                "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void Relocate(ToolRecord r)
    {
        var dlg = new MissingPathWindow(r.ExecutablePath) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        using var ofd = new Forms.OpenFileDialog
        {
            Title = "重新定位程序",
            Filter = "程序 (*.exe)|*.exe",
            CheckFileExists = true,
            FileName = r.ExecutablePath,
        };
        if (ofd.ShowDialog(new Win32Window(this)) != Forms.DialogResult.OK) return;

        r.ExecutablePath = Path.GetFullPath(ofd.FileName);
        if (string.IsNullOrWhiteSpace(r.WorkingDirectory) || !Directory.Exists(r.WorkingDirectory))
            r.WorkingDirectory = Path.GetDirectoryName(r.ExecutablePath) ?? "";
        Save();
        RefreshList();
    }

    void OpenContainingDir(ToolRecord r)
    {
        try
        {
            string exe = r.ExecutablePath;
            string? dir = Path.GetDirectoryName(exe);
            if (!File.Exists(exe) && (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)))
            {
                MessageBox.Show(this, "程序路径已失效，无法打开所在目录。", "打开目录失败",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string args = File.Exists(exe) ? $"/select,\"{exe}\"" : $"\"{dir}\"";
            Process.Start(new ProcessStartInfo("explorer.exe", args) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "无法打开目录：" + ex.Message, "打开目录失败",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- 卡片事件 ----------

    void Card_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ToolRecord r) Launch(r);
    }

    void More_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        b.ContextMenu!.PlacementTarget = b;
        b.ContextMenu.IsOpen = true;
    }

    void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ToolRecord r) EditRecord(r);
    }

    void OpenDir_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ToolRecord r) OpenContainingDir(r);
    }

    void Remove_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ToolRecord r) RemoveRecord(r);
    }

    // ---------- 标题栏 ----------

    void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    void Close_Click(object sender, MouseButtonEventArgs e) => Close();
    void Minimize_Click(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;
    void Maximize_Click(object sender, MouseButtonEventArgs e) => ToggleMaximize();
    void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    // 窗口整体圆角：用窗口区域裁剪（Win10/11 均生效）
    [DllImport("gdi32.dll")]
    static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

    [DllImport("user32.dll")]
    static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    void ApplyRoundedRegion()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        if (WindowState == WindowState.Maximized)
        {
            SetWindowRgn(hwnd, IntPtr.Zero, true);
            return;
        }
        var dpi = VisualTreeHelper.GetDpi(this);
        int radius = (int)Math.Round(10 * dpi.DpiScaleX);
        GetWindowRect(hwnd, out var r);
        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        var region = CreateRoundRectRgn(0, 0, w + 1, h + 1, radius * 2, radius * 2);
        SetWindowRgn(hwnd, region, true); // 设置后区域归系统所有，勿删除
    }

    // 最大化时不超过工作区（不遮任务栏）
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
        ApplyRoundedRegion();
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_GETMINMAXINFO = 0x0024;
        if (msg == WM_GETMINMAXINFO)
        {
            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;
            var wa = SystemParameters.WorkArea;
            mmi.ptMaxPosition = new POINT { X = (int)wa.Left, Y = (int)wa.Top };
            mmi.ptMaxSize = new POINT { X = (int)wa.Width, Y = (int)wa.Height };
            mmi.ptMaxTrackSize = new POINT { X = (int)wa.Width, Y = (int)wa.Height };
            Marshal.StructureToPtr(mmi, lParam, true);
            handled = true;
        }
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    // ---------- 快捷键 ----------

    void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            Add_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}



// WinForms 对话框需要 IWin32Window 所有者
sealed class Win32Window : System.Windows.Forms.IWin32Window
{
    readonly nint _hwnd;
    public Win32Window(Window w) => _hwnd = new WindowInteropHelper(w).Handle;
    public nint Handle => _hwnd;
}




