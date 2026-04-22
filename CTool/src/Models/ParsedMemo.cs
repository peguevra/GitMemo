namespace CTool.Models;

public class ParsedMemo
{
    public string Era { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }

    public int? DayEnd { get; set; }

    public string Time { get; set; }

    public string Rest { get; set; }

    public string Raw { get; set; }
}