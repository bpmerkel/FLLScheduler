using System.Text.Json.Serialization;

namespace FLLScheduler.Shared;

public class ScheduleContext
{
    [JsonPropertyName("pods")]
    public string[] Pods { get; set; }
    [JsonPropertyName("tables")]
    public string[] Tables { get; set; }
    [JsonPropertyName("schedule")]
    public TeamSchedule[] Schedule { get; set; }
}
