using System.Text.Json.Serialization;

namespace FLLScheduler.Shared;

public class ResponseModel
{
    [JsonPropertyName("schedule")]
    public TeamSchedule[] Schedule { get; set; }
}
