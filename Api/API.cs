using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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
    [Function(nameof(CalculateSchedule))]
    public static async Task<HttpResponseData> CalculateSchedule([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext executionContext)
    {
        var sw = Stopwatch.StartNew();

        var logger = executionContext.GetLogger("HttpTrigger1");
        logger.LogInformation("CalculateSchedule function processed a request.");

        var request = await req.ReadFromJsonAsync<RequestModel>();

        // validate the incoming request
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(request.Teams, nameof(request.Teams));
        ArgumentNullException.ThrowIfNull(request.Judging, nameof(request.Judging));
        ArgumentNullException.ThrowIfNull(request.Judging.Pods, nameof(request.Judging.Pods));
        ArgumentNullException.ThrowIfNull(request.RobotGame, nameof(request.RobotGame));
        ArgumentNullException.ThrowIfNull(request.RobotGame.Tables, nameof(request.RobotGame.Tables));
        ArgumentNullException.ThrowIfNull(request.Event, nameof(request.Event));
        ArgumentOutOfRangeException.ThrowIfZero(request.Judging.Pods.Length, nameof(request.Judging.Pods));
        ArgumentOutOfRangeException.ThrowIfZero(request.RobotGame.Tables.Length, nameof(request.RobotGame.Tables));
        // Ensure even number of tables specfified
        ArgumentOutOfRangeException.ThrowIfNotEqual(0, request.RobotGame.Tables.Length % 2, nameof(request.RobotGame.Tables));
        // Ensure number of pods can judge the team count
        ArgumentOutOfRangeException.ThrowIfNotEqual(true, request.Judging.Pods.Length >= request.Teams.Length / 6d, nameof(request.Teams));

        //ArgumentOutOfRangeException.ThrowIfZero(config.Judging.CycleTimeMinutes, nameof(config.Judging.CycleTimeMinutes));
        //ArgumentOutOfRangeException.ThrowIfZero(config.RobotGame.CycleTimeMinutes, nameof(config.RobotGame.CycleTimeMinutes));
        //ArgumentOutOfRangeException.ThrowIfZero(config.RobotGame.BreakTimes.Length, nameof(config.RobotGame.BreakTimes));

        var responseModel = ProcessRequest(request);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(responseModel);
        logger.LogMetric("TransactionTimeMS", sw.Elapsed.TotalMilliseconds);
        return response;
    }

    private static ResponseModel ProcessRequest(RequestModel request)
    {
        // always seed with same value for deterministic results
        var rnd = new Random(0);

        // map the incoming teams to the local working class, and randomize
        var teams = request.Teams
            .Select(t => new WorkingTeam { Number = t.Number, Name = t.Name })
            .OrderBy(t => rnd.Next())
            .ToArray();

        // first, assign judging times 
        var slot = request.Judging.StartTime;
        var podcounter = 0;
        foreach (var team in teams)
        {
            team.JudgingStart = slot;
            team.JudgingPod = request.Judging.Pods[podcounter];

            // when podcounter = 0 it's time for a new time slot 
            podcounter = (podcounter + 1) % request.Judging.Pods.Length;
            if (podcounter == 0)
            {
                slot = slot.AddMinutes(request.Judging.CycleTimeMinutes);
                // skip to afternoon if next slot overlaps lunch 
                var end = slot.AddMinutes(request.Judging.CycleTimeMinutes);
                if (end.IsBetween(request.Event.LunchStartTime.Add(TimeSpan.FromSeconds(1)), request.Event.LunchEndTime))
                {
                    slot = request.Event.LunchEndTime;
                }
            }
        }

        // next, assign robot game run times timeslot by timeslot 
        var tablecounter = 0;
        slot = request.RobotGame.StartTime;

        // assign times until all teams are all scheduled
        while (!teams.All(team => team.Match.All(match => match.Assigned)))
        {
            // skip teams that have a conflict with a team's judging time
            var teamsthatcanplaythisslot = teams
                .OrderBy(team => rnd.Next())
                .Where(team => team.Match.Any(match => !match.Assigned))
                .Where(team =>
                {
                    // skip judging + buffer
                    var maxbuffer = Math.Max(request.Judging.BufferMinutes, request.RobotGame.BufferMinutes);
                    var start = team.JudgingStart.AddMinutes(-maxbuffer);
                    var end = team.JudgingStart.AddMinutes(request.Judging.CycleTimeMinutes).AddMinutes(request.Judging.BufferMinutes);
                    return !slot.IsBetween(start, end);
                })
                .Where(team =>
                {
                    // skip teams with time slots already booked + buffer
                    var start = team.Match.Min(m => m.Start);
                    var end = team.Match.Max(m => m.Start.AddMinutes(request.RobotGame.CycleTimeMinutes)).AddMinutes(request.RobotGame.BufferMinutes);
                    return !slot.IsBetween(start, end);
                })
                .OrderBy(team => team.Match.Select((m, i) => new { m, i }).First(e => !e.m.Assigned).i)
                .ToArray();

            // fill all tables for this slot 
            foreach (var team in teamsthatcanplaythisslot)
            {
                // for this team, get the match index of the first available match 
                var match = team.Match.First(m => !m.Assigned);
                match.Start = slot;
                match.Table = request.RobotGame.Tables[tablecounter];
                match.Assigned = true;

                // rotate the table counter to the next table
                tablecounter = (tablecounter + 1) % request.RobotGame.Tables.Length;
                // when tablecounter = 0 it's time for a new time slot 
                if (tablecounter == 0)
                {
                    break;
                }
            }

            // increment to the next timeslot
            slot = slot.AddMinutes(request.RobotGame.CycleTimeMinutes);

            // skip to afternoon if next slot overlaps into lunch 
            var end = slot.AddMinutes(request.RobotGame.CycleTimeMinutes);
            if (end.IsBetween(request.Event.LunchStartTime.AddMinutes(1), request.Event.LunchEndTime))
            {
                slot = request.Event.LunchEndTime;
            }

            // skip break times for break durations 
            foreach (var breaktime in request.RobotGame.BreakTimes)
            {
                end = slot.AddMinutes(request.RobotGame.CycleTimeMinutes);
                if (slot.IsBetween(breaktime, breaktime.AddMinutes(request.RobotGame.BreakDurationMinutes))
                    || end.IsBetween(breaktime.AddMinutes(1), breaktime.AddMinutes(request.RobotGame.BreakDurationMinutes)))
                {
                    slot = breaktime.AddMinutes(request.RobotGame.BreakDurationMinutes);
                    break;
                }
            }
        }

        return new ResponseModel
        {
            Request = request,
            Context = new ScheduleContext
            {
                Name = request.Name,
                Pods = request.Judging.Pods,
                Tables = request.RobotGame.Tables,
                Schedule = teams
                    .Select(team => new TeamSchedule
                    {
                        Number = team.Number,
                        Name = team.Name,
                        JudgingStart = team.JudgingStart,
                        JudgingPod = team.JudgingPod,
                        PracticeStart = team.Match[0].Start,
                        PracticeTable = team.Match[0].Table,
                        Match1Start = team.Match[1].Start,
                        Match1Table = team.Match[1].Table,
                        Match2Start = team.Match[2].Start,
                        Match2Table = team.Match[2].Table,
                        Match3Start = team.Match[3].Start,
                        Match3Table = team.Match[3].Table
                    })
                    .ToArray()
            }
        };
    }
}

class WorkingTeam
{
    public int Number { get; init; }
    public string Name { get; init; }
    public TimeOnly JudgingStart { get; set; }
    public string JudgingPod { get; set; }
    public RobotGameMatch[] Match { get; init; } = [new RobotGameMatch(), new RobotGameMatch(), new RobotGameMatch(), new RobotGameMatch()];
}