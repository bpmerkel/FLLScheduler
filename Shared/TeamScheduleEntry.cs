namespace FLLScheduler.Shared;

public class TeamScheduleEntry
{
    public int Team { get; set; }
    public string Name { get; set; }
    public TimeOnly Judging { get; set; }
    public string Pod { get; set; }
    public TimeOnly Practice { get; set; }
    public string PracticeTable { get; set; }
    public TimeOnly Match1 { get; set; }
    public string Match1Table { get; set; }
    public TimeOnly Match2 { get; set; }
    public string Match2Table { get; set; }
    public TimeOnly Match3 { get; set; }
    public string Match3Table { get; set; }
}
