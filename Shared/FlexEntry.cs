namespace FLLScheduler.Shared;

/// <summary>
/// Represents a flexible entry with a time, columns, and row data.
/// </summary>
public class FlexEntry
{
    /// <summary>
    /// Gets or sets the time of the entry.
    /// </summary>
    public TimeOnly Time { get; set; }

    /// <summary>
    /// Gets or sets the column names for the entry.
    /// </summary>
    public string[] Columns { get; set; }

    /// <summary>
    /// Gets or sets the row data for the entry.
    /// </summary>
    public List<string> Row { get; set; }

    /// <summary>
    /// Converts the entry to an ExpandoObject.
    /// </summary>
    /// <returns>An ExpandoObject representing the entry.</returns>
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

    /// <summary>
    /// Pivots an array of FlexEntry objects to a list of ExpandoObject.
    /// </summary>
    /// <param name="data">The array of FlexEntry objects.</param>
    /// <returns>A list of ExpandoObject representing the pivoted data.</returns>
    public static List<ExpandoObject> Pivot(Array data) => data.Cast<FlexEntry>().Select(e => e.ToFlex()).ToList();
}