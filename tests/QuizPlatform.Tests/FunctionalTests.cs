using System.Collections.Immutable;
using QuizPlatform.Core.Functional;

namespace QuizPlatform.Tests;

public class OptionTests
{
    [Fact]
    public void Map_and_Bind_work_on_Some_and_skip_None()
    {
        Assert.Equal(6, Option<int>.Some(3).Map(x => x * 2).GetOrElse(0));
        Assert.Equal(0, Option<int>.None.Map(x => x * 2).GetOrElse(0));
        Assert.Equal(4, Option<int>.Some(2).Bind(x => Option<int>.Some(x * 2)).GetOrElse(0));
        Assert.True(Option<int>.Some(2).Bind(_ => Option<int>.None).IsNone);
    }

    [Fact]
    public void Filter_and_OrElse()
    {
        Assert.True(Option<int>.Some(1).Filter(x => x > 5).IsNone);
        Assert.Equal(9, Option<int>.None.OrElse(() => Option<int>.Some(9)).GetOrElse(0));
    }

    [Fact]
    public void Works_with_LINQ_query_syntax()
    {
        var sum = from a in Parsing.Int("20")
                  from b in Parsing.Int("22")
                  select a + b;
        Assert.Equal(42, sum.GetOrElse(-1));

        var bad = from a in Parsing.Int("20")
                  from b in Parsing.Int("x")
                  select a + b;
        Assert.True(bad.IsNone);
    }

    [Fact]
    public void Parsing_never_throws()
    {
        Assert.True(Parsing.Int("abc").IsNone);
        Assert.Equal(1.5, Parsing.Double("1.5").GetOrElse(0));
        Assert.True(Parsing.Bool("maybe").IsNone);
        Assert.True(Parsing.Bool("YES").GetOrElse(false));
    }
}

public class ResultTests
{
    [Fact]
    public void Bind_short_circuits_on_first_error()
    {
        var calls = 0;
        var r = Result.Ok(1)
            .Bind(_ => Result.Fail<int>(DomainError.Validation("boom")))
            .Bind(x => { calls++; return Result.Ok(x); });
        Assert.True(r.IsError);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Sequence_collects_values_or_returns_first_error()
    {
        var ok = Result.Sequence(new[] { Result.Ok(1), Result.Ok(2) });
        Assert.Equal(new[] { 1, 2 }, ok.GetOrElse(ImmutableArray<int>.Empty).ToArray());

        var failed = Result.Sequence(new[] { Result.Ok(1), Result.Fail<int>(DomainError.Parse("x")), Result.Fail<int>(DomainError.Parse("y")) });
        Assert.Equal("x", failed.Match(_ => "", e => e.Message));
    }

    [Fact]
    public void Try_converts_exceptions_into_errors()
    {
        var r = Result.Try<int>(() => throw new InvalidOperationException("bad"));
        Assert.Equal("bad", r.Match(_ => "", e => e.Message));
    }

    [Fact]
    public void Validated_accumulates_all_errors()
    {
        var a = Validated<int>.Invalid(new[] { "e1" });
        var b = Validated<int>.Invalid(new[] { "e2", "e3" });
        var zipped = a.Zip(b, (x, y) => x + y);
        Assert.Equal(new[] { "e1", "e2", "e3" }, zipped.Errors.ToArray());
        Assert.Equal(5, Validated<int>.Valid(2).Zip(Validated<int>.Valid(3), (x, y) => x + y).Unwrap());
    }
}

internal static class ValidatedTestExt
{
    public static T Unwrap<T>(this Validated<T> v) => ((Validated<T>.ValidCase)v).Value;
}

public class FnTests
{
    [Fact]
    public void Curry_partial_compose_pipe()
    {
        Func<int, int, int> add = (a, b) => a + b;
        Assert.Equal(7, Fn.Curry(add)(3)(4));
        var add5 = Fn.Partial(add, 5);
        Assert.Equal(15, add5(10));

        Func<int, int> inc = x => x + 1;
        Assert.Equal(8, inc.Compose(x => x * 2)(3));
        Assert.Equal(30, 10.Pipe(add5).Pipe(x => x * 2));
    }

    [Fact]
    public void Memoize_calls_the_function_once_per_argument()
    {
        var calls = 0;
        Func<int, int> slow = x => { calls++; return x * x; };
        var fast = slow.Memoize();
        Assert.Equal(16, fast(4));
        Assert.Equal(16, fast(4));
        Assert.Equal(1, calls);
    }
}

public class EnumerableTests
{
    [Fact]
    public void Pairwise_returns_adjacent_differences()
    {
        Assert.Equal(new[] { 3, 5 }, new[] { 1, 4, 9 }.Pairwise((a, b) => b - a).ToArray());
        Assert.Empty(new[] { 1 }.Pairwise((a, b) => b - a));
    }

    [Fact]
    public void Choose_Scan_and_lazy_Unfold()
    {
        Assert.Equal(new[] { 1, 3 }, new[] { "1", "x", "3" }.Choose(s => Parsing.Int(s)).ToArray());
        Assert.Equal(new[] { 1, 3, 6 }, new[] { 1, 2, 3 }.Scan(0, (a, x) => a + x).ToArray());

        // infinite sequence: only the consumed part is computed
        var powers = Enumerables.Unfold(1, s => Option<(int, int)>.Some((s, s * 2)));
        Assert.Equal(new[] { 1, 2, 4, 8, 16 }, powers.Take(5).ToArray());
    }

    [Fact]
    public void Trampoline_is_stack_safe_for_a_million_steps()
    {
        static Trampoline<int> Count(int n, int acc) =>
            n == 0 ? Trampoline.Done(acc) : Trampoline.More(() => Count(n - 1, acc + 1));
        Assert.Equal(1_000_000, Count(1_000_000, 0).Run());
    }
}
