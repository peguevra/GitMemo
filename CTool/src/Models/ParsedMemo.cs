namespace CTool.Models;

public class ParsedMemo
{
    public required string Era { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }

    public int? DayEnd { get; set; }

    public required string Time { get; set; }

    public required string Rest { get; set; }

    public required string Raw { get; set; }
}