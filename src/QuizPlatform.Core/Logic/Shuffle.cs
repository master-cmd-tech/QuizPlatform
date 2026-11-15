using System.Collections.Immutable;

namespace QuizPlatform.Core.Logic;

public static class Shuffle
{
    /// <summary>Deterministic Fisher-Yates: same seed => same order, input is never mutated.</summary>
    public static ImmutableArray<T> Seeded<T>(IEnumerable<T> items, int seed)
    {
        var arr = items.ToArray();
        var rng = new Random(seed);
        for (var i = arr.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr.ToImmutableArray();
    }
}
