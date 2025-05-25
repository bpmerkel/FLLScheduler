namespace ApiIsolated;

/// <summary>
/// Represents a class that handles HTTP triggers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HttpTrigger"/> class.
/// </remarks>
public partial class API
{
    /// <summary>
    /// Runs the HTTP trigger.
    /// </summary>
    /// <param name="req">The HTTP request data.</param>
    /// <param name="executionContext">The execution context.</param>
    /// <returns>The HTTP response data.</returns>
    [Function(nameof(GeneratePDF))]
    public static async Task<HttpResponseData> GeneratePDF([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext executionContext)
    {
        var sw = Stopwatch.StartNew();

        var logger = executionContext.GetLogger("HttpTrigger1");
        logger.LogInformation("GeneratePDF function processed a request.");

        var context = await req.ReadFromJsonAsync<ScheduleContext>();
        var response = req.CreateResponse(HttpStatusCode.OK);
        ProcessPivots(context, response.Body);
        logger.LogMetric("TransactionTimeMS", sw.Elapsed.TotalMilliseconds);
        return response;
    }

    /// <summary>
    /// Processes the pivots and generates a PDF document.
    /// </summary>
    /// <param name="context">The schedule context.</param>
    /// <returns>A memory stream containing the generated PDF document.</returns>
    private static void ProcessPivots(ScheduleContext context, Stream outstream)
    {
        var pivots = new Pivots(context);
        // validate the incoming request
        ArgumentNullException.ThrowIfNull(pivots, nameof(pivots));
        ArgumentOutOfRangeException.ThrowIfZero(pivots.Count, nameof(pivots));

        var eventName = context.Name;
        var logo1 = LoadEmbedded("SUBMERGED.png");
        var logo2 = LoadEmbedded("BOTLogo.png");
        QuestPDF.Settings.License = LicenseType.Community;
        var pdfDoc = Document.Create(container =>
        {
            foreach (var pivotEntry in pivots)
            {
                switch (pivotEntry.Pivot)
                {
                    case PivotType.Registration:
                        GeneratePdfSection<RegistrationEntry>(pivotEntry.Data, pivotEntry.Name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.TeamSchedule:
                        GeneratePdfSection<TeamScheduleEntry>(pivotEntry.Data, pivotEntry.Name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.JudgingQueuingSchedule:
                        GeneratePdfSection<JudgingQueuingEntry>(pivotEntry.Data, pivotEntry.Name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.JudgingSchedule:
                        //GeneratePdfSection<FlexEntry>(pivotEntry.Data, pivotEntry.Name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.PodJudgingSchedule:
                        GeneratePdfSection<PodJudgingEntry>(pivotEntry.Data, pivotEntry.Name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.RobotGameQueuingSchedule:
                        GeneratePdfSection<RobotGameQueuingEntry>(pivotEntry.Data, pivotEntry.Name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.RobotGameSchedule:
                        //GeneratePdfSection<FlexEntry>(pivotEntry.Data, pivotEntry.Name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.RobotGameTableSchedule:
                        GeneratePdfSection<RobotGameTableEntry>(pivotEntry.Data, pivotEntry.Name, eventName, logo1, logo2, container);
                        break;
                    default:
                        throw new ApplicationException();
                };
            }
        });

        pdfDoc.GeneratePdf(outstream);
    }

    /// <summary>
    /// Generates a PDF section for the specified data type.
    /// </summary>
    /// <typeparam name="T">The type of data to generate the section for.</typeparam>
    /// <param name="data">The data array.</param>
    /// <param name="heading">The section heading.</param>
    /// <param name="eventTitle">The event title.</param>
    /// <param name="logoLeft">The left logo image.</param>
    /// <param name="logoRight">The right logo image.</param>
    /// <param name="container">The document container.</param>
    private static void GeneratePdfSection<T>(Array data, string heading, string eventTitle, byte[] logoLeft, byte[] logoRight, IDocumentContainer container)
    {
        var ts = typeof(T);
        var isFlex = typeof(T) == typeof(FlexEntry);

        container.Page(page =>
        {
            var props = ts.GetProperties();
            if (isFlex)
            {
                var flexData = (FlexEntry[])data;
                var flexColumns = flexData.First().Columns;
                page.Size(flexColumns.Length > 10 ? PageSizes.B0.Landscape() : PageSizes.B0.Portrait());
            }
            else
            {
                page.Size(props.Length > 10 ? PageSizes.Letter.Landscape() : PageSizes.Letter.Portrait());
            }
            page.Margin(.5f, Unit.Inch);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Row(row =>
            {
                row.ConstantItem(.75f, Unit.Inch)
                    .Padding(2, Unit.Point)
                    .Image(logoLeft);

                row.RelativeItem()
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(text =>
                    {
                        text.DefaultTextStyle(x =>
                            x.FontSize(16)
                            .SemiBold()
                            .FontColor(Colors.Black)
                            .LineHeight(.85f));
                        text.Line(heading);
                        text.Line(eventTitle)
                            .FontSize(14);
                    });

                row.ConstantItem(1f, Unit.Inch)
                    .Padding(2, Unit.Point)
                    .AlignRight()
                    .AlignMiddle()
                    .Image(logoRight);
            });

            page.Content()
                .Section(heading)
                .Border(1, Unit.Point)
                .BorderColor(Colors.Grey.Lighten3)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        // add a column for the checkbox, and then each property
                        columns.ConstantColumn(.25f, Unit.Inch);
                        if (isFlex)
                        {
                            var flexData = (FlexEntry[])data;
                            var flexColumns = flexData.First().Columns;
                            columns.RelativeColumn();    // add for the time column
                            foreach (var c in flexColumns)
                            {
                                columns.ConstantColumn(2.72f, Unit.Inch);
                            }
                        }
                        else
                        {
                            foreach (var c in props)
                            {
                                if (c.Name == "Name") columns.ConstantColumn(2.72f, Unit.Inch);
                                else columns.RelativeColumn();
                            }
                        }
                    });

                    table.Header(header =>
                    {
                        header.Cell().Row(1).Column(1)
                            .Element(c => HeaderBlock(c))
                            .Text("\u2705") // checked checkbox
                            .FontSize(8)
                            .SemiBold()
                            .FontColor(Colors.Black)
                            .LineHeight(.85f)
                            .AlignCenter();
                        if (isFlex)
                        {
                            var flexData = (FlexEntry[])data;
                            var flexColumns = flexData.First().Columns;
                            header.Cell().Row(1).Column(2)
                                .Element(c => HeaderBlock(c))
                                .Text("Time")
                                .SemiBold()
                                .FontColor(Colors.Black)
                                .LineHeight(.85f)
                                .AlignCenter();

                            for (uint ci = 0; ci < flexColumns.Length; ++ci)
                            {
                                var p = flexColumns[ci];
                                header.Cell().Row(1).Column(ci + 3)
                                    .Element(c => HeaderBlock(c))
                                    .Text(FixHeading(p))
                                    .SemiBold()
                                    .FontColor(Colors.Black)
                                    .LineHeight(.85f)
                                    .AlignCenter();
                            }
                        }
                        else
                        {
                            for (uint ci = 0; ci < props.Length; ++ci)
                            {
                                var p = props[ci];
                                var text = header.Cell().Row(1).Column(ci + 2)
                                    .Element(c => HeaderBlock(c))
                                    .Text(FixHeading(p.Name))
                                    .SemiBold()
                                    .FontColor(Colors.Black)
                                    .LineHeight(.85f);
                                if (p.Name == "Name") text.AlignLeft();
                                else text.AlignCenter();
                            }
                        }
                    });

                    // add the data rows
                    for (uint si = 0; si < data.Length; ++si)
                    {
                        table.Cell().Row(si + 2).Column(1)
                            .Element(c => Block(c, si))
                            .Text("\u2610") // unchecked checkbox
                            .AlignCenter();
                        var s = data.GetValue(si);
                        if (isFlex)
                        {
                            var row = data.GetValue(si) as FlexEntry;
                            var flexColumns = row.Columns;
                            table.Cell().Row(si + 2).Column(2)
                                .Element(c => Block(c, si))
                                .Text($"{row.Time:h:mm tt}")
                                .AlignCenter();

                            for (var ci = 0; ci < flexColumns.Length; ++ci)
                            {
                                var p = flexColumns[ci];
                                table.Cell().Row(si + 2).Column((uint)ci + 3)
                                    .Element(c => Block(c, si))
                                    .Text(row.Row[ci])
                                    .AlignCenter();
                            }
                        }
                        else
                        {
                            for (uint ci = 0; ci < props.Length; ++ci)
                            {
                                var p = props[ci];
                                var text = table.Cell().Row(si + 2).Column(ci + 2)
                                    .Element(c => Block(c, si))
                                    .Text(p.GetValue(s)?.ToString() ?? string.Empty);
                                if (p.Name == "Name") text.AlignLeft();
                                else text.AlignCenter();
                            }
                        }
                    }
                });

            // add a footer
            page.Footer()
                .AlignCenter()
                .Text(x =>
                {
                    x.Span("Page ");
                    x.PageNumberWithinSection(heading);
                });
        });
    }

    /// <summary>
    /// Creates a header block container with specific styling.
    /// </summary>
    /// <param name="container">The container to style.</param>
    /// <returns>The styled container.</returns>
    private static IContainer HeaderBlock(IContainer container) => container
        .Border(1, Unit.Point)
        .BorderColor(Colors.Grey.Lighten3)
        .Background(Colors.Grey.Lighten2)
        .Padding(1, Unit.Point)
        .AlignMiddle();

    /// <summary>
    /// Creates a block container with specific styling.
    /// </summary>
    /// <param name="container">The container to style.</param>
    /// <param name="row">The row index.</param>
    /// <returns>The styled container.</returns>
    private static IContainer Block(IContainer container, uint row) => container
        .Border(1, Unit.Point)
        .BorderColor(Colors.Grey.Lighten3)
        .Background(row % 2 == 0 ? Colors.White : Colors.Grey.Lighten4)
        .Padding(1, Unit.Point)
        .ShowOnce()
        .AlignMiddle();

    /// <summary>
    /// Loads an embedded resource as a byte array.
    /// </summary>
    /// <param name="Name">The name of the embedded resource.</param>
    /// <returns>The byte array of the embedded resource.</returns>
    private static byte[] LoadEmbedded(string Name)
    {
        var a = Assembly.GetCallingAssembly();
        using var stream = a.GetManifestResourceStream(a.GetName().Name + "." + Name);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Fixes the heading by adding spaces between PascalCase words.
    /// </summary>
    /// <param name="heading">The heading to fix.</param>
    /// <returns>The fixed heading.</returns>
    static string FixHeading(string heading)
    {
        var re = PascalCaseRegex();
        return re.Replace(heading, " $1");
    }

    /// <summary>
    /// Gets the regex for matching PascalCase words.
    /// </summary>
    /// <returns>The regex for matching PascalCase words.</returns>
    [GeneratedRegex(@"(?<!^)(?<!-)((?<=\p{Ll})[\p{Lu}\d]|\p{Lu}(?=\p{Ll}))")]
    private static partial Regex PascalCaseRegex();
}
