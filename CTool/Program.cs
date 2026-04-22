using System.Text;
using System.Diagnostics;
using CommonBatchFramework.App;
using CTool;
using CTool.Services;

AppRunner.Run(() =>
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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
        // push（TXT → JSON → Git）
        // =========================
        else if (mode == "push")
        {
            // TXT → JSON
            var lines = File.ReadAllLines(
                paths.InputFile,
                Encoding.GetEncoding("shift_jis")
            );

            var parser = new MemoParser();
            var parsed = parser.Parse(lines);

            var builder = new EventBuilder();
            var events = builder.Build(parsed);

            var exporter = new JsonExporter();
            exporter.Export(events, paths.JsonFile);
            exporter.Export(events, paths.WebJsonFile);

            Log.Info($"JSON出力: {events.Count}件");

            // =========================
            // Git 自動処理（完全自動）
            // =========================

            // ★ リポジトリルートへ移動
            Directory.SetCurrentDirectory(paths.RootDir);

            RunGit("status");

            // ① pull（rebase）
            RunGit("pull --rebase");

            // ② コンフリクト強制解決（JSONはローカル優先）
            if (IsRebaseInProgress())
            {
                Log.Info("コンフリクト検出 → 自動解決");

                RunGit("checkout --ours docs/data/events.json");
                RunGit("add docs/data/events.json");
                RunGit("rebase --continue");
            }

            // ③ commit
            RunGit("add .");
            RunGit("commit -m \"update\"");

            // ④ push
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
// Git 実行
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
// rebase中判定
// =========================
bool IsRebaseInProgress()
{
    var gitDir = ".git";

    return Directory.Exists(Path.Combine(gitDir, "rebase-apply")) ||
           Directory.Exists(Path.Combine(gitDir, "rebase-merge"));
}