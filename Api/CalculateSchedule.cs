using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FLLScheduler.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ApiIsolated;

/// <summary>
/// Represents a class that handles HTTP triggers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HttpTrigger"/> class.
/// </remarks>
/// <param name="loggerFactory">The logger factory.</param>
public class CalculateSchedule
{
    private readonly ILogger _logger;

    public CalculateSchedule(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CalculateSchedule>();
    }

    /// <summary>
    /// Runs the HTTP trigger.
    /// </summary>
    /// <param name="req">The HTTP request data.</param>
    /// <returns>The HTTP response data.</returns>
    [Function(nameof(CalculateSchedule))]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var sw = Stopwatch.StartNew();
        var config = await req.ReadFromJsonAsync<RequestModel>();

        // validate the incoming request
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        ArgumentNullException.ThrowIfNull(config.Teams, nameof(config.Teams));
        ArgumentNullException.ThrowIfNull(config.Judging, nameof(config.Judging));
        ArgumentNullException.ThrowIfNull(config.Judging.Pods, nameof(config.Judging.Pods));
        ArgumentNullException.ThrowIfNull(config.RobotGame, nameof(config.RobotGame));
        ArgumentNullException.ThrowIfNull(config.RobotGame.Tables, nameof(config.RobotGame.Tables));
        ArgumentNullException.ThrowIfNull(config.Event, nameof(config.Event));
        ArgumentOutOfRangeException.ThrowIfZero(config.Judging.Pods.Length, nameof(config.Judging.Pods));
        ArgumentOutOfRangeException.ThrowIfZero(config.RobotGame.Tables.Length, nameof(config.RobotGame.Tables));
        ArgumentOutOfRangeException.ThrowIfZero(config.RobotGame.CycleTimeMinutes, nameof(config.RobotGame.CycleTimeMinutes));
        ArgumentOutOfRangeException.ThrowIfZero(config.RobotGame.BreakTimes.Length, nameof(config.RobotGame.BreakTimes));

        var teams = config.Teams;

        // first, assign judging times 
        var slot = config.Judging.StartTime;
        var podcounter = 0;
        foreach (var team in teams)
        {
            team.JudgingStart = slot;
            //team.JudgingEnd = slot.Add(state.judging.cycletime); 
            team.JudgingPod = config.Judging.Pods[podcounter];
            podcounter = (podcounter + 1) % config.Judging.Pods.Length;

            // when podcounter = 0 it's time for a new time slot 
            if (podcounter == 0)
            {
                slot = slot.AddMinutes(config.Judging.CycleTimeMinutes);
                // skip to afternoon if next slot overlaps into lunch 
                var end = slot.AddMinutes(config.Judging.CycleTimeMinutes);
                if (end.IsBetween(config.Event.LunchStartTime.Add(TimeSpan.FromSeconds(1)), config.Event.AfternoonStartTime))
                {
                    slot = config.Event.AfternoonStartTime;
                }
            }
        }

        // next, assign robot game run times 
        // but do this timeslot by timeslot 
        var tablecounter = 0;
        slot = config.RobotGame.StartTime;
        var rnd = new Random();
        for (;;)
        {
            // skip teams that have a conflict with a team's judging time 
            var teamsthatcanplaythisslot = teams
                .Where(team => team.Match.Any(match => match.Table == null))
                .Where(team =>
                {
                    // skip judging + buffer 
                    var start = team.JudgingStart.AddMinutes(-config.Judging.BufferMinutes).AddMinutes(-config.RobotGame.BufferMinutes);
                    var end = team.JudgingStart.AddMinutes(config.Judging.CycleTimeMinutes).AddMinutes(config.Judging.BufferMinutes);
                    return !slot.IsBetween(start, end);
                })
                .Where(team =>
                {
                    // skip teams with times already booked 
                    var start = team.Match.Min(m => m.Start).AddMinutes(-config.RobotGame.BufferMinutes);
                    var end = team.Match.Max(m => m.Start.AddMinutes(config.RobotGame.CycleTimeMinutes)).AddMinutes(config.RobotGame.BufferMinutes);
                    return !slot.IsBetween(start, end);
                })
                .OrderBy(team => team.Match.Select((m, i) => new { m, i }).First(e => e.m.Table == null).i)
                .ThenBy(team => rnd.Next())
                .ToArray();

            // break when all teams are all scheduled 
            if (teams.All(team => team.Match.All(match => match.Table != null)))
            {
                break;
            }

            // fill all tables for this slot 
            foreach (var team in teamsthatcanplaythisslot)
            {
                // for this team, get the match of the first null table 
                var match = team.Match.Select((m, i) => new { m, i }).First(e => e.m.Table == null).i;
                team.Match[match].Start = slot;
                //team.Match[match].RunEnd = slot.Add(state.robotgame.cycletime); 
                team.Match[match].Table = config.RobotGame.Tables[tablecounter];

                tablecounter = (tablecounter + 1) % config.RobotGame.Tables.Length;
                // when tablecounter = 0 it's time for a new time slot 
                if (tablecounter == 0)
                {
                    break;
                }
            }

            slot = slot.AddMinutes(config.RobotGame.CycleTimeMinutes);

            // skip to afternoon if next slot overlaps into lunch 
            var end = slot.AddMinutes(config.RobotGame.CycleTimeMinutes);
            if (end.IsBetween(config.Event.LunchStartTime.Add(TimeSpan.FromSeconds(1)), config.Event.AfternoonStartTime))
            {
                slot = config.Event.AfternoonStartTime;
            }

            // skip break times for break durations 
            foreach (var @break in config.RobotGame.BreakTimes)
            {
                end = slot.AddMinutes(config.RobotGame.CycleTimeMinutes);
                if (slot.IsBetween(@break, @break.AddMinutes(config.RobotGame.BreakDurationMinutes))
                    || end.IsBetween(@break, @break.AddMinutes(config.RobotGame.BreakDurationMinutes)))
                {
                    slot = @break.AddMinutes(config.RobotGame.BreakDurationMinutes);
                    break;
                }
            }
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new ResponseModel
        {
            Teams = teams
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
        });
        _logger.LogMetric("TransactionTimeMS", sw.Elapsed.TotalMilliseconds);
        return response;
    }
}
