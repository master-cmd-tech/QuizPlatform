using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;

namespace QuizPlatform.Core.Logic;

/// <summary>Adaptive practice: difficulty goes up after a streak and down after a mistake.</summary>
public static class AdaptiveEngine
{
    public static Difficulty Next(Difficulty current, bool correct, int streak) => (correct, streak) switch
    {
        (false, _) => Down(current),
        (true, >= 2) => Up(current),
        _ => current
    };

    private static Difficulty Up(Difficulty d) => d switch
    {
        Difficulty.Easy => Difficulty.Medium,
        _ => Difficulty.Hard
    };

    private static Difficulty Down(Difficulty d) => d switch
    {
        Difficulty.Hard => Difficulty.Medium,
        _ => Difficulty.Easy
    };

    /// <summary>Picks an unseen question closest to the target difficulty (seeded => testable).</summary>
    public static Option<Question> PickQuestion(
        IEnumerable<Question> pool, Difficulty target, IEnumerable<string> seen, int seed)
    {
        var seenSet = seen.ToHashSet();
        var candidates = pool.Where(q => !seenSet.Contains(q.Id)).ToArray();
        if (candidates.Length == 0) return Option<Question>.None;

        var distance = candidates.Min(q => Math.Abs((int)q.Difficulty - (int)target));
        var best = candidates.Where(q => Math.Abs((int)q.Difficulty - (int)target) == distance).ToArray();
        return Option.Some(best[new Random(seed).Next(best.Length)]);
    }
}
