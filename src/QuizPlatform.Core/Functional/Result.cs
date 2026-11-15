using System.Collections.Immutable;

namespace QuizPlatform.Core.Functional;

/// <summary>An error expressed as a value (no exceptions for expected failures).</summary>
public sealed record DomainError(string Code, string Message)
{
    public static DomainError Validation(string message) => new("validation", message);
    public static DomainError Parse(string message) => new("parse", message);
    public static DomainError NotFound(string message) => new("not_found", message);
    public static DomainError Forbidden(string message) => new("forbidden", message);
    public static DomainError Conflict(string message) => new("conflict", message);
    public override string ToString() => $"{Code}: {Message}";
}

/// <summary>Either-style union: Ok(value) or Fail(error).</summary>
public abstract record Result<T>
{
    private Result() { }

    public sealed record OkCase(T Value) : Result<T> { }
    public sealed record ErrCase(DomainError Error) : Result<T> { }

    public static Result<T> Ok(T value) => new OkCase(value);
    public static Result<T> Fail(DomainError error) => new ErrCase(error);

    public bool IsOk => this is OkCase;
    public bool IsError => this is ErrCase;

    public TResult Match<TResult>(Func<T, TResult> ok, Func<DomainError, TResult> error) => this switch
    {
        OkCase o => ok(o.Value),
        ErrCase e => error(e.Error),
        _ => throw new InvalidOperationException("Unreachable: Result has exactly two cases.")
    };

    public Result<TResult> Map<TResult>(Func<T, TResult> f) =>
        Match(v => Result<TResult>.Ok(f(v)), e => Result<TResult>.Fail(e));

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> f) =>
        Match(f, e => Result<TResult>.Fail(e));

    public Result<T> Ensure(Func<T, bool> predicate, DomainError error) =>
        Match(v => predicate(v) ? this : Result<T>.Fail(error), _ => this);

    public T GetOrElse(T fallback) => Match(v => v, _ => fallback);

    public Option<T> ToOption() => Match(v => Option<T>.Some(v), _ => Option<T>.None);

    /// <summary>Runs a side effect for either case (UI code).</summary>
    public void Switch(Action<T> ok, Action<DomainError> error)
    {
        switch (this)
        {
            case OkCase o: ok(o.Value); break;
            case ErrCase e: error(e.Error); break;
        }
    }
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
    public static Result<T> Fail<T>(DomainError error) => Result<T>.Fail(error);

    /// <summary>Bridges exception-based code into the Result world.</summary>
    public static Result<T> Try<T>(Func<T> f)
    {
        try { return Ok(f()); }
        catch (Exception ex) { return Fail<T>(new DomainError("exception", ex.Message)); }
    }

    /// <summary>Turns a sequence of results into a result of a sequence (stops at the first error).</summary>
    public static Result<ImmutableArray<T>> Sequence<T>(IEnumerable<Result<T>> results)
    {
        var builder = ImmutableArray.CreateBuilder<T>();
        foreach (var r in results)
        {
            switch (r)
            {
                case Result<T>.OkCase ok: builder.Add(ok.Value); break;
                case Result<T>.ErrCase err: return Fail<ImmutableArray<T>>(err.Error);
            }
        }
        return Ok(builder.ToImmutable());
    }
}

public static class ResultLinq
{
    public static Result<TResult> Select<T, TResult>(this Result<T> r, Func<T, TResult> f) => r.Map(f);
    public static Result<TResult> SelectMany<T, TResult>(this Result<T> r, Func<T, Result<TResult>> f) => r.Bind(f);
    public static Result<TResult> SelectMany<T, TInner, TResult>(
        this Result<T> r, Func<T, Result<TInner>> f, Func<T, TInner, TResult> project) =>
        r.Bind(t => f(t).Map(u => project(t, u)));
}
