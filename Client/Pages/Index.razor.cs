using FLLScheduler.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Dynamic;
using static FLLScheduler.Pages.Index;
using Markdig;

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
    private MarkupString GridsToShow { get; set; }
    private MudDataGrid<TeamSchedule> dataGrid;

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            await DoProfileSelected(Profiles[0]);
        }
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

    private void DoUpdateProfile()
    {
        ArgumentNullException.ThrowIfNull(RegistrationTime);
        ArgumentNullException.ThrowIfNull(CoachesMeetingTime);
        ArgumentNullException.ThrowIfNull(OpeningCeremonyTime);
        ArgumentNullException.ThrowIfNull(LunchStartTime);
        ArgumentNullException.ThrowIfNull(LunchEndTime);
        ArgumentNullException.ThrowIfNull(JudgingStartTime);
        ArgumentNullException.ThrowIfNull(RobotGamesStartTime);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(PodNames);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(TableNames);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(Teams);

        //ArgumentOutOfRangeException.ThrowIfEqual(0, CycleTimeMinutes, nameof(CycleTimeMinutes));
        //ArgumentOutOfRangeException.ThrowIfEqual(0, JudgingBufferMinutes, nameof(JudgingBufferMinutes));
        //ArgumentOutOfRangeException.ThrowIfEqual(0, RobotGameCycleTimeMinutes, nameof(RobotGameCycleTimeMinutes));
        //ArgumentOutOfRangeException.ThrowIfEqual(0, RobotGameBufferMinutes, nameof(RobotGameBufferMinutes));
        //ArgumentOutOfRangeException.ThrowIfEqual(0, BreakDurationMinutes, nameof(BreakDurationMinutes));

        // generate an updated profile based on the modifications in the UI
        var profile = new RequestModel();
        profile.Event.RegistrationTime = TimeOnly.FromTimeSpan(RegistrationTime.Value);
        profile.Event.CoachesMeetingTime = TimeOnly.FromTimeSpan(CoachesMeetingTime.Value);
        profile.Event.OpeningCeremonyTime = TimeOnly.FromTimeSpan(OpeningCeremonyTime.Value);
        profile.Event.LunchStartTime = TimeOnly.FromTimeSpan(LunchStartTime.Value);
        profile.Event.AfternoonStartTime = TimeOnly.FromTimeSpan(LunchEndTime.Value);
        profile.Judging.StartTime = TimeOnly.FromTimeSpan(JudgingStartTime.Value);
        profile.RobotGame.StartTime = TimeOnly.FromTimeSpan(RobotGamesStartTime.Value);
        profile.Judging.CycleTimeMinutes = CycleTimeMinutes;
        profile.Judging.BufferMinutes = JudgingBufferMinutes;
        profile.RobotGame.CycleTimeMinutes = RobotGameCycleTimeMinutes;
        profile.RobotGame.BufferMinutes = RobotGameBufferMinutes;
        profile.RobotGame.BreakDurationMinutes = BreakDurationMinutes;
        profile.Judging.Pods = PodNames.Split(",;\t ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        profile.RobotGame.Tables = TableNames.Split(",;\t ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        profile.RobotGame.BreakTimes = Breaks.Split(",;\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            .Select(b => TimeOnly.TryParse(b, out TimeOnly t) ? t : TimeOnly.MaxValue)  // midnight if invalid
            .ToArray();
        profile.Teams = Teams.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Split(",;\t".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries))
            .Select(pair => new Team { Number = pair[0], Name = pair[1] })
            .ToArray();
        profile.Name = $"Customized: {profile.Teams.Length} Teams, {profile.Judging.Pods.Length} Judging Pods, {profile.RobotGame.Tables.Length} Game Tables";

        ArgumentOutOfRangeException.ThrowIfNotEqual(0, profile.RobotGame.Tables.Length % 2);    // ensure an even number of tables

        Profile = profile;
        var existing = Profiles.FirstOrDefault(p => p.Name == profile.Name);
        if (existing != null)
        {
            Profiles.Remove(existing);
        }
        Profiles.Insert(0, profile);
    }

    private async Task<GridData<TeamSchedule>> ServerReload(GridState<TeamSchedule> state)
    {
        var json = new StringContent(JsonSerializer.Serialize(Profile), Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync("api/CalculateSchedule", json);
        if (response.IsSuccessStatusCode)
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel>();
            ShowResults(responseModel);
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

    private void ShowResults(ResponseModel response)
    {
        var md = new StringBuilder();
        md.AppendLine($"# {response.Request.Name}");
        md.AppendLine($"## Generated {response.GeneratedUtc.ToLocalTime():DDD MMM-dd hh\\:mm tt}");

        var master = response.Schedule;

        var games = master
            .Select(s => new
            {
                Time = s.PracticeStart,
                Table = s.PracticeTable,
                s.Number,
                s.Name,
                Match = "P"
            })
            .Union(master
                .Select(s => new
                {
                    Time = s.Match1Start,
                    Table = s.Match1Table,
                    s.Number,
                    s.Name,
                    Match = "1"
                }))
            .Union(master
                .Select(s => new
                {
                    Time = s.Match2Start,
                    Table = s.Match2Table,
                    s.Number,
                    s.Name,
                    Match = "2"
                }))
            .Union(master
                .Select(s => new
                {
                    Time = s.Match3Start,
                    Table = s.Match3Table,
                    s.Number,
                    s.Name,
                    Match = "3"
                }))
            .Select(e => new QueuerEntry
            {
                QueueTime = e.Time.AddMinutes(-5),
                Number = e.Number,
                Name = e.Name,
                MatchTime = e.Time,
                Match = e.Match,
                Table = e.Table
            })
            .OrderBy(e => e.QueueTime)
            // order tables in the order given in the request
            .ThenBy(e => response.Request.RobotGame.Tables.Select((t, i) => (t, i)).First(ee => ee.t == e.Table).i)
            .ToArray();

        md.AppendLine("### Queuing Schedule");
        md.AppendLine("|Queue Time|Number|Name|Match Time|Match|Table|");
        md.AppendLine("|---------:|-----:|:---|---------:|:---:|:----|");
        foreach (var qe in games)
        {
            md.AppendLine($"|{qe.QueueTime:hh\\:mm tt}|{qe.Number}|{qe.Name}|{qe.MatchTime:hh\\:mm tt}|{qe.Match}|{qe.Table}|");
        }
        md.AppendLine();

        var combined = games
            .GroupBy(game => game.MatchTime)
            .Select(g => new { Time = g.Key, Games = g.ToArray() })
            .Select(e =>
            {
                var schedule = new AllTableSchedule { Time = e.Time };
                foreach (var table in response.Request.RobotGame.Tables)
                {
                    var assignment = e.Games.FirstOrDefault(g => g.Table == table);
                    schedule.Columns.Add((table, assignment == null
                        ? "-"
                        : $"{assignment.Number} - {assignment.Name} ({assignment.Match})"));
                }
                return schedule;
            })
            .ToArray();

        md.AppendLine("### Robot Game Schedule");
        md.AppendLine("|Time|" + string.Join("|", response.Request.RobotGame.Tables) + "|");
        md.AppendLine("|---:|" + string.Concat(Enumerable.Repeat(":---|", response.Request.RobotGame.Tables.Length)));
        foreach (var s in combined)
        {
            md.Append($"|{s.Time:hh\\:mm tt}");
            foreach (var t in s.Columns)
            {
                md.Append($"|{t.team}");
            }
            md.AppendLine("|");
        }
        md.AppendLine();

        foreach (var pod in response.Request.Judging.Pods)
        {
            var podschedule = master
                .Where(s => s.JudgingPod == pod)
                .OrderBy(s => s.JudgingStart)
                .Select(s => new PodEntry
                {
                    JudgingStart = s.JudgingStart,
                    Number = s.Number,
                    Name = s.Name
                })
                .ToArray();

            md.AppendLine($"### Pod {pod} Schedule");
            md.AppendLine("|Time|Number|Name|");
            md.AppendLine("|---:|-----:|:---|");
            foreach (var s in podschedule)
            {
                md.AppendLine($"|{s.JudgingStart:hh\\:mm tt}|{s.Number}|{s.Name}|");
            }
            md.AppendLine();
        }

        foreach (var table in response.Request.RobotGame.Tables)
        {
            var gamesattable = games
                .Where(g => g.Table == table)
                .Select(g => new TableEntry
                {
                    MatchTime = g.MatchTime,
                    Number = g.Number,
                    Name = g.Name,
                    Match = g.Match
                })
                .OrderBy(g => g.MatchTime)
                .ToArray();
            md.AppendLine($"### Table {table} Schedule");
            md.AppendLine("|Match Time|Number|Name|Match|");
            md.AppendLine("|---------:|-----:|:---|:---:|");
            foreach (var s in gamesattable)
            {
                md.AppendLine($"|{s.MatchTime:hh\\:mm tt}|{s.Number}|{s.Name}|{s.Match}");
            }
            md.AppendLine();
        }

        GridsToShow = (MarkupString)Markdown.ToHtml(md.ToString(), new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
    }

    public class AllTableSchedule
    {
        public TimeOnly Time { get; set; }
        public List<(string table, string team)> Columns { get; } = [];
    }

    public class QueuerEntry
    {
        public TimeOnly QueueTime { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public TimeOnly MatchTime { get; set; }
        public string Match { get; set; }
        public string Table { get; set; }
    }

    public class PodEntry
    {
        public TimeOnly JudgingStart { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
    }

    public class TableEntry
    {
        public TimeOnly MatchTime { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string Match { get; set; }
    }

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

    private static List<RequestModel> Profiles = new[]
        {
            (teamcount: 12, tablecount: 2),
            (teamcount: 18, tablecount: 4),
            (teamcount: 24, tablecount: 4),
            (teamcount: 36, tablecount: 4),
            (teamcount: 48, tablecount: 6),
            (teamcount: 60, tablecount: 10)
        }
        .Select(e => BuildRequest(e.teamcount, e.tablecount))
        .ToList();

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
