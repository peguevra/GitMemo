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
        // 既存ファイル比較
        // =========================
        if (File.Exists(path))
        {
            var oldJson = File.ReadAllText(path);

            if (Normalize(oldJson) == Normalize(newJson))
            {
                return false; // 変更なし
            }
        }

        File.WriteAllText(path, newJson);
        return true; // 変更あり
    }

    private string Normalize(string json)
    {
        return json
            .Replace("\r", "")
            .Trim();
    }
}