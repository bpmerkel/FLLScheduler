namespace FLLScheduler.Shared;

/// <summary>
/// Represents an entry in a pivot table.
/// </summary>
public class PivotEntry
{
    /// <summary>
    /// Gets or sets the name of the pivot entry.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of the pivot.
    /// </summary>
    public PivotType Pivot { get; set; }

    /// <summary>
    /// Gets or sets the data associated with the pivot entry.
    /// </summary>
    public Array Data { get; set; }
}
