using System.Text;
using System.Diagnostics;
using CommonBatchFramework.App;
using CTool;
using CTool.Services;

AppRunner.Run(() =>
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // =========================
    // コンソール設定（Shift-JIS）
    // =========================
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
            // =========================
            // memo.txt 読み込み（Shift-JIS）
            // =========================
            var lines = File.ReadAllLines(
                paths.InputFile,
                Encoding.GetEncoding("shift_jis")
            );

            var parser = new MemoParser();
            var parsed = parser.Parse(lines);

            var builder = new EventBuilder();
            var events = builder.Build(parsed);

            var exporter = new JsonExporter();

            // ★ 差分チェック付き出力
            bool changedLocal = exporter.Export(events, paths.JsonFile);
            bool changedWeb = exporter.Export(events, paths.WebJsonFile);

            Log.Info($"JSON生成: {events.Count}件");

            // =========================
            // 変更なしならGitしない
            // =========================
            if (!changedLocal && !changedWeb)
            {
                Log.Info("差分なし → Git処理スキップ");
                return;
            }

            // =========================
            // Git処理
            // =========================
            Directory.SetCurrentDirectory(paths.RootDir);

            RunGit("status");

            // ★ 安全pull（変更ある場合は失敗する可能性あり）
            var pullResult = RunGit("pull --rebase");

            if (pullResult.Contains("Please commit or stash"))
            {
                Log.Info("ローカル変更あり → 自動コミット後に再実行");

                RunGit("add .");
                RunGit("commit -m \"auto save before rebase\"");
                RunGit("pull --rebase");
            }

            RunGit("add docs/data/events.json");

            var commitResult = RunGit("commit -m \"update\"");

            if (commitResult.Contains("nothing to commit"))
            {
                Log.Info("コミット対象なし → 終了");
                return;
            }

            var pushResult = RunGit("push");

            if (pushResult.Contains("Everything up-to-date"))
            {
                Log.Info("Git: 変更なし");
            }

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
// Git実行（出力を返す版）
// =========================
string RunGit(string args)
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

    return output + "\n" + error;
}


// =========================
// rebase判定（未使用だが残す）
// =========================
bool IsRebaseInProgress()
{
    var gitDir = ".git";

    return Directory.Exists(Path.Combine(gitDir, "rebase-apply")) ||
           Directory.Exists(Path.Combine(gitDir, "rebase-merge"));
}