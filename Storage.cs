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

        if (TryParse(path, out var list, out int skipped))
        {
            if (skipped > 0) warning = $"{skipped} 条记录的可执行文件路径无效，已跳过。";
            return list;
        }

        string bak = BakPath(dir);
        if (File.Exists(bak) && TryParse(bak, out var backup, out skipped))
        {
            warning = skipped > 0
                ? $"数据文件已损坏，已从备份恢复（另有 {skipped} 条无效记录被跳过）。原文件未被覆盖。"
                : "数据文件已损坏，已从最近的有效备份恢复。原文件未被覆盖。";
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
        if (File.Exists(path) && TryParse(path, out _, out _)) File.Copy(path, bak, true); // 只备份有效文件，避免把损坏内容升级成备份
        File.Move(tmp, path, true);                          // 再原子替换
    }

    static bool TryParse(string path, out List<ToolRecord> tools, out int skipped)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<ToolRecord>>(File.ReadAllText(path), JsonOpts);
            tools = new List<ToolRecord>();
            skipped = 0;
            foreach (var t in list ?? Enumerable.Empty<ToolRecord>())
            {
                if (t == null) continue;
                t.Name ??= "";
                t.Description ??= "";
                t.ExecutablePath ??= "";
                t.WorkingDirectory ??= "";
                t.Arguments ??= "";
                if (!IsUsablePath(t.ExecutablePath))   // 路径为空的记录会让 IsSamePath/GetDirectoryName 抛异常
                {
                    skipped++;
                    continue;
                }
                tools.Add(t);
            }
            return true;
        }
        catch
        {
            tools = new List<ToolRecord>();
            skipped = 0;
            return false;
        }
    }

    static bool IsUsablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try { Path.GetFullPath(path); return true; }
        catch { return false; }
    }
}

