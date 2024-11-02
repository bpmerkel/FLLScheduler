using System.Dynamic;

namespace FLLScheduler.Shared;

public class FlexEntry
{
    public TimeOnly Time { get; set; }
    public string[] Columns { get; set; }
    public List<string> Row { get; set; }

    private ExpandoObject ToFlex()
    {
        var ex = new ExpandoObject();
        ex.TryAdd(nameof(Time), Time);
        for (var i = 0; i < Columns.Length; i++)
        {
            ex.TryAdd(Columns[i], Row[i]);
        }
        return ex;
    }

    public static List<ExpandoObject> Pivot(Array data) => data.Cast<FlexEntry>().Select(e => e.ToFlex()).ToList();
}
