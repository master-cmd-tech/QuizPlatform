using QuizPlatform.Core.Domain;

namespace QuizPlatform.Core.Logic;

public sealed record ScoringRules(double BonusPerStreakStep, double MaxBonusMultiplier)
{
    public static ScoringRules Default { get; } = new(0.1, 0.5);
}

public static class Scoring
{
    private sealed record Acc(int Bonus, int Streak);

    /// <summary>Streak bonus computed with a fold (Aggregate): +10% per previous correct answer, capped at +50%.</summary>
    public static int Bonus(IEnumerable<GradedAnswer> answers, ScoringRules rules) =>
        answers.Aggregate(new Acc(0, 0), (acc, a) => a.IsCorrect
            ? new Acc(
                acc.Bonus + (int)Math.Round(
                    a.Points * Math.Min(rules.MaxBonusMultiplier, rules.BonusPerStreakStep * acc.Streak),
                    MidpointRounding.AwayFromZero),
                acc.Streak + 1)
            : acc with { Streak = 0 }).Bonus;

    /// <summary>Maps a percentage to the Astana IT University letter scale.</summary>
    public static string Letter(double percent) => percent switch
    {
        >= 95 => "A",
        >= 90 => "A-",
        >= 85 => "B+",
        >= 80 => "B",
        >= 75 => "B-",
        >= 70 => "C+",
        >= 65 => "C",
        >= 60 => "C-",
        >= 55 => "D+",
        >= 50 => "D",
        >= 25 => "FX",
        _ => "F"
    };
}
