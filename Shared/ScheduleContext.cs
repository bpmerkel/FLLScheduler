namespace FLLScheduler.Shared;

/// <summary>
/// Represents the context for a schedule, including name, pods, tables, and team schedules.
/// </summary>
public class ScheduleContext
{
    /// <summary>
    /// Gets or sets the name of the schedule context.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the array of pod names.
    /// </summary>
    [JsonPropertyName("pods")]
    public string[] Pods { get; set; }

    /// <summary>
    /// Gets or sets the array of table names.
    /// </summary>
    [JsonPropertyName("tables")]
    public string[] Tables { get; set; }

    /// <summary>
    /// Gets or sets the array of team schedules.
    /// </summary>
    [JsonPropertyName("schedule")]
    public TeamSchedule[] Schedule { get; set; }
}
