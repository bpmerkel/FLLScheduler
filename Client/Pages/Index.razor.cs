using FLLScheduler.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.Extensions.Logging;

namespace FLLScheduler.Pages;

/// <summary>
/// Represents the main page of the application.
/// </summary>
public partial class Index
{
    /// <summary>
    /// Gets or sets the dialog service.
    /// </summary>
    [Inject] private IDialogService DialogService { get; set; }

    /// <summary>
    /// Gets or sets the browser viewport service.
    /// </summary>
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; }
    [Inject] private HttpClient httpClient { get; set; }

    private RequestModel Profile { get; set; }
    private TimeSpan? RegistrationTime { get; set; }
    private TimeSpan? CoachesMeetingTime { get; set; }
    private TimeSpan? OpeningCeremonyTime { get; set; }
    private TimeSpan? LunchStartTime { get; set; }
    private TimeSpan? LunchEndTime { get; set; }
    private TimeSpan? JudgingStartTime { get; set; }
    private TimeSpan? RobotGamesStartTime { get; set; }
    private int CycleTimeMinutes { get; set; }
    private int JudgingBufferMinutes { get; set; }
    private int RobotGameCycleTimeMinutes { get; set; }
    private int RobotGameBufferMinutes { get; set; }
    private int BreakDurationMinutes { get; set; }
    private string PodNames { get; set; }
    private string Breaks { get; set; }
    private string TableNames { get; set; }
    private string Teams { get; set; }
    //private TeamSchedule[] Schedule { get; set; }
    private MudDataGrid<TeamSchedule> dataGrid;

    protected override async void OnAfterRender(bool firstRender)
    {
        await DoProfileSelected(Profiles[0]);
    }

    private async Task DoProfileSelected(RequestModel value)
    {
        Profile = value;
        RegistrationTime = Profile.Event.RegistrationTime.ToTimeSpan();
        CoachesMeetingTime = Profile.Event.CoachesMeetingTime.ToTimeSpan();
        OpeningCeremonyTime = Profile.Event.OpeningCeremonyTime.ToTimeSpan();
        LunchStartTime = Profile.Event.LunchStartTime.ToTimeSpan();
        LunchEndTime = Profile.Event.AfternoonStartTime.ToTimeSpan();
        JudgingStartTime = Profile.Judging.StartTime.ToTimeSpan();
        RobotGamesStartTime = Profile.RobotGame.StartTime.ToTimeSpan();
        CycleTimeMinutes = Profile.Judging.CycleTimeMinutes;
        JudgingBufferMinutes = Profile.Judging.BufferMinutes;
        RobotGameCycleTimeMinutes = Profile.RobotGame.CycleTimeMinutes;
        RobotGameBufferMinutes = Profile.RobotGame.BufferMinutes;
        BreakDurationMinutes = Profile.RobotGame.BreakDurationMinutes;
        PodNames = string.Join(", ", Profile.Judging.Pods);
        TableNames = string.Join(", ", Profile.RobotGame.Tables);
        Breaks = string.Join(", ", Profile.RobotGame.BreakTimes.Select(t => $"{t:hh\\:mm tt}"));
        Teams = string.Join(Environment.NewLine, Profile.Teams.Select(t => $"{t.Number}, {t.Name}"));
        await dataGrid.ReloadServerData();
    }

    private async Task<GridData<TeamSchedule>> ServerReload(GridState<TeamSchedule> state)
    {
        var json = new StringContent(JsonSerializer.Serialize(Profile), Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync("api/CalculateSchedule", json);
        if (response.IsSuccessStatusCode)
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel>();
            return new GridData<TeamSchedule>
            {
                TotalItems = responseModel.Schedule.Length,
                Items = responseModel.Schedule
            };
        }
        return new GridData<TeamSchedule>
        {
            TotalItems = 0,
            Items = []
        };
    }

    //private async Task<ResponseModel> CallApi(RequestModel request)
    //{
    //    using var httpClient = new HttpClient();
    //    var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
    //    using var response = await httpClient.PostAsync("https://fllscheduler.merkel.net/api/calculateschedule", json);
    //    if (response.IsSuccessStatusCode)
    //    {
    //        return await response.Content.ReadFromJsonAsync<ResponseModel>();
    //    }
    //    else
    //    {
    //        return new ResponseModel { Schedule = [], Request = request, GeneratedUtc = DateTime.UtcNow };
    //    }
    //}

    private async Task<IEnumerable<RequestModel>> IdentifyProfiles(string value, CancellationToken token)
    {
        // if text is null or empty, show complete list
        if (string.IsNullOrEmpty(value)) return Profiles;
        return await Task.FromResult(Profiles
            .Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase))
            .AsEnumerable());
    }

    /// <summary>
    /// Represents the width of the browser viewport.
    /// </summary>
    private int _width = 0;

    /// <summary>
    /// Gets a value indicating whether the screen is small.
    /// </summary>
    private bool IsSmallScreen => _width < 900;

    /// <summary>
    /// Executes after the component is rendered.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
            OpenWelcomeDialog();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    /// Opens the welcome dialog.
    /// </summary>
    private void OpenWelcomeDialog() => DialogService.Show<WelcomeDialog>("Welcome", new DialogOptions
    {
        MaxWidth = MaxWidth.Large,
        CloseButton = true,
        BackdropClick = false,
        NoHeader = false,
        Position = DialogPosition.Center,
        CloseOnEscapeKey = true
    });

    /// <summary>
    /// Gets the ID of the browser viewport observer.
    /// </summary>
    Guid IBrowserViewportObserver.Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the resize options of the browser viewport observer.
    /// </summary>
    ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
    {
        ReportRate = 1000,
        NotifyOnBreakpointOnly = false
    };

    /// <summary>
    /// Notifies the browser viewport change.
    /// </summary>
    Task IBrowserViewportObserver.NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
    {
        _width = browserViewportEventArgs.BrowserWindowSize.Width;
        //_height = browserViewportEventArgs.BrowserWindowSize.Height;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Disposes the component.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await BrowserViewportService.UnsubscribeAsync(this);
        GC.SuppressFinalize(this);
    }

    public static RequestModel[] Profiles = new[]
        {
            (teamcount: 12, tablecount: 2),
            (teamcount: 18, tablecount: 4),
            (teamcount: 24, tablecount: 4),
            (teamcount: 36, tablecount: 4),
            (teamcount: 48, tablecount: 6),
            (teamcount: 60, tablecount: 10)
        }
        .Select(e => BuildRequest(e.teamcount, e.tablecount))
        .ToArray();

    private static RequestModel BuildRequest(int teamcount, int tablecount)
    {
        // Ensure even number of tables specfified
        ArgumentOutOfRangeException.ThrowIfNotEqual(0, tablecount % 2, nameof(tablecount));

        var podcount = Convert.ToInt32(Math.Ceiling(teamcount / 6d));   // always a max of 6 teams judged per pod
        var allteams = Enumerable.Range(1001, teamcount)
            .Select(i => new Team { Number = $"{i:0000}", Name = $"team {i:0000}" })
            .ToArray();
        var allpods = Enumerable.Range(1, podcount)
            .Select(i => $"Pod {i}")
            .ToArray();
        var oceans = new[] { "Atlantic", "Pacific", "Indian", "Arctic", "Southern", "Procellarum", "Boreum", "Europa", "Enceladus", "Ganymede", "Titan", "Callisto" };
        var alltables = Enumerable.Range(0, tablecount)
            .Select(i => oceans[i])
            .ToArray();

        return new RequestModel
        {
            Name = $"{teamcount} Teams, {podcount} Judging Pods, {tablecount} Game Tables",
            Event = new EventConfig
            {
                RegistrationTime = TimeOnly.Parse("8:00 am"),
                CoachesMeetingTime = TimeOnly.Parse("8:30 am"),
                OpeningCeremonyTime = TimeOnly.Parse("9:00 am"),
                LunchStartTime = TimeOnly.Parse("12:00 pm"),
                AfternoonStartTime = TimeOnly.Parse("1:00 pm")
            },
            Judging = new JudgingConfig
            {
                Pods = allpods,
                StartTime = TimeOnly.Parse("9:30 am"),
                CycleTimeMinutes = 30,
                BufferMinutes = 15
            },
            RobotGame = new RobotGameConfig
            {
                Tables = alltables,
                StartTime = TimeOnly.Parse("9:20 am"),
                CycleTimeMinutes = 10,
                BufferMinutes = 10,
                BreakTimes = [TimeOnly.Parse("10:00 am"), TimeOnly.Parse("11:00 am"), TimeOnly.Parse("2:00 pm")],
                BreakDurationMinutes = 10
            },
            Teams = allteams
        };
    }
}