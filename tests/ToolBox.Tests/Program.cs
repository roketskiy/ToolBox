using System.IO;
using ToolBox;

static void Check(bool cond, string name)
{
    if (!cond) throw new Exception($"FAIL: {name}");
    Console.WriteLine($"PASS: {name}");
}

// 名称补全回退顺序
Check(Core.PickName("Product", "Desc", "file.exe") == "Product", "ProductName 优先");
Check(Core.PickName("   ", "Desc", "file.exe") == "Desc", "FileDescription 回退");
Check(Core.PickName(null, "", "file.exe") == "file", "文件名回退");

// 搜索：不区分大小写包含匹配 name/description
var r = new ToolRecord { Name = "图片压缩", Description = "Batch resize PNG" };
Check(Core.SearchMatches(r, "png"), "简介小写命中");
Check(Core.SearchMatches(r, "PNG"), "简介大写命中");
Check(Core.SearchMatches(r, "图片"), "名称命中");
Check(!Core.SearchMatches(r, "video"), "不相关不命中");
Check(Core.SearchMatches(r, ""), "空查询返回全部");

// 路径去重（Windows 不区分大小写）
Check(Core.IsSamePath(@"C:\A\B.exe", @"c:\a\b.exe"), "路径大小写不敏感");

// JSON 往返
string dir = Path.Combine(Path.GetTempPath(), "ToolBoxTest_" + Guid.NewGuid().ToString("N"));
var rec = new ToolRecord
{
    Name = "N", Description = "D", ExecutablePath = @"C:\x\y.exe",
    WorkingDirectory = @"C:\x", Arguments = "--flag \"a b\"", IconPath = null,
};
Storage.Save(dir, new List<ToolRecord> { rec });
Storage.Save(dir, new List<ToolRecord> { rec }); // 第二次保存才会产生备份
var loaded = Storage.Load(dir, out var warning);
Check(warning == null, "干净加载无警告");
Check(loaded.Count == 1, "往返数量一致");
var l = loaded[0];
Check(l.Id == rec.Id && l.Name == rec.Name && l.Description == rec.Description
    && l.ExecutablePath == rec.ExecutablePath && l.WorkingDirectory == rec.WorkingDirectory
    && l.Arguments == rec.Arguments && l.IconPath == rec.IconPath, "往返字段一致");

// 损坏恢复：不覆盖原文件，从备份读取
File.WriteAllText(Storage.DataPath(dir), "{corrupted");
var reloaded = Storage.Load(dir, out var warning2);
Check(warning2 != null, "损坏时提示");
Check(reloaded.Count == 1 && reloaded[0].Name == "N", "从备份恢复");
Check(File.ReadAllText(Storage.DataPath(dir)) == "{corrupted", "损坏原文件未被覆盖");

// 损坏恢复后再保存：不把损坏文件复制成备份，正式文件被有效内容替换
string bakPath = Path.Combine(dir, "tools.json.bak");
string beforeBak = File.ReadAllText(bakPath);
Storage.Save(dir, reloaded);
Check(File.ReadAllText(bakPath) == beforeBak, "保存不会用损坏文件覆盖备份");
Check(File.ReadAllText(Storage.DataPath(dir)) == beforeBak, "损坏文件被有效内容替换");

// 无效路径记录：加载时跳过并提示，避免 IsSamePath/GetDirectoryName 崩溃
string dir2 = Path.Combine(Path.GetTempPath(), "ToolBoxTest2_" + Guid.NewGuid().ToString("N"));
Storage.Save(dir2, new List<ToolRecord>
{
    rec,
    new ToolRecord { Name = "Bad", Description = "D", ExecutablePath = "" },
});
var loaded3 = Storage.Load(dir2, out var warning3);
Check(loaded3.Count == 1 && loaded3[0].Name == "N", "无效路径记录被跳过");
Check(warning3 != null, "跳过时提示");
Directory.Delete(dir2, true);

Directory.Delete(dir, true);
Console.WriteLine("ALL PASS");


