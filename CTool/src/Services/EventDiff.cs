using CTool.Models;

namespace CTool.Services;

public class EventDiff
{
    public DiffResult Diff(List<Event> oldList, List<Event> newList)
    {
        var oldMap = oldList.ToDictionary(x => x.Id);
        var newMap = newList.ToDictionary(x => x.Id);

        var added = new List<Event>();
        var updated = new List<Event>();
        var deleted = new List<Event>();

        // 追加・更新
        foreach (var kv in newMap)
        {
            if (!oldMap.ContainsKey(kv.Key))
            {
                added.Add(kv.Value);
            }
            else
            {
                var old = oldMap[kv.Key];
                var now = kv.Value;

                if (IsChanged(old, now))
                {
                    updated.Add(now);
                }
            }
        }

        // 削除
        foreach (var kv in oldMap)
        {
            if (!newMap.ContainsKey(kv.Key))
            {
                deleted.Add(kv.Value);
            }
        }

        return new DiffResult
        {
            Added = added,
            Updated = updated,
            Deleted = deleted
        };
    }

    private bool IsChanged(Event a, Event b)
    {
        return a.StartDateTime != b.StartDateTime
            || a.EndDateTime != b.EndDateTime
            || a.Title != b.Title;
    }
}

public class DiffResult
{
    public List<Event> Added { get; set; }
    public List<Event> Updated { get; set; }
    public List<Event> Deleted { get; set; }
}