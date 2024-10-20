using System.Text.Json.Serialization;

namespace FLLScheduler.Shared;

public class ResponseModel
{
    [JsonPropertyName("schedule")]
    public TeamSchedule[] Schedule { get; set; }
    [JsonPropertyName("request")]
    public RequestModel Request { get; set; }
    [JsonPropertyName("generated")]
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
}
