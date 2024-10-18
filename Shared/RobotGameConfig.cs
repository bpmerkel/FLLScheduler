namespace FLLScheduler.Shared;

public class RobotGameConfig
{
    public string[] Tables { get; set; }
    public TimeOnly StartTime { get; set; }
    public int CycleTimeMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public TimeOnly[] BreakTimes  { get; set; }
    public int BreakDurationMinutes { get; set; }
    // NOTE: this scheduler runs all tables at the same timeslot 
}
