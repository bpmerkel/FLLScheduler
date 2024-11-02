namespace FLLScheduler.Shared;

public class RobotGameQueuingEntry
{
    public TimeOnly QueueTime { get; set; }
    public int Team { get; set; }
    public string Name { get; set; }
    public TimeOnly MatchTime { get; set; }
    public string Match { get; set; }
    public string Table { get; set; }
}
