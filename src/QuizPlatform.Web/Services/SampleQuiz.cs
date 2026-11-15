namespace QuizPlatform.Web.Services;

public static class SampleQuiz
{
    public const string Text = """
        title: My first quiz
        description: Two quick questions.
        category: General
        time: 20

        q: Which keyword creates an immutable data type with value equality?
        t: C#
        d: easy
        e: Records have value-based equality and support "with".
        + record
        - static
        - abstract
        - extern

        tf: Pure functions have no side effects.
        t: FP
        a: true
        """;
}
