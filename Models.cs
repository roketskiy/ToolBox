using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace ToolBox;

public class ToolRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string? IconPath { get; set; }

    [JsonIgnore]
    public System.Windows.Media.ImageSource Icon => IconProvider.Get(this);
}

public static class Core
{
    // 名称补全：ProductName -> FileDescription -> 文件名
    public static string PickName(string? productName, string? fileDescription, string fileName)
        => !string.IsNullOrWhiteSpace(productName) ? productName.Trim()
         : !string.IsNullOrWhiteSpace(fileDescription) ? fileDescription.Trim()
         : Path.GetFileNameWithoutExtension(fileName);

    public static ToolRecord ReadFromExe(string path)
    {
        string full = Path.GetFullPath(path);
        var r = new ToolRecord { ExecutablePath = full };
        try
        {
            var vi = FileVersionInfo.GetVersionInfo(full);
            r.Name = PickName(vi.ProductName, vi.FileDescription, Path.GetFileName(full));
        }
        catch
        {
            r.Name = Path.GetFileNameWithoutExtension(full);
        }
        r.WorkingDirectory = Path.GetDirectoryName(full) ?? "";
        return r;
    }

    public static bool SearchMatches(ToolRecord r, string query)
        => query.Length == 0
        || r.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || r.Description.Contains(query, StringComparison.OrdinalIgnoreCase);

    public static bool IsSamePath(string a, string b)
        => string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}

