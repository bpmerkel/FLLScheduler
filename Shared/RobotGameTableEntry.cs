namespace FLLScheduler.Shared;

/// <summary>
/// Represents an entry in the robot game table.
/// </summary>
public class RobotGameTableEntry
{
    /// <summary>
    /// Gets or sets the match time.
    /// </summary>
    public TimeOnly MatchTime { get; set; }

    /// <summary>
    /// Gets or sets the team number.
    /// </summary>
    public int Team { get; set; }

    /// <summary>
    /// Gets or sets the name of the team.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the match description.
    /// </summary>
    public string Match { get; set; }
}