namespace FLLScheduler.Shared;

/// <summary>
/// Represents the configuration for judging sessions.
/// </summary>
public class JudgingConfig
{
    /// <summary>
    /// Gets or sets the array of pod names.
    /// </summary>
    public string[] Pods { get; set; }

    /// <summary>
    /// Gets or sets the start time for the judging sessions.
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// Gets or sets the cycle time in minutes for each judging session.
    /// </summary>
    public int CycleTimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets the buffer time in minutes between judging sessions.
    /// </summary>
    public int BufferMinutes { get; set; }
}
