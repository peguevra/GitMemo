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
            // ---------- TXT → JSON ----------
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

            // ---------- Git処理（安定版） ----------
            RunGit("pull");   // ★ rebase禁止

            RunGit("add .");

            // ★ 変更あるときだけcommit
            var status = RunGitWithResult("status --porcelain");

            if (!string.IsNullOrWhiteSpace(status))
            {
                RunGit("commit -m \"update\"");
                RunGit("push");

                Log.Info("Git push 完了");
            }
            else
            {
                Log.Info("変更なし（commitスキップ）");
            }
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
    var repoRoot = GetRepoRoot();

    var psi = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = args,
        WorkingDirectory = repoRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var proc = Process.Start(psi);

    string output = proc.StandardOutput.ReadToEnd();
    string error = proc.StandardError.ReadToEnd();

    proc.WaitForExit();

    Console.WriteLine($"[git {args}]");

    if (!string.IsNullOrWhiteSpace(output))
        Console.WriteLine(output);

    if (!string.IsNullOrWhiteSpace(error))
        Console.WriteLine("Git Error: " + error);
}


// =========================
// Git結果取得
// =========================
string RunGitWithResult(string args)
{
    var repoRoot = GetRepoRoot();

    var psi = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = args,
        WorkingDirectory = repoRoot,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var proc = Process.Start(psi);

    string output = proc.StandardOutput.ReadToEnd();

    proc.WaitForExit();

    return output;
}


// =========================
// リポジトリルート取得
// =========================
string GetRepoRoot()
{
    return Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")
    );
}