namespace FLLScheduler.Shared;

/// <summary>
/// Represents an entry in the robot game queuing system.
/// </summary>
public class RobotGameQueuingEntry
{
    /// <summary>
    /// Gets or sets the time when the team is queued.
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
    /// Gets or sets the time of the match.
    /// </summary>
    public TimeOnly MatchTime { get; set; }

    /// <summary>
    /// Gets or sets the match identifier.
    /// </summary>
    public string Match { get; set; }

    /// <summary>
    /// Gets or sets the table identifier.
    /// </summary>
    public string Table { get; set; }
}