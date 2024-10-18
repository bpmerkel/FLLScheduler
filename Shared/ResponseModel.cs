using System.Text.Json.Serialization;

namespace FLLScheduler.Shared;

public class ResponseModel
{
    [JsonPropertyName("teams")]
    public TeamSchedule[] Teams { get; set; }
}
