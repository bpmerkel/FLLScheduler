using System.Text.Json.Serialization;

namespace FLLScheduler.Shared;

public class RequestModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("event")]
    public EventConfig Event { get; set; }
    [JsonPropertyName("judging")]
    public JudgingConfig Judging { get; set; }
    [JsonPropertyName("robotgame")]
    public RobotGameConfig RobotGame { get; set; }
    [JsonPropertyName("teams")]
    public Team[] Teams { get; set; }
}