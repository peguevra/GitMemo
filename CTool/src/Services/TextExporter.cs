using System.Text;
using CTool.Models;

namespace CTool.Services;

public class TextExporter
{
    public void Export(IEnumerable<Event> events, string path)
    {
        var lines = events
            .OrderBy(e => e.StartDateTime)
            .Select(e => ToLine(e))
            .ToList();

        File.WriteAllLines(
            path,
            lines,
            Encoding.UTF8
        );
    }

    private string ToLine(Event e)
    {
        var dt = e.StartDateTime;

        string era = GetEra(dt, out int eraYear);

        return $"{era}{eraYear}.{dt:MM}.{dt:dd} {dt:HH:mm} {e.Title}";
    }

    private string GetEra(DateTime dt, out int year)
    {
        if (dt >= new DateTime(2019, 5, 1))
        {
            year = dt.Year - 2018;
            return "R";
        }
        else if (dt >= new DateTime(1989, 1, 8))
        {
            year = dt.Year - 1988;
            return "H";
        }
        else
        {
            year = dt.Year - 1925;
            return "S";
        }
    }
}