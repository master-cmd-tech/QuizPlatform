using System.Globalization;
using QuizPlatform.Core.Domain;

namespace QuizPlatform.Core.Logic;

/// <summary>Pure formatting helpers shared by UI components (pattern matching over the unions).</summary>
public static class Presenter
{
    public static string DescribeAnswer(Question q, Answer? a) => (q, a) switch
    {
        (_, null) => "(no answer)",
        (MultipleChoiceQuestion mc, ChoiceAnswer c) when c.Index >= 0 && c.Index < mc.Options.Length => mc.Options[c.Index],
        (_, BoolAnswer b) => b.Value ? "True" : "False",
        (_, TextAnswer t) => t.Text,
        _ => "(invalid)"
    };

    public static string DescribeCorrect(Question q) => q switch
    {
        MultipleChoiceQuestion mc => mc.Options[mc.CorrectIndex],
        TrueFalseQuestion tf => tf.CorrectAnswer ? "True" : "False",
        ShortAnswerQuestion sa => string.Join(" / ", sa.AcceptedAnswers),
        NumericQuestion n => n.Tolerance > 0
            ? $"{n.Answer.ToString(CultureInfo.InvariantCulture)} (±{n.Tolerance.ToString(CultureInfo.InvariantCulture)})"
            : n.Answer.ToString(CultureInfo.InvariantCulture),
        _ => ""
    };

    public static string KindLabel(Question q) => q switch
    {
        MultipleChoiceQuestion => "Multiple choice",
        TrueFalseQuestion => "True / False",
        ShortAnswerQuestion => "Short answer",
        NumericQuestion => "Numeric",
        _ => "Question"
    };

    public static string Percent(double v) => v.ToString("0.#", CultureInfo.InvariantCulture) + "%";

    public static string Signed(double v) => (v >= 0 ? "+" : "") + v.ToString("0.#", CultureInfo.InvariantCulture) + "%";

    public static string Duration(TimeSpan t) => $"{(int)t.TotalMinutes}:{t.Seconds:00}";
}
