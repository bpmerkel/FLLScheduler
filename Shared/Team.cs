namespace FLLScheduler.Shared;

public class Team
{
    public string Number { get; init; }
    public string Name { get; init; }
    public TimeOnly JudgingStart { get; set; }
    public string JudgingPod { get; set; }
    public RobotGameMatch[] Match { get; set; }

    public Team(string number, string name)
    {
        Number = number;
        Name = name;
        // 0 = Practice, 1 = Match 1, 2 = Match 2, 3 = Match 3
        Match = [new RobotGameMatch(), new RobotGameMatch(), new RobotGameMatch(), new RobotGameMatch()];
    }
}
