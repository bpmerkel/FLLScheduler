namespace FLLScheduler.Shared;

public class JudgingQueuingEntry
{
    public TimeOnly QueueTime { get; set; }
    public int Team { get; set; }
    public string Name { get; set; }
    public TimeOnly Judging { get; set; }
    public string Pod { get; set; }
}
