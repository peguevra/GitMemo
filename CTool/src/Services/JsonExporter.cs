using System.Text.Json;
using CTool.Models;

namespace CTool.Services;

public class JsonExporter
{
    public void Export(IEnumerable<Event> events, string path)
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

        var json = JsonSerializer.Serialize(wrapper, options);

        File.WriteAllText(path, json);
    }
}