namespace FLLScheduler.Shared;

/// <summary>
/// Represents an entry in the judging queue.
/// </summary>
public class JudgingQueuingEntry
{
    /// <summary>
    /// Gets or sets the time the team is queued.
    /// </summary>
    public TimeOnly QueueTime { get; set; }

    /// <summary>
    /// Gets or sets the team number.
    /// </summary>
    public int Team { get; set; }

    /// <summary>
    /// Gets or sets the name of the team.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the time the team is scheduled for judging.
    /// </summary>
    public TimeOnly Judging { get; set; }

    /// <summary>
    /// Gets or sets the pod assigned to the team.
    /// </summary>
    public string Pod { get; set; }
}
