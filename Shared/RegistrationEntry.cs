namespace FLLScheduler.Shared;

/// <summary>
/// Represents a registration entry for a team.
/// </summary>
public class RegistrationEntry
{
    /// <summary>
    /// Gets or sets the team number.
    /// </summary>
    public int Team { get; set; }

    /// <summary>
    /// Gets or sets the name of the team.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the roster of the team members.
    /// </summary>
    public string Roster { get; set; }

    /// <summary>
    /// Gets or sets the name of the first coach.
    /// </summary>
    public string Coach1 { get; set; }

    /// <summary>
    /// Gets or sets the name of the second coach.
    /// </summary>
    public string Coach2 { get; set; }
}