using System.Text.Json.Serialization;

namespace FLLScheduler.Shared;

public class ResponseModel
{
    [JsonPropertyName("request")]
    public RequestModel Request { get; set; }
    [JsonPropertyName("pivots")]
    public Pivots Pivots { get; set; }
    [JsonPropertyName("generated")]
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
}
