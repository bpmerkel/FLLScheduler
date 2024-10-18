namespace FLLScheduler.Shared;

public class EventConfig
{
    public TimeOnly RegistrationTime { get; set; }
    public TimeOnly CoachesMeetingTime { get; set; }
    public TimeOnly OpeningCeremonyTime { get; set; }
    public TimeOnly LunchStartTime { get; set; }
    public TimeOnly AfternoonStartTime { get; set; }
}
