namespace FLLScheduler.Shared;

/// <summary>
/// Represents a schedule entry for a team.
/// </summary>
public class TeamScheduleEntry
{
    /// <summary>
    /// Gets or sets the team number.
    /// </summary>
    public int Team { get; set; }

    /// <summary>
    /// Gets or sets the name of the team.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the judging time.
    /// </summary>
    public TimeOnly Judging { get; set; }

    /// <summary>
    /// Gets or sets the pod name.
    /// </summary>
    public string Pod { get; set; }

    /// <summary>
    /// Gets or sets the practice time.
    /// </summary>
    public TimeOnly Practice { get; set; }

    /// <summary>
    /// Gets or sets the practice table name.
    /// </summary>
    public string PracticeTable { get; set; }

    /// <summary>
    /// Gets or sets the time for the first match.
    /// </summary>
    public TimeOnly Match1 { get; set; }

    /// <summary>
    /// Gets or sets the table name for the first match.
    /// </summary>
    public string Match1Table { get; set; }

    /// <summary>
    /// Gets or sets the time for the second match.
    /// </summary>
    public TimeOnly Match2 { get; set; }

    /// <summary>
    /// Gets or sets the table name for the second match.
    /// </summary>
    public string Match2Table { get; set; }

    /// <summary>
    /// Gets or sets the time for the third match.
    /// </summary>
    public TimeOnly Match3 { get; set; }

    /// <summary>
    /// Gets or sets the table name for the third match.
    /// </summary>
    public string Match3Table { get; set; }
}