namespace CTool.Models;

public class Event
{
    public string Id { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    public string Title { get; set; }

    public string Raw { get; set; }
}