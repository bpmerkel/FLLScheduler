namespace FLLScheduler.Shared;

public enum PivotType
{
    Registration,
    TeamSchedule,
    JudgingQueuingSchedule,
    JudgingSchedule,
    PodJudgingSchedule,
    RobotGameQueuingSchedule,
    RobotGameSchedule,
    RobotGameTableSchedule
}

public class Pivots : List<(string pivot, PivotType pivotType, Array data)>
{
    public Pivots(RequestModel request, TeamSchedule[] master)
    {
        Add(("Registration", PivotType.Registration, master
            .OrderBy(s => s.Number)
            .Select(s => new RegistrationEntry
            {
                Team = s.Number,
                Name = s.Name,
                Roster = string.Empty
            })
            .ToArray()));

        Add(("Team Schedule", PivotType.TeamSchedule, master
            .OrderBy(s => s.Number)
            .Select(s => new TeamScheduleEntry
            {
                Team = s.Number,
                Name = s.Name,
                Judging = s.JudgingStart,
                Pod = s.JudgingPod,
                Practice = s.PracticeStart,
                PracticeTable = s.PracticeTable,
                Match1 = s.Match1Start,
                Match1Table = s.Match1Table,
                Match2 = s.Match2Start,
                Match2Table = s.Match2Table,
                Match3 = s.Match3Start,
                Match3Table = s.Match3Table
            })
            .ToArray()));

        Add(("Judging Queuing Schedule", PivotType.JudgingQueuingSchedule, master
            .Select(s => new JudgingQueuingEntry
            {
                QueueTime = s.JudgingStart.AddMinutes(-5),
                Team = s.Number,
                Name = s.Name,
                Judging = s.JudgingStart,
                Pod = s.JudgingPod
            })
            .OrderBy(s => s.QueueTime)
            .ThenBy(s => s.Pod)
            .ToArray()));

        Add(("Judging Schedule", PivotType.JudgingSchedule, master
            .GroupBy(t => t.JudgingStart)
            .Select(g => new { Time = g.Key, Sessions = g.ToArray() })
            .Select(e =>
            {
                var schedule = new FlexEntry { Time = e.Time, Columns = request.Judging.Pods, Row = [] };
                foreach (var pod in schedule.Columns)
                {
                    var assignment = e.Sessions.FirstOrDefault(s => s.JudgingPod == pod);
                    schedule.Row.Add(assignment == null
                        ? "-"
                        : $"{assignment.Number} - {assignment.Name}");
                }
                return schedule;
            })
            .ToArray()));

        foreach (var pod in request.Judging.Pods)
        {
            Add(($"{pod} Judging Schedule", PivotType.PodJudgingSchedule, master
                .Where(s => s.JudgingPod == pod)
                .OrderBy(s => s.JudgingStart)
                .Select(s => new PodJudgingEntry
                {
                    Time = s.JudgingStart,
                    Team = s.Number,
                    Name = s.Name
                })
                .ToArray()));
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
            .Select(e => new RobotGameQueuingEntry
            {
                QueueTime = e.Time.AddMinutes(-5),
                Team = e.Number,
                Name = e.Name,
                MatchTime = e.Time,
                Match = e.Match,
                Table = e.Table
            })
            .OrderBy(e => e.QueueTime)
            // order tables in the order given in the request
            .ThenBy(e => request.RobotGame.Tables.Select((t, i) => (t, i)).First(ee => ee.t == e.Table).i)
            .ToArray();
        Add(("Robot Game Queuing Schedule", PivotType.RobotGameQueuingSchedule, games));

        var combined = games
            .GroupBy(game => game.MatchTime)
            .Select(g => new { Time = g.Key, Games = g.ToArray() })
            .Select(e =>
            {
                var schedule = new FlexEntry { Time = e.Time, Columns = request.RobotGame.Tables, Row = [] };
                foreach (var table in schedule.Columns)
                {
                    var assignment = e.Games.FirstOrDefault(g => g.Table == table);
                    schedule.Row.Add(assignment == null
                        ? "-"
                        : $"{assignment.Team} - {assignment.Name} ({assignment.Match})");
                }
                return schedule;
            })
            .ToArray();
        Add(("Robot Game Schedule", PivotType.RobotGameSchedule, combined));

        foreach (var table in request.RobotGame.Tables)
        {
            var gamesattable = games
                .Where(g => g.Table == table)
                .Select(g => new RobotGameTableEntry
                {
                    MatchTime = g.MatchTime,
                    Team = g.Team,
                    Name = g.Name,
                    Match = g.Match
                })
                .OrderBy(g => g.MatchTime)
                .ToArray();
            Add(($"{table} Robot Game Table Schedule", PivotType.RobotGameTableSchedule, gamesattable));
        }
    }
}