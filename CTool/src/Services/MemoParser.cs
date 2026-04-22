using System.Text.RegularExpressions;
using CommonBatchFramework.App;
using CTool.Models;

namespace CTool.Services;

public class MemoParser
{
    private static readonly Regex _regex = new Regex(
        @"^(?<era>[A-Z])\s*(?<year>\d+)\.?\s*(?<month>\d{1,2})\.?\s*(?<day>\d{1,2})(-(?<day2>\d{1,2}))?\s+(?<time>\d{1,2}[:：]\d{2}).*?(?<rest>.+)$",
        RegexOptions.Compiled
    );

    public List<ParsedMemo> Parse(IEnumerable<string> lines)
    {
        var result = new List<ParsedMemo>();
        int lineNo = 0;

        foreach (var rawLine in lines)
        {
            lineNo++;

            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            if (rawLine.TrimStart().StartsWith("#"))
                continue;

            var line = Normalize(rawLine);

            var m = _regex.Match(line);

            if (!m.Success)
            {
                Log.Error($"[Parse] 形式不正 L{lineNo}: {rawLine}");
                continue;
            }

            result.Add(new ParsedMemo
            {
                Era = m.Groups["era"].Value,
                Year = int.Parse(m.Groups["year"].Value),
                Month = int.Parse(m.Groups["month"].Value),
                Day = int.Parse(m.Groups["day"].Value),
                DayEnd = m.Groups["day2"].Success
                    ? int.Parse(m.Groups["day2"].Value)
                    : null,
                Time = NormalizeTime(m.Groups["time"].Value),
                Rest = m.Groups["rest"].Value.Trim(),
                Raw = rawLine
            });
        }

        return result;
    }

    private string Normalize(string input)
    {
        return input
            .Replace("　", " ")
            .Replace("：", ":")
            .Replace("－", "-")
            .Replace("−", "-")
            .Replace("～", "-")
            .Replace("〜", "-")
            .Replace("�", "")
            .Trim();
    }

    private string NormalizeTime(string time)
    {
        return time.Replace("：", ":");
    }
}