using System.Text;
using System.Diagnostics;
using System.Text.Json;
using CommonBatchFramework.App;
using CTool;
using CTool.Services;
using CTool.Models;

AppRunner.Run(() =>
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    Console.InputEncoding = Encoding.GetEncoding("shift_jis");
    Console.OutputEncoding = Encoding.GetEncoding("shift_jis");

    var paths = new GlobalPaths();
    paths.Ensure();

    Log.Initialize(paths.OutputDir);

    Log.Info("CTool batch start");

    try
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length < 2)
        {
            Log.Error("コマンド指定: pull / push");
            return;
        }

        var mode = args[1].ToLower();

        if (mode == "pull")
        {
            var importer = new GitHubImporter();
            var url = "https://peguevra.github.io/GitMemo/data/events.json";

            var remoteEvents = importer.Fetch(url).Result;

            var exporter = new TextExporter();
            exporter.Export(remoteEvents, paths.InputFile);

            Log.Info($"GitHub取込: {remoteEvents.Count}件");
        }
        else if (mode == "push")
        {
            // =========================
            // 1. memo.txt → parse
            // =========================
            var lines = File.ReadAllLines(
                paths.InputFile,
                Encoding.GetEncoding("shift_jis")
            );

            var parser = new MemoParser();
            var parsed = parser.Parse(lines);

            var builder = new EventBuilder();
            var newEvents = builder.Build(parsed);

            // =========================
            // 2. 既存JSON読み込み
            // =========================
            List<Event> oldEvents = new();

            if (File.Exists(paths.WebJsonFile))
            {
                var oldJson = File.ReadAllText(paths.WebJsonFile);
                var wrapper = JsonSerializer.Deserialize<Wrapper>(oldJson);
                oldEvents = wrapper?.events ?? new List<Event>();
            }

            // =========================
            // 3. 差分比較
            // =========================
            var options = new JsonSerializerOptions { WriteIndented = false };

            var newJson = JsonSerializer.Serialize(newEvents, options);
            var oldJsonNorm = JsonSerializer.Serialize(oldEvents, options);

            if (newJson == oldJsonNorm)
            {
                Log.Info("差分なし → Git処理スキップ");
                return;
            }

            Log.Info("差分あり → 更新実行");

            // =========================
            // 4. 出力
            // =========================
            var exporter = new JsonExporter();
            exporter.Export(newEvents, paths.JsonFile);
            exporter.Export(newEvents, paths.WebJsonFile);

            Log.Info($"JSON生成: {newEvents.Count}件");

            // =========================
            // 5. Git処理
            // =========================
            Directory.SetCurrentDirectory(paths.RootDir);

            RunGit("status");
            RunGit("add docs/data/events.json");
            RunGit("commit -m \"auto update\"");
            RunGit("push");

            Log.Info("Git push 完了");
        }
        else
        {
            Log.Error("不明コマンド");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex.ToString());
    }

    Log.Info("CTool batch end");
});


// =========================
// Git実行
// =========================
void RunGit(string args)
{
    Log.Info($"[git {args}]");

    var psi = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = args,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.GetEncoding("shift_jis"),
        StandardErrorEncoding = Encoding.GetEncoding("shift_jis")
    };

    using var proc = Process.Start(psi);

    string output = proc.StandardOutput.ReadToEnd();
    string error = proc.StandardError.ReadToEnd();

    proc.WaitForExit();

    if (!string.IsNullOrWhiteSpace(output))
        Console.WriteLine(output);

    if (!string.IsNullOrWhiteSpace(error))
        Console.WriteLine("Git Error: " + error);
}


// =========================
// GitHub JSON wrapper
// =========================
class Wrapper
{
    public int version { get; set; }
    public List<Event> events { get; set; } = new();
}