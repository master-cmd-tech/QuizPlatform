using System.Collections.Immutable;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;

namespace QuizPlatform.Core.Logic;

public sealed record TopicStat(string Topic, int Answered, int Correct)
{
    public double Accuracy => Answered == 0 ? 0 : 100.0 * Correct / Answered;
}

public sealed record QuestionStat(string QuestionId, int Answered, int Correct)
{
    public double Accuracy => Answered == 0 ? 0 : 100.0 * Correct / Answered;
}

public sealed record LeaderboardEntry(int Rank, string Player, int TotalPoints, double Percent, TimeSpan Duration, int Attempts);

/// <summary>LINQ-based, side-effect-free analytics over attempts.</summary>
public static class Analytics
{
    public static ImmutableArray<TopicStat> ByTopic(IEnumerable<Attempt> attempts) =>
        attempts.SelectMany(a => a.Answers)
            .GroupBy(x => x.Topic)
            .Select(g => new TopicStat(g.Key, g.Count(), g.Count(x => x.IsCorrect)))
            .OrderBy(t => t.Accuracy).ThenBy(t => t.Topic)
            .ToImmutableArray();

    /// <summary>Change in percentage between consecutive attempts (adjacent-element query).</summary>
    public static ImmutableArray<double> Trend(IEnumerable<Attempt> attempts) =>
        attempts.OrderBy(a => a.FinishedAt).Pairwise((prev, next) => next.Percent - prev.Percent).ToImmutableArray();

    public static Option<Attempt> Best(IEnumerable<Attempt> attempts) =>
        attempts.OrderByDescending(a => a.Percent).ThenBy(a => a.Duration)
            .Take(1)
            .Select(a => Option<Attempt>.Some(a))
            .DefaultIfEmpty(Option<Attempt>.None)
            .First();

    public static double AveragePercent(IEnumerable<Attempt> attempts)
    {
        var list = attempts.ToList();
        return list.Count == 0 ? 0 : list.Average(a => a.Percent);
    }

    public static ImmutableArray<QuestionStat> HardestQuestions(IEnumerable<Attempt> attempts, int take) =>
        attempts.SelectMany(a => a.Answers)
            .GroupBy(a => a.QuestionId)
            .Select(g => new QuestionStat(g.Key, g.Count(), g.Count(a => a.IsCorrect)))
            .OrderBy(s => s.Accuracy).ThenByDescending(s => s.Answered)
            .Take(take)
            .ToImmutableArray();

    /// <summary>Longest run of correct answers - tail-recursive, run on a trampoline (stack safe).</summary>
    public static int LongestCorrectRun(IEnumerable<Attempt> attempts)
    {
        var flags = attempts.OrderBy(a => a.FinishedAt).SelectMany(a => a.Answers).Select(a => a.IsCorrect).ToArray();
        return Go(flags, 0, 0, 0).Run();
    }

    public static int LongestRun(IReadOnlyList<bool> flags) => Go(flags, 0, 0, 0).Run();

    private static Trampoline<int> Go(IReadOnlyList<bool> xs, int i, int run, int best) =>
        i >= xs.Count
            ? Trampoline.Done(best)
            : xs[i]
                ? Trampoline.More(() => Go(xs, i + 1, run + 1, Math.Max(best, run + 1)))
                : Trampoline.More(() => Go(xs, i + 1, 0, best));
}

public static class Rankings
{
    /// <summary>Best attempt per player, ordered by points then speed.</summary>
    public static ImmutableArray<LeaderboardEntry> Top(IEnumerable<Attempt> attempts, int take) =>
        attempts.GroupBy(a => a.Player, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Best: g.OrderByDescending(a => a.TotalPoints).ThenBy(a => a.Duration).First(), Count: g.Count()))
            .OrderByDescending(x => x.Best.TotalPoints).ThenBy(x => x.Best.Duration)
            .Take(take)
            .Select((x, i) => new LeaderboardEntry(i + 1, x.Best.Player, x.Best.TotalPoints, x.Best.Percent, x.Best.Duration, x.Count))
            .ToImmutableArray();
}
