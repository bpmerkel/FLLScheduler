namespace FLLScheduler.Shared;

/// <summary>
/// Represents the configuration for an event, including various scheduled times.
/// </summary>
public class EventConfig
{
    /// <summary>
    /// Gets or sets the time for registration.
    /// </summary>
    public TimeOnly RegistrationTime { get; set; }

    /// <summary>
    /// Gets or sets the time for the coaches' meeting.
    /// </summary>
    public TimeOnly CoachesMeetingTime { get; set; }

    /// <summary>
    /// Gets or sets the time for the opening ceremony.
    /// </summary>
    public TimeOnly OpeningCeremonyTime { get; set; }

    /// <summary>
    /// Gets or sets the start time for lunch.
    /// </summary>
    public TimeOnly LunchStartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time for lunch.
    /// </summary>
    public TimeOnly LunchEndTime { get; set; }
}