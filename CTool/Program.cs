using System.Text;
using CommonBatchFramework.App;
using CTool;
using CTool.Services;

AppRunner.Run(() =>
{
    // ★ Shift-JIS対応
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    var paths = new GlobalPaths();
    paths.Ensure();

    Log.Initialize(paths.OutputDir);

    Log.Info("CTool batch start");

    try
    {
        // =========================
        // ① ローカル → JSON生成
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

        exporter.Export(events, paths.JsonFile);
        exporter.Export(events, paths.WebJsonFile);

        Log.Info($"JSON出力: {events.Count}件");


        // =========================
        // ② GitHub → TXT取込
        // =========================

        var importer = new GitHubImporter();

        // ★ GitHub Pages URL
        var url = "https://peguevra.github.io/GitMemo/data/events.json";

        var remoteEvents = importer.Fetch(url).Result;

        var textExporter = new TextExporter();

        textExporter.Export(remoteEvents, paths.InputFile);

        Log.Info($"GitHub取込: {remoteEvents.Count}件");
    }
    catch (Exception ex)
    {
        Log.Error($"致命的エラー: {ex.Message}");
    }

    Log.Info("CTool batch end");
});