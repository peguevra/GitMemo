using System.Text;
using System.Text.RegularExpressions;
using CommonBatchFramework.App;
using CTool.Models;

namespace CTool.Services;

public class MemoParser
{
    private static readonly Regex _regex = new Regex(
        @"^(?<era>[A-Z])\s*(?<year>\d+)\.?\s*(?<month>\d{1,2})\.?\s*(?<day>\d{1,2})(-(?<day2>\d{1,2}))?\s+(?<time>\d{1,2}[:：]\d{2}).*?(?<rest>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
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

            try
            {
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
            catch (Exception ex)
            {
                Log.Error($"[Parse] 例外 L{lineNo}: {rawLine} / {ex.Message}");
            }
        }

        return result;
    }

    private string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        input = input.Replace("�", "");

        input = input
            .Replace("　", " ")
            .Replace("：", ":")
            .Replace("－", "-")
            .Replace("−", "-")
            .Replace("～", "-")
            .Replace("〜", "-");

        input = RemoveControlChars(input);

        return input.Trim();
    }

    private string NormalizeTime(string time)
    {
        return time.Replace("：", ":");
    }

    private string RemoveControlChars(string input)
    {
        var sb = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (!char.IsControl(c) || c == '\t')
                sb.Append(c);
        }

        return sb.ToString();
    }
}