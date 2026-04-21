using System.Text;
using CommonBatchFramework.App;
using CTool;
using CTool.Services;

AppRunner.Run(() =>
{
    // ★ これ必須（Shift-JIS有効化）
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    var paths = new GlobalPaths();
    paths.Ensure();

    Log.Initialize(paths.OutputDir);

    Log.Info("CTool batch start");

    try
    {
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

        Log.Info($"処理完了: {events.Count}件");
    }
    catch (Exception ex)
    {
        Log.Error($"致命的エラー: {ex.Message}");
    }

    Log.Info("CTool batch end");
});