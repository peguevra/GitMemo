namespace CTool.Models;

public class Event
{
    public required string Id { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    public required string Title { get; set; }

    public required string Raw { get; set; }
}