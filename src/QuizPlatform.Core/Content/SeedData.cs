using System.Collections.Immutable;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;
using QuizPlatform.Core.Logic;

namespace QuizPlatform.Core.Content;

/// <summary>Built-in learning content. Quizzes are written in the platform's own text format and parsed at start-up.</summary>
public static class SeedData
{
    public const string FpBasicsText = """
        id: fp-basics
        title: Functional Programming Basics
        description: Pure functions, immutability, higher-order functions and recursion.
        category: Functional Programming
        time: 30

        q: Which of these functions is pure?
        t: Pure functions
        d: easy
        p: 10
        e: A pure function depends only on its arguments and has no side effects.
        + int Add(int a, int b) => a + b;
        - int Next() => ++counter;
        - DateTime Now() => DateTime.Now;
        - void Log(string s) => Console.WriteLine(s);

        tf: Immutable data can be shared between threads without locks.
        t: Immutability
        d: easy
        a: true
        e: Nothing can change it, so there are no race conditions on the data itself.

        q: What is a higher-order function?
        t: Higher-order functions
        d: easy
        e: Functions are first-class values, so they can be passed in and returned.
        - A function that runs on many threads
        + A function that takes or returns another function
        - A function with more than three parameters
        - A function declared inside a class

        sa: What do we call a recursive call that is the very last operation of a function?
        t: Recursion
        d: medium
        p: 15
        a: tail call | tail recursion | tail-recursive
        e: A tail call can be optimised or trampolined so the stack does not grow.

        q: Which C# feature gives you immutable data with non-destructive mutation via "with"?
        t: Immutability
        d: medium
        e: Records support "with" expressions that copy the instance and change selected properties.
        - struct
        - static class
        + record
        - interface

        tf: A function that reads DateTime.Now is referentially transparent.
        t: Pure functions
        d: medium
        a: false
        e: Two calls with the same arguments can return different values.

        num: A curried function of type Func<int, Func<int, int>> takes how many arguments per call?
        t: Currying
        d: hard
        p: 15
        a: 1
        e: Currying turns a multi-argument function into a chain of one-argument functions.

        q: Partial application means...
        t: Currying
        d: hard
        p: 15
        e: Fixing some arguments of a function yields a new, smaller function.
        - Calling a function only on part of a collection
        - Running half of a function body
        + Fixing some arguments and getting back a function that expects the rest
        - Catching only some exceptions
        """;

    public const string LinqText = """
        id: csharp-linq
        title: LINQ and Lambdas in C#
        description: Query operators, deferred execution and folds.
        category: C# and LINQ
        time: 25

        q: Which LINQ operator transforms every element of a sequence?
        t: LINQ operators
        d: easy
        + Select
        - Where
        - Aggregate
        - Distinct

        q: Which operator keeps only elements matching a predicate?
        t: LINQ operators
        d: easy
        - Select
        + Where
        - Take
        - Zip

        tf: A LINQ query with deferred execution runs when it is enumerated.
        t: Lazy evaluation
        d: medium
        a: true
        e: Nothing executes until you iterate (foreach, ToList, Sum, ...).

        num: What does new[] {1, 2, 3, 4}.Where(x => x % 2 == 0).Sum() return?
        t: LINQ operators
        d: medium
        a: 6
        e: Where keeps 2 and 4; their sum is 6.

        sa: Which delegate type represents a function that returns a value?
        t: Lambdas and delegates
        d: easy
        a: Func | Func<T> | Func<T,TResult>
        e: Func<...> returns a value, Action<...> returns void.

        q: Aggregate in LINQ is the C# name for which functional concept?
        t: Folds
        d: hard
        p: 15
        - map
        - filter
        + fold / reduce
        - zip

        tf: An IEnumerable<T> built with yield return can be infinite.
        t: Lazy evaluation
        d: hard
        p: 15
        a: true
        e: Elements are produced on demand, so only what you consume is computed.
        """;

    public const string TypesText = """
        id: types-errors
        title: Types, Option and Error Handling
        description: Algebraic data types, Option, Result and monadic chaining.
        category: Types and Errors
        time: 30

        q: What does Option<T> (Maybe) represent?
        t: Option
        d: easy
        - A value that is always present
        + A value that may be absent, without using null
        - A list of values
        - An exception

        q: What is Result<T> used for?
        t: Error handling
        d: easy
        - Caching values
        + Returning either a success value or an error as a normal value
        - Logging
        - Parallel execution

        tf: Throwing exceptions for expected validation failures is the preferred functional style.
        t: Error handling
        d: medium
        a: false
        e: Expected failures are modelled as values (Result/Validated), exceptions are for the unexpected.

        q: How can a discriminated union be simulated in C#?
        t: Algebraic data types
        d: medium
        - With a static class
        - With a generic list
        + With an abstract record and sealed derived records
        - With extension methods only

        q: What does Map do on an Option<T>?
        t: Option
        d: medium
        - Removes the value
        + Applies a function to the inner value when it is present
        - Converts Option to List
        - Throws if there is no value

        sa: What is the name of the operation that chains functions returning wrapped values (flatMap)?
        t: Monads
        d: hard
        p: 15
        a: bind | flatmap | selectmany
        e: Bind (SelectMany in LINQ) avoids nested wrappers such as Option<Option<T>>.
        """;

    private static Quiz ParseSeed(string text) =>
        QuizTextParser.Parse(text, "QuizLab", () => "seed")
            .Match(q => q, e => throw new InvalidOperationException($"Seed quiz is invalid: {e.Message}"));

    public static ImmutableArray<Quiz> Quizzes { get; } =
        ImmutableArray.Create(ParseSeed(FpBasicsText), ParseSeed(LinqText), ParseSeed(TypesText));

    public static AppData Initial() => AppData.Empty with { Quizzes = Quizzes };

    public static ImmutableArray<Flashcard> Flashcards { get; } = ImmutableArray.Create(
        new Flashcard("c1", "Functional Programming", "Pure function", "Same input => same output, and no side effects."),
        new Flashcard("c2", "Functional Programming", "Immutability", "Data never changes after creation; \"changes\" create new values."),
        new Flashcard("c3", "Functional Programming", "Higher-order function", "A function that takes functions as arguments or returns a function."),
        new Flashcard("c4", "Functional Programming", "Currying", "Rewriting f(a, b) as f(a)(b): a chain of one-argument functions."),
        new Flashcard("c5", "Functional Programming", "Partial application", "Supplying some arguments now and getting a function that waits for the rest."),
        new Flashcard("c6", "Functional Programming", "Closure", "A function that captures variables from the scope where it was created."),
        new Flashcard("c7", "Functional Programming", "Referential transparency", "An expression can be replaced by its value without changing behaviour."),
        new Flashcard("c8", "Functional Programming", "Memoization", "Caching results of a pure function so repeated calls are free."),
        new Flashcard("c9", "Functional Programming", "Tail recursion", "Recursion where the recursive call is the last action; can run in constant stack."),
        new Flashcard("c10", "Types and Errors", "Option / Maybe", "A type that is either Some(value) or None - a safe replacement for null."),
        new Flashcard("c11", "Types and Errors", "Result / Either", "A type that is either Ok(value) or Error(error)."),
        new Flashcard("c12", "Types and Errors", "Functor", "A container you can Map over without changing its shape."),
        new Flashcard("c13", "Types and Errors", "Monad (bind)", "A container with Bind/flatMap to chain steps that return wrapped values."),
        new Flashcard("c14", "C# and LINQ", "Deferred execution", "A LINQ query is not run until you enumerate it."),
        new Flashcard("c15", "C# and LINQ", "Aggregate", "LINQ's fold: combines all elements into one value with an accumulator."));

    public static ImmutableArray<Lesson> Lessons { get; } = ImmutableArray.Create(
        new Lesson("pure-immutable", "Pure functions and immutability", "Functional Programming",
            "The foundation of FP: functions without side effects and data that never changes.",
            ImmutableArray.Create(
                "A pure function always returns the same result for the same arguments and does not touch anything outside itself: no files, no clock, no global state.",
                "Pure functions are easy to test, easy to cache and safe to run in parallel. Immutable data removes a whole class of bugs caused by shared mutable state.",
                "In C# use records, init-only properties, readonly structs and ImmutableArray<T>. Instead of mutating, create a modified copy with a 'with' expression."),
            "public sealed record Counter(int Value)\n{\n    public Counter Increment() => this with { Value = Value + 1 };\n}\n\nvar a = new Counter(0);\nvar b = a.Increment();   // a is still 0, b is 1"),
        new Lesson("hof-linq", "Higher-order functions and LINQ", "C# and LINQ",
            "Pass behaviour around as values and build data pipelines.",
            ImmutableArray.Create(
                "Func<> and Action<> let you treat behaviour as data. LINQ operators such as Select, Where and Aggregate are higher-order functions: you give them a lambda.",
                "LINQ is lazy: a query describes work and runs only when enumerated. That makes pipelines composable and even infinite sequences possible."),
            "var scores = new[] { 40, 85, 92, 67 };\nvar passed = scores\n    .Where(s => s >= 50)\n    .Select(s => s * 1.1)\n    .Sum();"),
        new Lesson("option-result", "Option and Result: errors as values", "Types and Errors",
            "Stop using null and exceptions for expected situations.",
            ImmutableArray.Create(
                "Option<T> says 'there may be no value'. The compiler-visible type forces callers to handle the empty case.",
                "Result<T> says 'this either worked or failed with this error'. Chain steps with Bind; the first failure short-circuits the rest.",
                "Validated<T> collects every error instead of stopping at the first one - perfect for forms."),
            "var parsed = Parsing.Int(\"42\")\n    .Map(n => n * 2)\n    .GetOrElse(0);   // 84\n\nResult<Quiz> r = QuizTextParser.Parse(text, \"me\", NewId);"),
        new Lesson("currying", "Currying, composition and pipelines", "Functional Programming",
            "Build big behaviour from small functions.",
            ImmutableArray.Create(
                "Currying turns Func<A,B,R> into Func<A,Func<B,R>>. Partial application fixes the first argument and returns a new function.",
                "Compose joins functions end to end; Pipe pushes a value through a chain left to right, which reads like a sentence."),
            "Func<int, int, int> add = (a, b) => a + b;\nvar add5 = Fn.Partial(add, 5);\nvar result = 10.Pipe(add5).Pipe(x => x * 2);   // 30"));
}
