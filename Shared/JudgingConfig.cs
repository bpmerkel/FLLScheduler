namespace FLLScheduler.Shared;

public class JudgingConfig
{
    public string[] Pods { get; set; }
    public TimeOnly StartTime { get; set; }
    public int CycleTimeMinutes { get; set; }
    public int BufferMinutes { get; set; }
}
