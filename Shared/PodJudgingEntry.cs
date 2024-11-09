namespace FLLScheduler.Shared;

/// <summary>
/// Represents an entry for pod judging.
/// </summary>
public class PodJudgingEntry
{
    /// <summary>
    /// Gets or sets the time of the judging entry.
    /// </summary>
    public TimeOnly Time { get; set; }

    /// <summary>
    /// Gets or sets the team number.
    /// </summary>
    public int Team { get; set; }

    /// <summary>
    /// Gets or sets the name associated with the judging entry.
    /// </summary>
    public string Name { get; set; }
}
