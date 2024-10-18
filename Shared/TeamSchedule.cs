namespace FLLScheduler.Shared;

public class TeamSchedule
{
    public string Number { get; set; }
    public string Name { get; set; }
    public TimeOnly JudgingStart { get; set; }
    public string JudgingPod { get; set; }
    public TimeOnly PracticeStart { get; set; }
    public string PracticeTable { get; set; }
    public TimeOnly Match1Start { get; set; }
    public string Match1Table { get; set; }
    public TimeOnly Match2Start { get; set; }
    public string Match2Table { get; set; }
    public TimeOnly Match3Start { get; set; }
    public string Match3Table { get; set; }
}
