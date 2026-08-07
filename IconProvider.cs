using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ToolBox;

public static class IconProvider
{
    static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    public static ImageSource Get(ToolRecord r)
    {
        // 自定义图标失效时回退到 EXE 图标
        if (!string.IsNullOrWhiteSpace(r.IconPath) && File.Exists(r.IconPath))
        {
            var custom = Load(r.IconPath);
            if (custom != null) return custom;
        }
        return FromExe(r.ExecutablePath);
    }

    static ImageSource FromExe(string path)
    {
        if (Cache.TryGetValue(path, out var cached)) return cached;
        var src = Extract(path);
        if (src != null) Cache[path] = src;
        return src ?? Fallback;
    }

    static ImageSource? Load(string path)
    {
        try
        {
            if (string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
                return Extract(path);
            var bmp = new BitmapImage(new Uri(Path.GetFullPath(path)));
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    static ImageSource? Extract(string path)
    {
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon == null) return null;
            using var bmp = icon.ToBitmap();
            return FromHbitmap(bmp.GetHbitmap());
        }
        catch { return null; }
    }

    static ImageSource FromHbitmap(IntPtr hbitmap)
    {
        try
        {
            var src = Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            src.Freeze();
            return src;
        }
        finally { DeleteObject(hbitmap); }
    }

    static ImageSource? _fallback;
    public static ImageSource Fallback => _fallback ??= FromHbitmap(SystemIcons.Application.ToBitmap().GetHbitmap());
}

