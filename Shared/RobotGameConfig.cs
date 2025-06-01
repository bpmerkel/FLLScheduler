namespace FLLScheduler.Shared;

/// <summary>
/// Configuration settings for the Robot Game.
/// </summary>
public class RobotGameConfig
{
    /// <summary>
    /// Gets or sets the tables used in the game.
    /// </summary>
    public string[] Tables { get; set; }

    /// <summary>
    /// Gets or sets the start time of the game.
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// Gets or sets the cycle time in minutes.
    /// </summary>
    public int CycleTimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets the buffer time in minutes.
    /// </summary>
    public int BufferMinutes { get; set; }

    /// <summary>
    /// Gets or sets the break times during the game.
    /// </summary>
    public TimeOnly[] BreakTimes { get; set; }

    /// <summary>
    /// Gets or sets the duration of each break in minutes.
    /// </summary>
    public int BreakDurationMinutes { get; set; }

    // NOTE: this scheduler runs all tables at the same timeslot 
}