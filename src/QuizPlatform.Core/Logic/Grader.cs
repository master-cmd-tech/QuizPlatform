using System.Globalization;
using QuizPlatform.Core.Domain;

namespace QuizPlatform.Core.Logic;

/// <summary>Pure grading: (question, answer) -> verdict, using tuple pattern matching.</summary>
public static class Grader
{
    public static string Normalize(string s) =>
        string.Join(' ', s.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static bool IsCorrect(Question question, Answer answer) => (question, answer) switch
    {
        (MultipleChoiceQuestion mc, ChoiceAnswer c) => c.Index == mc.CorrectIndex,
        (TrueFalseQuestion tf, BoolAnswer b) => b.Value == tf.CorrectAnswer,
        (ShortAnswerQuestion sa, TextAnswer t) =>
            sa.AcceptedAnswers.Any(x => Normalize(x) == Normalize(t.Text)),
        (NumericQuestion n, TextAnswer t) =>
            double.TryParse(t.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            && Math.Abs(v - n.Answer) <= n.Tolerance + 1e-9,
        _ => false
    };

    public static GradedAnswer Grade(Question question, Answer? given, double seconds)
    {
        var correct = given is not null && IsCorrect(question, given);
        return new GradedAnswer(question.Id, question.Topic, question.Difficulty,
            correct, correct ? question.Points : 0, question.Points, given, seconds);
    }
}
