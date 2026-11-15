namespace QuizPlatform.Core.Functional;

/// <summary>
/// Trampolining: turns deep (tail) recursion into a loop so it can never overflow the stack.
/// </summary>
public abstract record Trampoline<T>
{
    public sealed record DoneCase(T Value) : Trampoline<T> { }
    public sealed record MoreCase(Func<Trampoline<T>> Next) : Trampoline<T> { }

    public T Run()
    {
        Trampoline<T> current = this;
        while (true)
        {
            switch (current)
            {
                case DoneCase d: return d.Value;
                case MoreCase m: current = m.Next(); break;
                default: throw new InvalidOperationException("Unknown trampoline case.");
            }
        }
    }
}

public static class Trampoline
{
    public static Trampoline<T> Done<T>(T value) => new Trampoline<T>.DoneCase(value);
    public static Trampoline<T> More<T>(Func<Trampoline<T>> next) => new Trampoline<T>.MoreCase(next);
}
