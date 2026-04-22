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

    // ★ UTF-8統一
    Console.InputEncoding = Encoding.UTF8;
    Console.OutputEncoding = Encoding.UTF8;

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

        // =========================
        // pull（GitHub → TXT）
        // =========================
        if (mode == "pull")
        {
            var importer = new GitHubImporter();
            var url = "https://peguevra.github.io/GitMemo/data/events.json";

            var remoteEvents = importer.Fetch(url).Result;

            var textExporter = new TextExporter();
            textExporter.Export(remoteEvents, paths.InputFile);

            Log.Info($"GitHub取込: {remoteEvents.Count}件");
        }

        // =========================
        // push（差分更新版）
        // =========================
        else if (mode == "push")
        {
            // -------------------------
            // 1. memo.txt → 新イベント生成
            // -------------------------
            var lines = File.ReadAllLines(paths.InputFile, Encoding.UTF8);

            var parser = new MemoParser();
            var parsed = parser.Parse(lines);

            var builder = new EventBuilder();
            var newEvents = builder.Build(parsed);

            Log.Info($"新規生成: {newEvents.Count}件");

            // -------------------------
            // 2. 既存JSON読み込み（旧状態）
            // -------------------------
            Dictionary<string, Event> oldMap = new();

            if (File.Exists(paths.JsonFile))
            {
                var oldJson = File.ReadAllText(paths.JsonFile);
                var wrapper = JsonSerializer.Deserialize<JsonWrapper>(oldJson);

                if (wrapper?.events != null)
                {
                    oldMap = wrapper.events.ToDictionary(e => e.Id);
                }
            }

            // -------------------------
            // 3. 新状態マップ
            // -------------------------
            var newMap = newEvents.ToDictionary(e => e.Id);

            // -------------------------
            // 4. 差分算出
            // -------------------------
            var added = newMap.Keys.Except(oldMap.Keys).ToList();
            var deleted = oldMap.Keys.Except(newMap.Keys).ToList();
            var updated = newMap.Keys.Intersect(oldMap.Keys)
                .Where(id =>
                    newMap[id].Title != oldMap[id].Title ||
                    newMap[id].StartDateTime != oldMap[id].StartDateTime
                )
                .ToList();

            Log.Info($"追加: {added.Count}");
            Log.Info($"削除: {deleted.Count}");
            Log.Info($"更新: {updated.Count}");

            // -------------------------
            // 5. マージ処理
            // -------------------------
            foreach (var id in deleted)
                oldMap.Remove(id);

            foreach (var id in added)
                oldMap[id] = newMap[id];

            foreach (var id in updated)
                oldMap[id] = newMap[id];

            var merged = oldMap.Values
                .OrderBy(e => e.StartDateTime)
                .ToList();

            // -------------------------
            // 6. JSON出力
            // -------------------------
            var exporter = new JsonExporter();
            exporter.Export(merged, paths.JsonFile);
            exporter.Export(merged, paths.WebJsonFile);

            Log.Info($"JSON出力: {merged.Count}件");

            // -------------------------
            // 7. Git処理
            // -------------------------
            Directory.SetCurrentDirectory(paths.RootDir);

            RunGit("status");
            RunGit("pull --rebase");

            if (IsRebaseInProgress())
            {
                Log.Info("コンフリクト検出 → 自動解決");

                RunGit("checkout --ours docs/data/events.json");
                RunGit("add docs/data/events.json");
                RunGit("rebase --continue");
            }

            RunGit("add .");
            RunGit("commit -m \"update\"");
            RunGit("push");

            Log.Info("Git push 完了");
        }
        else
        {
            Log.Error("不明コマンド: pull / push");
        }
    }
    catch (Exception ex)
    {
        Log.Error($"致命的エラー: {ex.Message}");
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
        CreateNoWindow = true
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
// rebase判定
// =========================
bool IsRebaseInProgress()
{
    var gitDir = ".git";

    return Directory.Exists(Path.Combine(gitDir, "rebase-apply")) ||
           Directory.Exists(Path.Combine(gitDir, "rebase-merge"));
}


// =========================
// JSONラッパー
// =========================
public class JsonWrapper
{
    public int version { get; set; }
    public List<Event> events { get; set; }
}