using FLLScheduler.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
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
    [Inject] private HttpClient HttpClient { get; set; }

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
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

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
        LunchEndTime = Profile.Event.LunchEndTime.ToTimeSpan();
        JudgingStartTime = Profile.Judging.StartTime.ToTimeSpan();
        RobotGamesStartTime = Profile.RobotGame.StartTime.ToTimeSpan();
        CycleTimeMinutes = Profile.Judging.CycleTimeMinutes;
        JudgingBufferMinutes = Profile.Judging.BufferMinutes;
        RobotGameCycleTimeMinutes = Profile.RobotGame.CycleTimeMinutes;
        RobotGameBufferMinutes = Profile.RobotGame.BufferMinutes;
        BreakDurationMinutes = Profile.RobotGame.BreakDurationMinutes;
        PodNames = string.Join(", ", Profile.Judging.Pods);
        TableNames = string.Join(", ", Profile.RobotGame.Tables);
        Breaks = string.Join(", ", Profile.RobotGame.BreakTimes.Select(t => $"{t:h\\:mm tt}"));
        Teams = string.Join(Environment.NewLine, Profile.Teams.Select(t => $"{t.Number}, {t.Name}"));
        await ServerReload();
    }

    private async Task DoUpdateProfile()
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
        profile.Event.LunchEndTime = TimeOnly.FromTimeSpan(LunchEndTime.Value);
        profile.Judging.StartTime = TimeOnly.FromTimeSpan(JudgingStartTime.Value);
        profile.RobotGame.StartTime = TimeOnly.FromTimeSpan(RobotGamesStartTime.Value);
        profile.Judging.CycleTimeMinutes = CycleTimeMinutes;
        profile.Judging.BufferMinutes = JudgingBufferMinutes;
        profile.RobotGame.CycleTimeMinutes = RobotGameCycleTimeMinutes;
        profile.RobotGame.BufferMinutes = RobotGameBufferMinutes;
        profile.RobotGame.BreakDurationMinutes = BreakDurationMinutes;
        profile.Judging.Pods = PodNames.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        profile.RobotGame.Tables = TableNames.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        profile.RobotGame.BreakTimes = Breaks.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(b => TimeOnly.TryParse(b, out TimeOnly t) ? t : TimeOnly.MaxValue)  // midnight if invalid
            .ToArray();
        profile.Teams = Teams.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Split(",;\t ".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(pair => new Team { Number = pair[0], Name = pair[1] })
            .ToArray();
        profile.Name = $"Customized: {profile.Teams.Length} Teams, {profile.Judging.Pods.Length} Judging Pods, {profile.RobotGame.Tables.Length} Game Tables";

        ArgumentOutOfRangeException.ThrowIfNotEqual(0, profile.RobotGame.Tables.Length % 2);    // ensure an even number of tables

        var existing = Profiles.FirstOrDefault(p => p.Name == profile.Name);
        if (existing != null)
        {
            Profiles.Remove(existing);
        }
        Profiles.Insert(0, profile);
        Profile = profile;

        await ServerReload();
    }

    private async Task ServerReload()
    {
        var json = new StringContent(JsonSerializer.Serialize(Profile), Encoding.UTF8, "application/json");
        using var response = await HttpClient.PostAsync("api/CalculateSchedule", json);
        if (response.IsSuccessStatusCode)
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel>();
            ShowResults(responseModel);
        }
    }

    private void ShowResults(ResponseModel response)
    {
        var master = response.Schedule;

        var md = new StringBuilder();
        md.AppendLine($"# {response.Request.Name}{{#name .profile-name .mud-typography .mud-typography-h4}}");
        md.AppendLine($"## Generated {response.GeneratedUtc.ToLocalTime():dddd MMM-dd h\\:mm tt}{{#time .profile-time .mud-typography .mud-typography-h5}}");
        md.AppendLine();

        md.AppendLine("### Registration{.mud-typography .mud-typography-h6}");
        md.AppendLine("{#registration-table .markdown-table}");
        md.AppendLine("|Team|Name|Roster|Coach 1|Coach 2|");
        md.AppendLine("|:--:|:---|-----:|:------|:------|");
        foreach (var s in master.OrderBy(s => s.Number))
        {
            md.AppendLine($"|{s.Number}|{s.Name}| | | |");
        }
        md.AppendLine();

        md.AppendLine("### Team Schedule{.mud-typography .mud-typography-h6}");
        md.AppendLine("{#team-table .markdown-table}");
        md.AppendLine("|Team|Name|Judging|Pod|Practice|Practice Table|Match 1|Match 1 Table|Match 2|Match 2 Table|Match 3|Match 3 Table|");
        md.AppendLine("|:--:|:---|------:|:--|-------:|:------------:|------:|:-----------:|------:|:-----------:|------:|:-----------:|");
        foreach (var s in master.OrderBy(s => s.Number))
        {
            md.Append($"|{s.Number}|{s.Name}|{s.JudgingStart:h\\:mm tt}|{s.JudgingPod}");
            md.Append($"|{s.PracticeStart:h\\:mm tt}|{s.PracticeTable}");
            md.Append($"|{s.Match1Start:h\\:mm tt}|{s.Match1Table}");
            md.Append($"|{s.Match2Start:h\\:mm tt}|{s.Match2Table}");
            md.AppendLine($"|{s.Match3Start:h\\:mm tt}|{s.Match3Table}|");
        }
        md.AppendLine();

        var judgingqueue = master
            .Select(s => new
            {
                QueueTime = s.JudgingStart.AddMinutes(-5),
                s.JudgingStart,
                s.JudgingPod,
                s.Number,
                s.Name
            })
            .OrderBy(s => s.QueueTime)
            .ThenBy(s => s.JudgingPod)
            .ToArray();

        md.AppendLine();
        md.AppendLine("### Judging Queuing Schedule{.mud-typography .mud-typography-h6}");
        md.AppendLine("{#judging-queuer-table .markdown-table}");
        md.AppendLine("|Queue Time|Team|Name|Judging|Pod|");
        md.AppendLine("|---------:|:--:|:---|------:|:--|");
        foreach (var qe in judgingqueue)
        {
            md.AppendLine($"|{qe.QueueTime:h\\:mm tt}|{qe.Number}|{qe.Name}|{qe.JudgingStart:h\\:mm tt}|{qe.JudgingPod}|");
        }
        md.AppendLine();

        var judging = master
            .GroupBy(t => t.JudgingStart)
            .Select(g => new { Time = g.Key, Sessions = g.ToArray() })
            .Select(e =>
            {
                var schedule = new { e.Time, Columns = new List<(string pod, string team)>() };
                foreach (var pod in response.Request.Judging.Pods)
                {
                    var assignment = e.Sessions.FirstOrDefault(g => g.JudgingPod == pod);
                    schedule.Columns.Add((pod, team: assignment == null
                        ? "-"
                        : $"{assignment.Number} - {assignment.Name}"));
                }
                return schedule;
            })
            .ToArray();

        md.AppendLine();
        md.AppendLine("### Judging Schedule{.mud-typography .mud-typography-h6}");
        md.AppendLine("{#pod-table .markdown-table}");
        md.AppendLine("|Time|" + string.Join("|", response.Request.Judging.Pods) + "|");
        md.AppendLine("|---:|" + string.Concat(Enumerable.Repeat(":---:|", response.Request.Judging.Pods.Length)));
        foreach (var s in judging)
        {
            md.Append($"|{s.Time:h\\:mm tt}");
            foreach (var (pod, team) in s.Columns)
            {
                md.Append($"|{team}");
            }
            md.AppendLine("|");
        }
        md.AppendLine();

        foreach (var pod in response.Request.Judging.Pods)
        {
            var podschedule = master
                .Where(s => s.JudgingPod == pod)
                .OrderBy(s => s.JudgingStart)
                .Select(s => new
                {
                    s.JudgingStart,
                    s.Number,
                    s.Name
                })
                .ToArray();

            md.AppendLine();
            md.AppendLine($"### {pod} Judging Schedule{{.mud-typography .mud-typography-h6}}");
            md.AppendLine("{#judging-table .markdown-table}");
            md.AppendLine("|Time|Team|Name|");
            md.AppendLine("|---:|:----:|:---|");
            foreach (var s in podschedule)
            {
                md.AppendLine($"|{s.JudgingStart:h\\:mm tt}|{s.Number}|{s.Name}|");
            }
            md.AppendLine();
        }

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
            .Select(e => new
            {
                QueueTime = e.Time.AddMinutes(-5),
                e.Number,
                e.Name,
                MatchTime = e.Time,
                e.Match,
                e.Table
            })
            .OrderBy(e => e.QueueTime)
            // order tables in the order given in the request
            .ThenBy(e => response.Request.RobotGame.Tables.Select((t, i) => (t, i)).First(ee => ee.t == e.Table).i)
            .ToArray();

        md.AppendLine();
        md.AppendLine("### Robot Game Queuing Schedule{.mud-typography .mud-typography-h6}");
        md.AppendLine("{#queuer-table .markdown-table}");
        md.AppendLine("|Queue Time|Team|Name|Match Time|Match|Table|");
        md.AppendLine("|---------:|:--:|:---|---------:|:---:|:----|");
        foreach (var qe in games)
        {
            md.AppendLine($"|{qe.QueueTime:h\\:mm tt}|{qe.Number}|{qe.Name}|{qe.MatchTime:h\\:mm tt}|{qe.Match}|{qe.Table}|");
        }
        md.AppendLine();

        var combined = games
            .GroupBy(game => game.MatchTime)
            .Select(g => new { Time = g.Key, Games = g.ToArray() })
            .Select(e =>
            {
                var schedule = new { e.Time, Columns = new List<(string table, string team)>() };
                foreach (var table in response.Request.RobotGame.Tables)
                {
                    var assignment = e.Games.FirstOrDefault(g => g.Table == table);
                    schedule.Columns.Add((table, team: assignment == null
                        ? "-"
                        : $"{assignment.Number} - {assignment.Name} ({assignment.Match})"));
                }
                return schedule;
            })
            .ToArray();

        md.AppendLine();
        md.AppendLine("### Robot Game Schedule{.mud-typography .mud-typography-h6}");
        md.AppendLine("{#game-table .markdown-table}");
        md.AppendLine("|Time|" + string.Join("|", response.Request.RobotGame.Tables) + "|");
        md.AppendLine("|---:|" + string.Concat(Enumerable.Repeat(":---:|", response.Request.RobotGame.Tables.Length)));
        foreach (var s in combined)
        {
            md.Append($"|{s.Time:h\\:mm tt}");
            foreach (var (table, team) in s.Columns)
            {
                md.Append($"|{team}");
            }
            md.AppendLine("|");
        }
        md.AppendLine();

        foreach (var table in response.Request.RobotGame.Tables)
        {
            var gamesattable = games
                .Where(g => g.Table == table)
                .Select(g => new
                {
                    g.MatchTime,
                    g.Number,
                    g.Name,
                    g.Match
                })
                .OrderBy(g => g.MatchTime)
                .ToArray();
            md.AppendLine();
            md.AppendLine($"### {table} Robot Game Table Schedule{{.mud-typography .mud-typography-h6}}");
            md.AppendLine("{#game-table-table .markdown-table}");
            md.AppendLine("|Match Time|Team|Name|Match|");
            md.AppendLine("|---------:|:----:|:---|:---:|");
            foreach (var s in gamesattable)
            {
                md.AppendLine($"|{s.MatchTime:h\\:mm tt}|{s.Number}|{s.Name}|{s.Match}");
            }
            md.AppendLine();
        }

        GridsToShow = (MarkupString)Markdown.ToHtml(md.ToString(), pipeline);
    }

    private async Task DoExport()
    {
        await Task.Delay(1);
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

    private static readonly List<RequestModel> Profiles = new[]
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
                LunchEndTime = TimeOnly.Parse("1:00 pm")
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
