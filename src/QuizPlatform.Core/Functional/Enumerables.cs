using System.Globalization;

namespace QuizPlatform.Core.Functional;

/// <summary>Extension methods over IEnumerable (lazy, composable, LINQ-style).</summary>
public static class Enumerables
{
    /// <summary>Applies a function to every pair of adjacent elements (a,b), (b,c), ...</summary>
    public static IEnumerable<TResult> Pairwise<T, TResult>(this IEnumerable<T> source, Func<T, T, TResult> selector)
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext()) yield break;
        var previous = e.Current;
        while (e.MoveNext())
        {
            yield return selector(previous, e.Current);
            previous = e.Current;
        }
    }

    /// <summary>Keeps the values of all Some options and drops the None ones.</summary>
    public static IEnumerable<T> Choose<T>(this IEnumerable<Option<T>> source) =>
        source.SelectMany(o => o.Match(v => new[] { v }, () => Array.Empty<T>()));

    public static IEnumerable<TResult> Choose<T, TResult>(this IEnumerable<T> source, Func<T, Option<TResult>> f) =>
        source.Select(f).Choose();

    /// <summary>Running fold: yields every intermediate accumulator value.</summary>
    public static IEnumerable<TAcc> Scan<T, TAcc>(this IEnumerable<T> source, TAcc seed, Func<TAcc, T, TAcc> f)
    {
        var acc = seed;
        foreach (var item in source)
        {
            acc = f(acc, item);
            yield return acc;
        }
    }

    /// <summary>Generates a (possibly infinite) lazy sequence from a seed.</summary>
    public static IEnumerable<T> Unfold<TState, T>(TState seed, Func<TState, Option<(T, TState)>> step)
    {
        var state = seed;
        while (step(state) is Option<(T, TState)>.SomeCase some)
        {
            yield return some.Value.Item1;
            state = some.Value.Item2;
        }
    }
}

/// <summary>Safe parsing: every parser returns an Option instead of throwing or using out parameters.</summary>
public static class Parsing
{
    public static Option<int> Int(string s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            ? Option<int>.Some(v) : Option<int>.None;

    public static Option<double> Double(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? Option<double>.Some(v) : Option<double>.None;

    public static Option<bool> Bool(string s) => s.Trim().ToLowerInvariant() switch
    {
        "true" or "yes" or "t" or "y" or "1" => Option<bool>.Some(true),
        "false" or "no" or "f" or "n" or "0" => Option<bool>.Some(false),
        _ => Option<bool>.None
    };

    public static Option<TEnum> Enum<TEnum>(string s) where TEnum : struct, Enum =>
        System.Enum.TryParse<TEnum>(s.Trim(), true, out var v) && System.Enum.IsDefined(v)
            ? Option<TEnum>.Some(v) : Option<TEnum>.None;
}
