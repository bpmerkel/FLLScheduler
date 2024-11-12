namespace FLLScheduler.Shared;

/// <summary>
/// Represents the schedule for a team, including judging, practice, and match times and locations.
/// </summary>
public class TeamSchedule
{
    /// <summary>
    /// Gets or sets the team number.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the team name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the start time for judging.
    /// </summary>
    public TimeOnly JudgingStart { get; set; }

    /// <summary>
    /// Gets or sets the judging pod location.
    /// </summary>
    public string JudgingPod { get; set; }

    /// <summary>
    /// Gets or sets the start time for practice.
    /// </summary>
    public TimeOnly PracticeStart { get; set; }

    /// <summary>
    /// Gets or sets the practice table location.
    /// </summary>
    public string PracticeTable { get; set; }

    /// <summary>
    /// Gets or sets the start time for the first match.
    /// </summary>
    public TimeOnly Match1Start { get; set; }

    /// <summary>
    /// Gets or sets the table location for the first match.
    /// </summary>
    public string Match1Table { get; set; }

    /// <summary>
    /// Gets or sets the start time for the second match.
    /// </summary>
    public TimeOnly Match2Start { get; set; }

    /// <summary>
    /// Gets or sets the table location for the second match.
    /// </summary>
    public string Match2Table { get; set; }

    /// <summary>
    /// Gets or sets the start time for the third match.
    /// </summary>
    public TimeOnly Match3Start { get; set; }

    /// <summary>
    /// Gets or sets the table location for the third match.
    /// </summary>
    public string Match3Table { get; set; }
}
