namespace FLLScheduler.Shared;

/// <summary>
/// Represents a request model containing event, judging, robot game configurations, and teams.
/// </summary>
public class RequestModel
{
    /// <summary>
    /// Gets or sets the name of the request.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the event configuration.
    /// </summary>
    [JsonPropertyName("event")]
    public EventConfig Event { get; set; } = new EventConfig();

    /// <summary>
    /// Gets or sets the judging configuration.
    /// </summary>
    [JsonPropertyName("judging")]
    public JudgingConfig Judging { get; set; } = new JudgingConfig();

    /// <summary>
    /// Gets or sets the robot game configuration.
    /// </summary>
    [JsonPropertyName("robotgame")]
    public RobotGameConfig RobotGame { get; set; } = new RobotGameConfig();

    /// <summary>
    /// Gets or sets the array of teams.
    /// </summary>
    [JsonPropertyName("teams")]
    public Team[] Teams { get; set; } = [];
}
