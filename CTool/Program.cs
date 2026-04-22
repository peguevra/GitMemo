using System.Text;
using System.Diagnostics;
using CommonBatchFramework.App;
using CTool.Services;
using CTool.Models;

namespace CTool;

class Program
{
    static void Main(string[] args)
    {
        AppRunner.Run(() =>
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var paths = new GlobalPaths();
            paths.Ensure();

            Log.Initialize(paths.OutputDir);

            Log.Info("CTool batch start");

            try
            {
                if (args.Length < 1)
                {
                    Log.Error("コマンド指定: pull / push");
                    return;
                }

                var mode = args[0].ToLower();

                if (mode == "pull")
                {
                    RunPull(paths);
                }
                else if (mode == "push")
                {
                    RunPush(paths);
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
    }

    // =========================
    // pull
    // =========================
    static void RunPull(GlobalPaths paths)
    {
        var importer = new GitHubImporter();

        var url = "https://peguevra.github.io/GitMemo/data/events.json";

        var remoteEvents = importer.Fetch(url).Result;

        var textExporter = new TextExporter();

        textExporter.Export(remoteEvents, paths.InputFile);

        Log.Info($"GitHub取込: {remoteEvents.Count}件");
    }

    // =========================
    // push
    // =========================
    static void RunPush(GlobalPaths paths)
    {
        var lines = File.ReadAllLines(
            paths.InputFile,
            Encoding.GetEncoding("shift_jis")
        );

        var parser = new MemoParser();
        var parsed = parser.Parse(lines);

        var builder = new EventBuilder();
        var newEvents = builder.Build(parsed);

        var importer = new GitHubImporter();
        var url = "https://peguevra.github.io/GitMemo/data/events.json";

        var oldEvents = importer.Fetch(url).Result;

        var diff = GetDiff(oldEvents, newEvents);

        Log.Info($"追加: {diff.Added.Count}");
        Log.Info($"削除: {diff.Removed.Count}");
        Log.Info($"変更: {diff.Updated.Count}");

        var exporter = new JsonExporter();

        exporter.Export(newEvents, paths.JsonFile);
        exporter.Export(newEvents, paths.WebJsonFile);

        if (diff.HasChanges)
        {
            RunGit("pull");
            RunGit("add .");
            RunGit("commit -m \"update\"");
            RunGit("push");

            Log.Info("Git push 完了");
        }
        else
        {
            Log.Info("差分なし → Gitスキップ");
        }
    }

    // =========================
    // 差分
    // =========================
    static DiffResult GetDiff(List<Event> oldList, List<Event> newList)
    {
        var oldDict = oldList.ToDictionary(x => x.Id);
        var newDict = newList.ToDictionary(x => x.Id);

        var added = new List<Event>();
        var removed = new List<Event>();
        var updated = new List<Event>();

        foreach (var kv in newDict)
        {
            if (!oldDict.ContainsKey(kv.Key))
            {
                added.Add(kv.Value);
            }
            else
            {
                var old = oldDict[kv.Key];
                var now = kv.Value;

                if (old.Title != now.Title ||
                    old.StartDateTime != now.StartDateTime)
                {
                    updated.Add(now);
                }
            }
        }

        foreach (var kv in oldDict)
        {
            if (!newDict.ContainsKey(kv.Key))
            {
                removed.Add(kv.Value);
            }
        }

        return new DiffResult
        {
            Added = added,
            Removed = removed,
            Updated = updated
        };
    }

    class DiffResult
    {
        public List<Event> Added { get; set; } = new();
        public List<Event> Removed { get; set; } = new();
        public List<Event> Updated { get; set; } = new();

        public bool HasChanges =>
            Added.Count > 0 ||
            Removed.Count > 0 ||
            Updated.Count > 0;
    }

    // =========================
    // Git
    // =========================
    static void RunGit(string args)
    {
        var repoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")
        );

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
}