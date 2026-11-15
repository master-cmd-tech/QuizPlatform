using System.Collections.Concurrent;

namespace QuizPlatform.Core.Functional;

/// <summary>Function-level helpers: currying, partial application, composition, pipelines, memoization.</summary>
public static class Fn
{
    public static Func<A, Func<B, R>> Curry<A, B, R>(Func<A, B, R> f) => a => b => f(a, b);

    public static Func<A, Func<B, Func<C, R>>> Curry<A, B, C, R>(Func<A, B, C, R> f) =>
        a => b => c => f(a, b, c);

    public static Func<B, R> Partial<A, B, R>(Func<A, B, R> f, A a) => b => f(a, b);

    public static Func<B, C, R> Partial<A, B, C, R>(Func<A, B, C, R> f, A a) => (b, c) => f(a, b, c);

    public static Func<A, C> Compose<A, B, C>(this Func<A, B> f, Func<B, C> g) => a => g(f(a));

    /// <summary>Forward pipe: value.Pipe(f).Pipe(g)</summary>
    public static R Pipe<T, R>(this T value, Func<T, R> f) => f(value);

    public static T Tap<T>(this T value, Action<T> sideEffect)
    {
        sideEffect(value);
        return value;
    }

    /// <summary>Caches results of a pure function.</summary>
    public static Func<A, R> Memoize<A, R>(this Func<A, R> f) where A : notnull
    {
        var cache = new ConcurrentDictionary<A, R>();
        return a => cache.GetOrAdd(a, f);
    }
}
