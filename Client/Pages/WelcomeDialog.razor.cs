namespace FLLScheduler.Pages;

/// <summary>
/// Represents the welcome dialog of the application.
/// </summary>
public partial class WelcomeDialog
{
    /// <summary>
    /// Gets or sets the MudDialog instance.
    /// </summary>
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    private void Submit() => MudDialog.Close(DialogResult.Ok(true));

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    /// <returns>The version of the application.</returns>
    private string GetVersion()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
        return $"{version} - {buildDate:ddd MM-dd-yyyy hh\\:mm tt}";
    }
}
