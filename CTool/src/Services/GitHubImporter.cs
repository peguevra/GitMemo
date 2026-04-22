using System.Text;
using System.Text.Json;
using CTool.Models;

namespace CTool.Services;

public class GitHubImporter
{
    public async Task<List<Event>> Fetch(string url)
    {
        using var client = new HttpClient();

        var bytes = await client.GetByteArrayAsync(url);
        var json = Encoding.UTF8.GetString(bytes);

        var doc = JsonSerializer.Deserialize<Wrapper>(json);

        return doc?.events ?? new List<Event>();
    }

    private class Wrapper
    {
        public int version { get; set; }
        public List<Event>? events { get; set; }
    }
}