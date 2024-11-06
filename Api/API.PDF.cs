using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using FLLScheduler.Shared;

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
    /// <returns>The HTTP response data.</returns>
    [Function(nameof(GeneratePDF))]
    public static async Task<HttpResponseData> GeneratePDF([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext executionContext)
    {
        var sw = Stopwatch.StartNew();

        var logger = executionContext.GetLogger("HttpTrigger1");
        logger.LogInformation("GeneratePDF function processed a request.");

        var context = await req.ReadFromJsonAsync<ScheduleContext>();
        var response = req.CreateResponse(HttpStatusCode.OK);
        using var ms = ProcessPivots(context);
        ms.Position = 0;
        await ms.CopyToAsync(response.Body);
        logger.LogMetric("TransactionTimeMS", sw.Elapsed.TotalMilliseconds);
        return response;
    }

    private static MemoryStream ProcessPivots(ScheduleContext context)
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

        var ms = new MemoryStream();
        pdfDoc.GeneratePdf(ms);
        ms.Flush();
        return ms;
    }

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

    private static IContainer HeaderBlock(IContainer container) => container
        .Border(1, Unit.Point)
        .BorderColor(Colors.Grey.Lighten3)
        .Background(Colors.Grey.Lighten2)
        .Padding(1, Unit.Point)
        .AlignMiddle();

    private static IContainer Block(IContainer container, uint row) => container
        .Border(1, Unit.Point)
        .BorderColor(Colors.Grey.Lighten3)
        .Background(row % 2 == 0 ? Colors.White : Colors.Grey.Lighten4)
        .Padding(1, Unit.Point)
        .ShowOnce()
        .AlignMiddle();

    private static byte[] LoadEmbedded(string Name)
    {
        var a = Assembly.GetCallingAssembly();
        using var stream = a.GetManifestResourceStream(a.GetName().Name + "." + Name);
        var buf = new byte[stream.Length];
        stream.Read(buf, 0, buf.Length);
        return buf;
    }

    static string FixHeading(string heading)
    {
        var re = PascalCaseRegex();
        return re.Replace(heading, " $1");
    }

    [GeneratedRegex(@"(?<!^)(?<!-)((?<=\p{Ll})[\p{Lu}\d]|\p{Lu}(?=\p{Ll}))")]
    private static partial Regex PascalCaseRegex();
}