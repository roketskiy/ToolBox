using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Forms = System.Windows.Forms;

namespace ToolBox;

public partial class EditWindow : Window
{
    readonly ToolRecord _record;
    readonly List<ToolRecord> _all;

    public EditWindow(ToolRecord record, List<ToolRecord> all, bool isNew)
    {
        InitializeComponent();
        _record = record;
        _all = all;
        Title = isNew ? "添加工具" : "编辑工具";
        Heading.Text = Title;
        NameBox.Text = record.Name;
        DescBox.Text = record.Description;
        ExeBox.Text = record.ExecutablePath;
        WorkDirBox.Text = record.WorkingDirectory;
        ArgsBox.Text = record.Arguments;
        IconBox.Text = record.IconPath ?? "";
        UpdatePreview();
    }

    // 预览用临时副本，避免取消时污染原记录
    void UpdatePreview()
    {
        var probe = new ToolRecord
        {
            ExecutablePath = ExeBox.Text.Trim(),
            IconPath = string.IsNullOrWhiteSpace(IconBox.Text) ? null : IconBox.Text.Trim(),
        };
        IconPreview.Source = IconProvider.Get(probe);
    }

    void Exe_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        string exe = ExeBox.Text.Trim();
        if (!File.Exists(exe)) return;
        if (string.IsNullOrWhiteSpace(NameBox.Text))
            NameBox.Text = Core.ReadFromExe(exe).Name;
        if (string.IsNullOrWhiteSpace(WorkDirBox.Text))
            WorkDirBox.Text = Path.GetDirectoryName(exe) ?? "";
    }

    void Icon_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdatePreview();

    void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        using var ofd = new Forms.OpenFileDialog
        {
            Title = "选择程序",
            Filter = "程序 (*.exe)|*.exe",
            CheckFileExists = true,
            FileName = ExeBox.Text,
        };
        if (ofd.ShowDialog(new Win32Window(this)) == Forms.DialogResult.OK)
            ExeBox.Text = Path.GetFullPath(ofd.FileName);
    }

    void BrowseDir_Click(object sender, RoutedEventArgs e)
    {
        using var fbd = new Forms.FolderBrowserDialog
        {
            Description = "选择工作目录",
            SelectedPath = WorkDirBox.Text,
        };
        if (fbd.ShowDialog(new Win32Window(this)) == Forms.DialogResult.OK)
            WorkDirBox.Text = fbd.SelectedPath;
    }

    void BrowseIcon_Click(object sender, RoutedEventArgs e)
    {
        using var ofd = new Forms.OpenFileDialog
        {
            Title = "选择图标",
            Filter = "图标文件 (*.ico;*.png;*.exe)|*.ico;*.png;*.exe|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            FileName = IconBox.Text,
        };
        if (ofd.ShowDialog(new Win32Window(this)) == Forms.DialogResult.OK)
            IconBox.Text = Path.GetFullPath(ofd.FileName);
    }

    void ClearIcon_Click(object sender, RoutedEventArgs e) => IconBox.Text = "";

    void Save_Click(object sender, RoutedEventArgs e)
    {
        string name = NameBox.Text.Trim();
        string desc = DescBox.Text.Trim();
        string exe = ExeBox.Text.Trim();
        if (name.Length == 0 || desc.Length == 0 || exe.Length == 0)
        {
            MessageBox.Show(this, "名称、简介和可执行文件路径为必填项。", "无法保存",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!File.Exists(exe))
        {
            MessageBox.Show(this, "可执行文件不存在，请检查路径。", "无法保存",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dup = _all.FirstOrDefault(t => t.Id != _record.Id && Core.IsSamePath(t.ExecutablePath, exe));
        if (dup != null)
        {
            MessageBox.Show(this, $"“{exe}”已用于“{dup.Name}”，不能重复添加。", "无法保存",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _record.Name = name;
        _record.Description = desc;
        _record.ExecutablePath = Path.GetFullPath(exe);
        _record.WorkingDirectory = WorkDirBox.Text.Trim();
        _record.Arguments = ArgsBox.Text;
        string icon = IconBox.Text.Trim();
        // 自定义图标失效时回退：不保存无效路径
        _record.IconPath = string.IsNullOrEmpty(icon) || !File.Exists(icon) ? null : Path.GetFullPath(icon);
        DialogResult = true;
    }
}

