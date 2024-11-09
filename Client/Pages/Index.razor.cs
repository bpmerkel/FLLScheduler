namespace FLLScheduler.Pages;

/// <summary>
/// Represents the main page of the application.
/// </summary>
public partial class Index : IBrowserViewportObserver, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the dialog service.
    /// </summary>
    [Inject] private IDialogService DialogService { get; set; }

    /// <summary>
    /// Gets or sets the browser viewport service.
    /// </summary>
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; }

    /// <summary>
    /// Gets or sets the HTTP client factory.
    /// </summary>
    [Inject] private IHttpClientFactory ClientFactory { get; set; }

    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    private RequestModel Profile { get; set; }

    /// <summary>
    /// Gets or sets the registration time.
    /// </summary>
    private TimeSpan? RegistrationTime { get; set; }

    /// <summary>
    /// Gets or sets the coaches meeting time.
    /// </summary>
    private TimeSpan? CoachesMeetingTime { get; set; }

    /// <summary>
    /// Gets or sets the opening ceremony time.
    /// </summary>
    private TimeSpan? OpeningCeremonyTime { get; set; }

    /// <summary>
    /// Gets or sets the lunch start time.
    /// </summary>
    private TimeSpan? LunchStartTime { get; set; }

    /// <summary>
    /// Gets or sets the lunch end time.
    /// </summary>
    private TimeSpan? LunchEndTime { get; set; }

    /// <summary>
    /// Gets or sets the judging start time.
    /// </summary>
    private TimeSpan? JudgingStartTime { get; set; }

    /// <summary>
    /// Gets or sets the robot games start time.
    /// </summary>
    private TimeSpan? RobotGamesStartTime { get; set; }

    /// <summary>
    /// Gets or sets the cycle time in minutes.
    /// </summary>
    private int CycleTimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets the judging buffer minutes.
    /// </summary>
    private int JudgingBufferMinutes { get; set; }

    /// <summary>
    /// Gets or sets the robot game cycle time in minutes.
    /// </summary>
    private int RobotGameCycleTimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets the robot game buffer minutes.
    /// </summary>
    private int RobotGameBufferMinutes { get; set; }

    /// <summary>
    /// Gets or sets the break duration in minutes.
    /// </summary>
    private int BreakDurationMinutes { get; set; }

    /// <summary>
    /// Gets or sets the pod names.
    /// </summary>
    private string PodNames { get; set; }

    /// <summary>
    /// Gets or sets the total number of pods.
    /// </summary>
    private int TotalPods { get; set; } = 1;

    /// <summary>
    /// Gets or sets the breaks.
    /// </summary>
    private string Breaks { get; set; }

    /// <summary>
    /// Gets or sets the table names.
    /// </summary>
    private string TableNames { get; set; }

    /// <summary>
    /// Gets or sets the total number of tables.
    /// </summary>
    private int TotalTables { get; set; } = 2;

    /// <summary>
    /// Gets or sets the teams.
    /// </summary>
    private string Teams { get; set; }

    /// <summary>
    /// Gets the list of errors.
    /// </summary>
    private List<string> Errors { get; init; } = [];

    /// <summary>
    /// Gets or sets the grids to show.
    /// </summary>
    private MarkupString GridsToShow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the schedule is being generated.
    /// </summary>
    private bool Generating { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the Excel export is in progress.
    /// </summary>
    private bool ExportingExcel { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the PDF export is in progress.
    /// </summary>
    private bool ExportingPdf { get; set; } = false;

    /// <summary>
    /// Gets the predefined table names.
    /// </summary>
    private readonly string[] Tables = [ "Atlantic", "Pacific", "Indian", "Arctic", "Southern", "Procellarum", "Boreum", "Europa", "Enceladus", "Ganymede", "Titan", "Callisto" ];

    /// <summary>
    /// Gets the Markdown pipeline.
    /// </summary>
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <summary>
    /// Gets or sets the response model.
    /// </summary>
    private ResponseModel Response;

    /// <summary>
    /// Called after the component has rendered.
    /// </summary>
    /// <param name="firstRender">Indicates whether this is the first render.</param>
    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            await DoProfileSelected(Profiles[0]);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles the profile selection.
    /// </summary>
    /// <param name="value">The selected profile.</param>
    private async Task DoProfileSelected(RequestModel value)
    {
        Generating = true;
        await Task.Run(async () =>
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
            TotalPods = Profile.Judging.Pods.Length;
            TableNames = string.Join(", ", Profile.RobotGame.Tables);
            TotalTables = Profile.RobotGame.Tables.Length;
            Breaks = string.Join(", ", Profile.RobotGame.BreakTimes.Select(t => $"{t:h\\:mm tt}"));
            Teams = string.Join(Environment.NewLine, Profile.Teams.Select(t => $"{t.Number}, {t.Name}"));
            await ServerReload();
            Generating = false;
        });
    }

    /// <summary>
    /// Updates the schedule based on the modifications in the UI.
    /// </summary>
    private async Task DoUpdateSchedule()
    {
        Generating = true;
        await Task.Run(async () =>
        {
            var profile = new RequestModel
            {
                Event = new EventConfig
                {
                    RegistrationTime = TimeOnly.FromTimeSpan(RegistrationTime.Value),
                    CoachesMeetingTime = TimeOnly.FromTimeSpan(CoachesMeetingTime.Value),
                    OpeningCeremonyTime = TimeOnly.FromTimeSpan(OpeningCeremonyTime.Value),
                    LunchStartTime = TimeOnly.FromTimeSpan(LunchStartTime.Value),
                    LunchEndTime = TimeOnly.FromTimeSpan(LunchEndTime.Value)
                },
                Judging = new JudgingConfig
                {
                    StartTime = TimeOnly.FromTimeSpan(JudgingStartTime.Value),
                    CycleTimeMinutes = CycleTimeMinutes,
                    BufferMinutes = JudgingBufferMinutes,
                    Pods = PodNames.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                },
                RobotGame = new RobotGameConfig
                {
                    StartTime = TimeOnly.FromTimeSpan(RobotGamesStartTime.Value),
                    CycleTimeMinutes = RobotGameCycleTimeMinutes,
                    BufferMinutes = RobotGameBufferMinutes,
                    BreakDurationMinutes = BreakDurationMinutes,
                    Tables = TableNames.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    BreakTimes = Breaks.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(b => TimeOnly.TryParse(b, out TimeOnly t) ? t : TimeOnly.MaxValue)
                        .ToArray()
                },
                Teams = Teams.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.Split(",;\t ".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Select(pair => new Team { Number = Convert.ToInt32(pair[0]), Name = pair[1] })
                    .ToArray()
            };
            profile.Name = $"Customized: {profile.Teams.Length} Teams, {profile.Judging.Pods.Length} Judging Pods, {profile.RobotGame.Tables.Length} Game Tables";

            var existing = Profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existing != null)
            {
                Profiles.Remove(existing);
            }
            Profiles.Insert(0, profile);
            Profile = profile;

            await ServerReload();
            Generating = false;
        });
    }

    /// <summary>
    /// Identifies the number of pods.
    /// </summary>
    /// <param name="podcount">The number of pods.</param>
    private void DoIdentifyPods(int podcount)
    {
        TotalPods = podcount;
        PodNames = string.Join(", ", Enumerable.Range(1, podcount).Select(i => $"Pod {i}"));
    }

    /// <summary>
    /// Identifies the number of tables.
    /// </summary>
    /// <param name="tablecount">The number of tables.</param>
    private void DoIdentifyTables(int tablecount)
    {
        TotalTables = tablecount;
        TableNames = string.Join(", ", Tables[..tablecount]);
    }

    /// <summary>
    /// Reloads the server data.
    /// </summary>
    private async Task ServerReload()
    {
        if (!ConfigIsValid())
        {
            return;
        }

        var httpClient = ClientFactory.CreateClient("API");
        using var response = await httpClient.PostAsJsonAsync("api/CalculateSchedule", Profile);
        if (response.IsSuccessStatusCode)
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel>();
            Response = responseModel;
            ShowResults();
        }
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>True if the configuration is valid; otherwise, false.</returns>
    private bool ConfigIsValid()
    {
        Errors.Clear();
        if (Profile == null) Errors.Add("Invalid configuration");
        else if (Profile.Event == null) Errors.Add("Invalid Event configuration");
        else if (Profile.Teams == null) Errors.Add("Invalid Teams configuration");

        if (Profile.Judging == null) Errors.Add("Invalid Judging configuration");
        else if (Profile.Judging.Pods == null) Errors.Add("Invalid Judging Pods configuration");
        else if (Profile.Judging.Pods.Length == 0) Errors.Add("Invalid Judging Pods configuration");
        else if (Profile.Judging.Pods.Length < Profile.Teams.Length / 6d) Errors.Add($"Invalid Judging Pods configuration -- each pod can judge no more than 6 teams, so you need at least {Math.Ceiling(Profile.Teams.Length / 6d):0} judging pods");

        if (Profile.RobotGame == null) Errors.Add("Invalid Robot Game configuration");
        else if (Profile.RobotGame.Tables == null) Errors.Add("Invalid Robot Game Tables configuration");
        else if (Profile.RobotGame.Tables.Length == 0) Errors.Add("Invalid Robot Game Tables configuration");
        else if (Profile.RobotGame.Tables.Length % 2 != 0) Errors.Add("Invalid Robot Game Tables configuration -- you need an even number of tables");

        return Errors.Count == 0;
    }

    /// <summary>
    /// Shows the results.
    /// </summary>
    private void ShowResults()
    {
        var pivots = new Pivots(Response.Context);
        var md = new StringBuilder();
        md.AppendLine($"# {Response.Request.Name}{{#name .profile-name .mud-typography .mud-typography-h4}}");
        md.AppendLine($"## Generated {Response.GeneratedUtc.ToLocalTime():dddd MMM-dd h\\:mm tt}{{#time .profile-time .mud-typography .mud-typography-h5}}");
        md.AppendLine();

        foreach (var entry in pivots)
        {
            switch (entry.Pivot)
            {
                case PivotType.Registration:
                    md.AppendLine("### Registration{.mud-typography .mud-typography-h6}");
                    md.AppendLine("{#registration-table .markdown-table}");
                    md.AppendLine("|Team|Name|Roster|Coach 1|Coach 2|");
                    md.AppendLine("|:--:|:---|-----:|:------|:------|");
                    foreach (var s in entry.Data.Cast<RegistrationEntry>())
                    {
                        md.AppendLine($"|{s.Team}|{s.Name}| | | |");
                    }
                    break;
                case PivotType.TeamSchedule:
                    md.AppendLine("### Team Schedule{.mud-typography .mud-typography-h6}");
                    md.AppendLine("{#team-table .markdown-table}");
                    md.AppendLine("|Team|Name|Judging|Pod|Practice|Practice Table|Match 1|Match 1 Table|Match 2|Match 2 Table|Match 3|Match 3 Table|");
                    md.AppendLine("|:--:|:---|------:|:--|-------:|:------------:|------:|:-----------:|------:|:-----------:|------:|:-----------:|");
                    foreach (var s in entry.Data.Cast<TeamScheduleEntry>())
                    {
                        md.Append($"|{s.Team}|{s.Name}|{s.Judging:h\\:mm tt}|{s.Pod}");
                        md.Append($"|{s.Practice:h\\:mm tt}|{s.PracticeTable}");
                        md.Append($"|{s.Match1:h\\:mm tt}|{s.Match1Table}");
                        md.Append($"|{s.Match2:h\\:mm tt}|{s.Match2Table}");
                        md.AppendLine($"|{s.Match3:h\\:mm tt}|{s.Match3Table}|");
                    }
                    break;
                case PivotType.JudgingQueuingSchedule:
                    md.AppendLine("### Judging Queuing Schedule{.mud-typography .mud-typography-h6}");
                    md.AppendLine("{#judging-queuer-table .markdown-table}");
                    md.AppendLine("|Queue Time|Team|Name|Judging|Pod|");
                    md.AppendLine("|---------:|:--:|:---|------:|:--|");
                    foreach (var s in entry.Data.Cast<JudgingQueuingEntry>())
                    {
                        md.AppendLine($"|{s.QueueTime:h\\:mm tt}|{s.Team}|{s.Name}|{s.Judging:h\\:mm tt}|{s.Pod}|");
                    }
                    break;
                case PivotType.JudgingSchedule:
                    md.AppendLine("### Judging Schedule{.mud-typography .mud-typography-h6}");
                    md.AppendLine("{#pod-table .markdown-table}");
                    md.AppendLine("|Time|" + string.Join("|", Response.Request.Judging.Pods) + "|");
                    md.AppendLine("|---:|" + string.Concat(Enumerable.Repeat(":---:|", Response.Request.Judging.Pods.Length)));
                    foreach (var s in entry.Data.Cast<FlexEntry>())
                    {
                        md.Append($"|{s.Time:h\\:mm tt}");
                        foreach (var team in s.Row)
                        {
                            md.Append($"|{team}");
                        }
                        md.AppendLine("|");
                    }
                    break;
                case PivotType.PodJudgingSchedule:
                    md.AppendLine($"### {entry.Name}{{.mud-typography .mud-typography-h6}}");
                    md.AppendLine("{#judging-table .markdown-table}");
                    md.AppendLine("|Time|Team|Name|");
                    md.AppendLine("|---:|:----:|:---|");
                    foreach (var s in entry.Data.Cast<PodJudgingEntry>())
                    {
                        md.AppendLine($"|{s.Time:h\\:mm tt}|{s.Team}|{s.Name}|");
                    }
                    break;
                case PivotType.RobotGameQueuingSchedule:
                    md.AppendLine("### Robot Game Queuing Schedule{.mud-typography .mud-typography-h6}");
                    md.AppendLine("{#queuer-table .markdown-table}");
                    md.AppendLine("|Queue Time|Team|Name|Match Time|Match|Table|");
                    md.AppendLine("|---------:|:--:|:---|---------:|:---:|:----|");
                    foreach (var qe in entry.Data.Cast<RobotGameQueuingEntry>())
                    {
                        md.AppendLine($"|{qe.QueueTime:h\\:mm tt}|{qe.Team}|{qe.Name}|{qe.MatchTime:h\\:mm tt}|{qe.Match}|{qe.Table}|");
                    }
                    break;
                case PivotType.RobotGameSchedule:
                    md.AppendLine("### Robot Game Schedule{.mud-typography .mud-typography-h6}");
                    md.AppendLine("{#game-table .markdown-table}");
                    md.AppendLine("|Time|" + string.Join("|", Response.Request.RobotGame.Tables) + "|");
                    md.AppendLine("|---:|" + string.Concat(Enumerable.Repeat(":---:|", Response.Request.RobotGame.Tables.Length)));
                    foreach (var s in entry.Data.Cast<FlexEntry>())
                    {
                        md.Append($"|{s.Time:h\\:mm tt}");
                        foreach (var team in s.Row)
                        {
                            md.Append($"|{team}");
                        }
                        md.AppendLine("|");
                    }
                    break;
                case PivotType.RobotGameTableSchedule:
                    md.AppendLine($"### {entry.Name}{{.mud-typography .mud-typography-h6}}");
                    md.AppendLine("{#game-table-table .markdown-table}");
                    md.AppendLine("|Match Time|Team|Name|Match|");
                    md.AppendLine("|---------:|:----:|:---|:---:|");
                    foreach (var s in entry.Data.Cast<RobotGameTableEntry>())
                    {
                        md.AppendLine($"|{s.MatchTime:h\\:mm tt}|{s.Team}|{s.Name}|{s.Match}");
                    }
                    break;
            }
            md.AppendLine();
        }

        GridsToShow = (MarkupString)Markdown.ToHtml(md.ToString(), pipeline);
        StateHasChanged();
    }

    [Inject] IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

    // export to PDF from the API layer
    private async Task DoExportPdf()
    {
        if (Response == null) return;
        ExportingPdf = true;

        await Task.Run(async () =>
        {
            // send request to server to generate PDF and download
            var httpClient = ClientFactory.CreateClient("API");
            using var response = await httpClient.PostAsJsonAsync("api/GeneratePDF", Response.Context);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await BlazorDownloadFileService.DownloadFile("Schedules.pdf", bytes, "application/pdf");
            }
            ExportingPdf = false;
        });
    }

    // export to Excel using ClosedXml
    private async Task DoExportExcel()
    {
        if (Response == null) return;
        ExportingExcel = true;

        await Task.Run(async () =>
        {
            var pivots = new Pivots(Response.Context);
            var wb = new XLWorkbook();
            foreach (var pivotEntry in pivots)
            {
                var wsname = pivotEntry.Name.Replace(" Schedule", string.Empty);
                var ws = wb.AddWorksheet(wsname);
                var cell = ws.Cell(1, 1);

                IXLTable table;
                switch (pivotEntry.Pivot)
                {
                    case PivotType.Registration:
                        table = cell.InsertTable(pivotEntry.Data.Cast<RegistrationEntry>(), true);
                        break;
                    case PivotType.TeamSchedule:
                        table = cell.InsertTable(pivotEntry.Data.Cast<TeamScheduleEntry>(), true);
                        break;
                    case PivotType.JudgingQueuingSchedule:
                        table = cell.InsertTable(pivotEntry.Data.Cast<JudgingQueuingEntry>(), true);
                        break;
                    case PivotType.JudgingSchedule:
                        table = cell.InsertTable(FlexEntry.Pivot(pivotEntry.Data), true);
                        ClosedXMLHelpers.FixFlexTable(table);
                        break;
                    case PivotType.PodJudgingSchedule:
                        table = cell.InsertTable(pivotEntry.Data.Cast<PodJudgingEntry>(), true);
                        break;
                    case PivotType.RobotGameQueuingSchedule:
                        table = cell.InsertTable(pivotEntry.Data.Cast<RobotGameQueuingEntry>(), true);
                        break;
                    case PivotType.RobotGameSchedule:
                        table = cell.InsertTable(FlexEntry.Pivot(pivotEntry.Data), true);
                        ClosedXMLHelpers.FixFlexTable(table);
                        break;
                    case PivotType.RobotGameTableSchedule:
                        table = cell.InsertTable(pivotEntry.Data.Cast<RobotGameTableEntry>(), true);
                        break;
                    default:
                        throw new ApplicationException();
                };

                ClosedXMLHelpers.FixStyles(table);
                table.SetShowAutoFilter(false);
                ws.Columns().AdjustToContents();
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            await BlazorDownloadFileService.DownloadFile("Schedules.xlsx", ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            ms.Flush();
            ExportingExcel = false;
        });
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
        (teamcount: 6, tablecount: 2),
        (teamcount: 12, tablecount: 2),
        (teamcount: 18, tablecount: 4),
        (teamcount: 24, tablecount: 4),
        (teamcount: 36, tablecount: 6),
        (teamcount: 48, tablecount: 6),
        (teamcount: 60, tablecount: 10)
    }
    .Select(e => BuildProfile(e.teamcount, e.tablecount))
    .ToList();

    private static RequestModel BuildProfile(int teamcount, int tablecount)
    {
        var allteams = Enumerable.Range(1001, teamcount)
            .Select(i => new Team { Number = i, Name = $"team {i:0000}" })
            .ToArray();
        var podcount = Convert.ToInt32(Math.Ceiling(teamcount / 6d));   // always a max of 6 teams judged per pod
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
                CycleTimeMinutes = allteams.Length <= 12 ? 8
                                : allteams.Length <= 24 ? 10
                                : allteams.Length <= 48 ? 15
                                : 20,
                BufferMinutes = 15,
                BreakTimes = [TimeOnly.Parse("10:00 am"), TimeOnly.Parse("11:00 am"), TimeOnly.Parse("2:00 pm")],
                BreakDurationMinutes = 10
            },
            Teams = allteams
        };
    }
}

public static partial class ClosedXMLHelpers
{
    public static void FixFlexTable(IXLTable table)
    {
        var range = table.AsRange();
        for (var rowidx = 1; rowidx <= range.RowCount(); rowidx++)
        {
            if (rowidx == 1)
            {
                // set row 1 to be column names from row 2
                var targetrow = range.Row(1);
                var sourcerow = range.Row(2);
                for (var cidx = 1; cidx <= targetrow.CellCount(); cidx++)
                {
                    var ct = targetrow.Cell(cidx);
                    var cs = sourcerow.Cell(cidx);
                    var values = cs.GetValue<string>().Split("[];,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    ct.SetValue(values[0]);
                }
            }
            else
            {
                // convert the value
                var row = range.Row(rowidx);
                foreach (var c in row.Cells())
                {
                    var values = c.GetValue<string>().Split("[];,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    c.SetValue(values[1]);
                }
            }
        }
    }

    public static void FixStyles(IXLTable table)
    {
        var headerrow = table.Row(1);

        foreach (var cell in headerrow.CellsUsed())
        {
            // convert the cell value to text split by pascal casing
            var value = cell.GetValue<string>();
            var fixedup = PascalCaseRegex().Replace(value, " $1");
            cell.SetValue(fixedup);
        }

        // walk all cells and convert the cell to numeric or DateTime
        var cells = table.CellsUsed();
        foreach (var cell in cells)
        {
            var value = cell.GetValue<string>();
            if (int.TryParse(value, out int numericValue))
            {
                cell.SetValue(numericValue);
                cell.Style.NumberFormat.Format = "#####0";
            }
            else if (DateTime.TryParse(value, out DateTime timeValue))
            {
                cell.SetValue(timeValue);
                cell.Style.NumberFormat.Format = "h:mm AM/PM";
            }
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    [GeneratedRegex(@"(?<!^)(?<!-)((?<=\p{Ll})[\p{Lu}\d]|\p{Lu}(?=\p{Ll}))")]
    private static partial Regex PascalCaseRegex();
}