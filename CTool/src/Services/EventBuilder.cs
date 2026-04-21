using System.Security.Cryptography;
using System.Text;
using CTool.Models;

namespace CTool.Services;

public class EventBuilder
{
    public List<Event> Build(IEnumerable<ParsedMemo> list)
    {
        var result = new List<Event>();

        foreach (var p in list)
        {
            int baseYear = p.Era switch
            {
                "R" => 2018,
                "H" => 1988,
                "S" => 1925,
                _ => throw new Exception($"未知の元号: {p.Era}")
            };

            int year = baseYear + p.Year;

            var start = new DateTime(year, p.Month, p.Day)
                .Add(TimeSpan.Parse(p.Time));

            DateTime? end = null;

            if (p.DayEnd.HasValue)
            {
                end = new DateTime(year, p.Month, p.DayEnd.Value)
                    .Add(TimeSpan.Parse(p.Time));
            }

            string id = GenerateId(p.Raw, start);

            result.Add(new Event
            {
                Id = id,
                StartDateTime = start,
                EndDateTime = end,
                Title = p.Rest,
                Raw = p.Raw
            });
        }

        return result;
    }

    private string GenerateId(string raw, DateTime dt)
    {
        using var sha1 = SHA1.Create();

        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(raw));

        string suffix = BitConverter.ToString(hash)
            .Replace("-", "")
            .Substring(0, 6);

        return $"{dt:yyyyMMdd_HHmm}_{suffix}";
    }
}