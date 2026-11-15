namespace QuizPlatform.Core.Functional;

/// <summary>
/// Discriminated union "value or nothing" - replaces null. Simulated in C# with an
/// abstract record and sealed nested records (private ctor = closed hierarchy).
/// </summary>
public abstract record Option<T>
{
    private Option() { }

    public sealed record SomeCase(T Value) : Option<T> { }
    public sealed record NoneCase : Option<T> { }

    public static Option<T> Some(T value) => new SomeCase(value);
    public static Option<T> None { get; } = new NoneCase();

    public bool IsSome => this is SomeCase;
    public bool IsNone => this is NoneCase;

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) =>
        this is SomeCase s ? some(s.Value) : none();

    /// <summary>Functor map.</summary>
    public Option<TResult> Map<TResult>(Func<T, TResult> f) =>
        this is SomeCase s ? Option<TResult>.Some(f(s.Value)) : Option<TResult>.None;

    /// <summary>Monadic bind (flatMap).</summary>
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> f) =>
        this is SomeCase s ? f(s.Value) : Option<TResult>.None;

    public Option<T> Filter(Func<T, bool> predicate) =>
        this is SomeCase s && !predicate(s.Value) ? None : this;

    public Option<T> OrElse(Func<Option<T>> alternative) => IsSome ? this : alternative();

    public T GetOrElse(T fallback) => this is SomeCase s ? s.Value : fallback;
    public T GetOrElseLazy(Func<T> fallback) => this is SomeCase s ? s.Value : fallback();
    public T? GetOrDefault() => this is SomeCase s ? s.Value : default;

    public Result<T> ToResult(DomainError error) =>
        this is SomeCase s ? Result<T>.Ok(s.Value) : Result<T>.Fail(error);

    /// <summary>Runs a side effect when a value is present and returns the option unchanged.</summary>
    public Option<T> Do(Action<T> action)
    {
        if (this is SomeCase s) action(s.Value);
        return this;
    }
}

public static class Option
{
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);
    public static Option<T> None<T>() => Option<T>.None;

    public static Option<T> OfNullable<T>(T? value) where T : class =>
        value is null ? Option<T>.None : Option<T>.Some(value);

    public static Option<T> OfNullableValue<T>(T? value) where T : struct =>
        value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;
}

/// <summary>Enables LINQ query syntax: from a in x from b in y select ...</summary>
public static class OptionLinq
{
    public static Option<TResult> Select<T, TResult>(this Option<T> o, Func<T, TResult> f) => o.Map(f);

    public static Option<TResult> SelectMany<T, TResult>(this Option<T> o, Func<T, Option<TResult>> f) => o.Bind(f);

    public static Option<TResult> SelectMany<T, TInner, TResult>(
        this Option<T> o, Func<T, Option<TInner>> f, Func<T, TInner, TResult> project) =>
        o.Bind(t => f(t).Map(u => project(t, u)));

    public static Option<T> Where<T>(this Option<T> o, Func<T, bool> predicate) => o.Filter(predicate);
}
