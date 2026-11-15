using System.Collections.Immutable;

namespace QuizPlatform.Core.Functional;

/// <summary>
/// Applicative validation: unlike Result (fail-fast) it ACCUMULATES every error,
/// so a teacher sees all problems of a quiz at once.
/// </summary>
public abstract record Validated<T>
{
    private Validated() { }

    public sealed record ValidCase(T Value) : Validated<T> { }
    public sealed record InvalidCase(ImmutableArray<string> ErrorList) : Validated<T> { }

    public static Validated<T> Valid(T value) => new ValidCase(value);
    public static Validated<T> Invalid(IEnumerable<string> errors) => new InvalidCase(errors.ToImmutableArray());

    public bool IsValid => this is ValidCase;

    public ImmutableArray<string> Errors => this is InvalidCase i ? i.ErrorList : ImmutableArray<string>.Empty;

    public Validated<TResult> Map<TResult>(Func<T, TResult> f) =>
        this is ValidCase v ? Validated<TResult>.Valid(f(v.Value)) : Validated<TResult>.Invalid(Errors);

    /// <summary>Combines two validations; errors of both sides are kept.</summary>
    public Validated<TResult> Zip<TOther, TResult>(Validated<TOther> other, Func<T, TOther, TResult> combine) =>
        this is ValidCase a && other is Validated<TOther>.ValidCase b
            ? Validated<TResult>.Valid(combine(a.Value, b.Value))
            : Validated<TResult>.Invalid(Errors.AddRange(other.Errors));

    public Result<T> ToResult() => this is ValidCase v
        ? Result<T>.Ok(v.Value)
        : Result<T>.Fail(DomainError.Validation(string.Join(" ", Errors)));
}

public static class Validated
{
    public static Validated<T> FromErrors<T>(T value, IEnumerable<string> errors)
    {
        var list = errors.ToImmutableArray();
        return list.IsEmpty ? Validated<T>.Valid(value) : Validated<T>.Invalid(list);
    }
}
