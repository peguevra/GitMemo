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
    // pull（GitHub → TXT）
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
    // push（TXT → JSON → Git）
    // =========================
    static void RunPush(GlobalPaths paths)
    {
        // ---------- TXT読込 ----------
        var lines = File.ReadAllLines(
            paths.InputFile,
            Encoding.GetEncoding("shift_jis")
        );

        var parser = new MemoParser();
        var parsed = parser.Parse(lines);

        var builder = new EventBuilder();
        var newEvents = builder.Build(parsed);

        // ---------- 重複検出 ----------
        var dupGroups = newEvents
            .GroupBy(x => x.Id)
            .Where(g => g.Count() > 1)
            .ToList();

        if (dupGroups.Any())
        {
            Log.Error($"重複ID検出: {dupGroups.Count}件");

            foreach (var g in dupGroups)
            {
                Log.Error($"ID重複: {g.Key} / 件数: {g.Count()}");
            }
        }

        // ---------- 重複除去 ----------
        var uniqueEvents = newEvents
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToList();

        // ---------- 既存JSON取得 ----------
        var importer = new GitHubImporter();
        var url = "https://peguevra.github.io/GitMemo/data/events.json";

        var oldEvents = importer.Fetch(url).Result;

        // ---------- 差分 ----------
        var diff = GetDiff(oldEvents, uniqueEvents);

        Log.Info($"追加: {diff.Added.Count}");
        Log.Info($"削除: {diff.Removed.Count}");
        Log.Info($"変更: {diff.Updated.Count}");

        // ---------- JSON出力 ----------
        var exporter = new JsonExporter();

        exporter.Export(uniqueEvents, paths.JsonFile);
        exporter.Export(uniqueEvents, paths.WebJsonFile);

        Log.Info($"JSON出力: {uniqueEvents.Count}件");

        // ---------- Git ----------
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
    // 差分検出
    // =========================
    static DiffResult GetDiff(List<Event> oldList, List<Event> newList)
    {
        var oldDict = oldList
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var newDict = newList
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First());

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
    // Git実行（修正版）
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

        // ★ ここが重要：エラー判定をExitCodeで行う
        if (proc.ExitCode != 0)
        {
            Console.WriteLine("Git ERROR!");
            Console.WriteLine(error);
        }
        else
        {
            // 正常時のstderrはログ扱い
            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine("[git log]");
                Console.WriteLine(error);
            }
        }
    }
}