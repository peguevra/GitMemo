using CTool.Services;
using CTool.Models;

namespace CTool;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=========================");
        Console.WriteLine("GitMemo START");
        Console.WriteLine("=========================");

        try
        {
            var paths = new GlobalPaths();
            paths.Ensure();

            // =========================
            // ★ GitHub → JSON取得
            // =========================
            var importer = new GitHubImporter();

            var url = "https://your-github-url/events.json";
            var events = await importer.Fetch(url);

            Console.WriteLine($"[IMPORT] {events.Count} 件");

            // =========================
            // ★ JSON → memo.txt 出力
            // =========================
            var textExporter = new TextExporter();
            textExporter.Export(events, paths.InputFile);

            Console.WriteLine("[EXPORT] memo.txt 出力完了");

            // =========================
            // ★ memo.txt → Parse
            // =========================
            var lines = File.ReadAllLines(paths.InputFile);

            var parser = new MemoParser();
            var parsed = parser.Parse(lines);

            Console.WriteLine($"[PARSE] {parsed.Count} 件");

            // =========================
            // ★ Event生成
            // =========================
            var builder = new EventBuilder();
            var built = builder.Build(parsed);

            Console.WriteLine($"[BUILD] {built.Count} 件");

            // =========================
            // ★ JSON出力
            // =========================
            var jsonExporter = new JsonExporter();
            jsonExporter.Export(built, paths.WebJsonFile);

            Console.WriteLine("[JSON] 出力完了");

            Console.WriteLine("=========================");
            Console.WriteLine("DONE");
            Console.WriteLine("=========================");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR]");
            Console.WriteLine(ex.ToString());
        }
    }
}