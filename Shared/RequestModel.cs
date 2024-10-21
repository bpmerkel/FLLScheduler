using System.Text.Json.Serialization;

namespace FLLScheduler.Shared;

public class RequestModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("event")]
    public EventConfig Event { get; set; } = new EventConfig();
    [JsonPropertyName("judging")]
    public JudgingConfig Judging { get; set; } = new JudgingConfig();
    [JsonPropertyName("robotgame")]
    public RobotGameConfig RobotGame { get; set; }= new RobotGameConfig();
    [JsonPropertyName("teams")]
    public Team[] Teams { get; set; } = [];
}