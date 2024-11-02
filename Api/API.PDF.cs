using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using FLLScheduler.Shared;
using System.IO;
using QuestPDF.Infrastructure;
using System.Reflection;
using QuestPDF.Fluent;
using System.Text.RegularExpressions;
using QuestPDF.Helpers;

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

        var pivots = await req.ReadFromJsonAsync<Pivots>();

        // validate the incoming request
        ArgumentNullException.ThrowIfNull(pivots, nameof(pivots));
        ArgumentOutOfRangeException.ThrowIfZero(pivots.Count, nameof(pivots));

        var response = req.CreateResponse(HttpStatusCode.OK);
        using var ms = ProcessPivots(pivots);
        response.Body = ms;
        logger.LogMetric("TransactionTimeMS", sw.Elapsed.TotalMilliseconds);
        return response;
    }

    private static MemoryStream ProcessPivots(Pivots pivots)
    {
        var eventName = "Manatee Robot Mayhem Practice Tournament";
        var logo1 = LoadEmbedded("SUBMERGED.png");
        var logo2 = LoadEmbedded("BOTLogo.png");
        QuestPDF.Settings.License = LicenseType.Community;
        var pdfDoc = Document.Create(container =>
        {
            foreach (var (name, pivotType, data) in pivots)
            {
                // GeneratePdfSection(data, name, "Manatee Robot Mayhem Practice Tournament", container);
                switch (pivotType)
                {
                    case PivotType.Registration:
                        GeneratePdfSection<RegistrationEntry>(data, name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.TeamSchedule:
                        GeneratePdfSection<TeamScheduleEntry>(data, name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.JudgingQueuingSchedule:
                        GeneratePdfSection<JudgingQueuingEntry>(data, name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.JudgingSchedule:
                        //GeneratePdfSection<RegistrationEntry>(data, name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.PodJudgingSchedule:
                        GeneratePdfSection<PodJudgingEntry>(data, name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.RobotGameQueuingSchedule:
                        GeneratePdfSection<RobotGameQueuingEntry>(data, name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.RobotGameSchedule:
                        //GeneratePdfSection<RegistrationEntry>(FlexEntry.Pivot(data), name, eventName, logo1, logo2, container);
                        break;
                    case PivotType.RobotGameTableSchedule:
                        GeneratePdfSection<RobotGameTableEntry>(data, name, eventName, logo1, logo2, container);
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

        container.Page(page =>
        {
            var props = ts.GetProperties();
            page.Size(props.Length > 10 ? PageSizes.Letter.Landscape() : PageSizes.Letter.Portrait());
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
                //.PaddingVertical(.1f, Unit.Inch)
                .Border(1, Unit.Point)
                .BorderColor(Colors.Grey.Lighten3)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        // add a column for the checkbox, and then each property
                        columns.ConstantColumn(.25f, Unit.Inch);
                        foreach (var c in props)
                        {
                            if (c.Name == "Name") columns.ConstantColumn(2.72f, Unit.Inch);
                            else columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        header.Cell().Row(1).Column(1)
                            .Element(c => HeaderBlock(c))
                            .Text("\u2705")
                            .FontSize(8)
                            .SemiBold()
                            .FontColor(Colors.Black)
                            .LineHeight(.85f)
                            .AlignCenter();
                        for (uint ci = 2; ci <= props.Length + 1; ++ci)
                        {
                            var p = props[ci - 2];
                            var text = header.Cell().Row(1).Column(ci)
                                .Element(c => HeaderBlock(c))
                                .Text(FixHeading(p.Name))
                                .SemiBold()
                                .FontColor(Colors.Black)
                                .LineHeight(.85f);
                            if (p.Name == "Name") text.AlignLeft();
                            else text.AlignCenter();
                        }
                    });

                    // add the data rows
                    for (uint si = 0; si < data.Length; ++si)
                    {
                        table.Cell().Row(si + 2).Column(1)
                            .Element(c => Block(c, si))
                            .Text("\u2610")
                            .AlignCenter();
                        var s = data.GetValue(si);
                        for (uint ci = 2; ci <= props.Length + 1; ++ci)
                        {
                            var p = props[ci - 2];
                            var text = table.Cell().Row(si + 2).Column(ci)
                                .Element(c => Block(c, si))
                                .Text(p.GetValue(s).ToString());
                            if (p.Name == "Name") text.AlignLeft();
                            else text.AlignCenter();
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
        .Padding(2, Unit.Point)
        .AlignMiddle();

    private static IContainer Block(IContainer container, uint row) => container
        .Border(1, Unit.Point)
        .BorderColor(Colors.Grey.Lighten3)
        .Background(row % 2 == 0 ? Colors.White : Colors.Grey.Lighten4)
        .Padding(2, Unit.Point)
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
        var re = new Regex(@"(?<!^)(?<!-)((?<=\p{Ll})[\p{Lu}\d]|\p{Lu}(?=\p{Ll}))");
        return re.Replace(heading, " $1");
    }
}