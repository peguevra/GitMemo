using System.Text.Json;
using CTool.Models;

namespace CTool.Services;

public class JsonExporter
{
    public bool Export(IEnumerable<Event> events, string path)
    {
        var sorted = events
            .OrderBy(e => e.StartDateTime)
            .ToList();

        var wrapper = new
        {
            version = 1,
            events = sorted
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var newJson = JsonSerializer.Serialize(wrapper, options);

        // =========================
        // ★ 差分チェック追加
        // =========================
        if (File.Exists(path))
        {
            var oldJson = File.ReadAllText(path);

            if (oldJson == newJson)
            {
                Console.WriteLine("[JSON] 変更なし（スキップ）");
                return false;
            }
        }

        File.WriteAllText(path, newJson);
        Console.WriteLine("[JSON] 更新あり");

        return true;
    }
}