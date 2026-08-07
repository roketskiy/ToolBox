using System.IO;
using System.Text.Json;

namespace ToolBox;

public static class Storage
{
    public static string DefaultDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ToolBox");

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string DataPath(string dir) => Path.Combine(dir, "tools.json");
    static string BakPath(string dir) => Path.Combine(dir, "tools.json.bak");
    static string TmpPath(string dir) => Path.Combine(dir, "tools.json.tmp");

    public static List<ToolRecord> Load(string dir, out string? warning)
    {
        warning = null;
        string path = DataPath(dir);
        if (!File.Exists(path)) return new List<ToolRecord>();

        if (TryParse(path, out var list)) return list;

        string bak = BakPath(dir);
        if (File.Exists(bak) && TryParse(bak, out var backup))
        {
            warning = "数据文件已损坏，已从最近的有效备份恢复。原文件未被覆盖。";
            return backup;
        }
        warning = "数据文件已损坏且没有有效备份，已从空列表开始。原文件未被覆盖。";
        return new List<ToolRecord>();
    }

    public static void Save(string dir, List<ToolRecord> tools)
    {
        Directory.CreateDirectory(dir);
        string path = DataPath(dir), bak = BakPath(dir), tmp = TmpPath(dir);
        File.WriteAllText(tmp, JsonSerializer.Serialize(tools, JsonOpts));
        if (File.Exists(path)) File.Copy(path, bak, true);   // 先备份当前有效文件
        File.Move(tmp, path, true);                          // 再原子替换
    }

    static bool TryParse(string path, out List<ToolRecord> tools)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<ToolRecord>>(File.ReadAllText(path), JsonOpts);
            tools = new List<ToolRecord>();
            foreach (var t in list ?? Enumerable.Empty<ToolRecord>())
            {
                if (t == null) continue;
                t.Name ??= "";
                t.Description ??= "";
                t.ExecutablePath ??= "";
                t.WorkingDirectory ??= "";
                t.Arguments ??= "";
                tools.Add(t);
            }
            return true;
        }
        catch
        {
            tools = new List<ToolRecord>();
            return false;
        }
    }
}

