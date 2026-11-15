using System.Collections.Immutable;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;

namespace QuizPlatform.Core.Logic;

/// <summary>Rule-based validation that accumulates ALL errors (Validated, not Result).</summary>
public static class QuizValidator
{
    private static Option<string> Check(bool ok, string message) =>
        ok ? Option<string>.None : Option<string>.Some(message);

    private static readonly Func<Quiz, Option<string>>[] QuizRules =
    {
        q => Check(!string.IsNullOrWhiteSpace(q.Title), "Title is required."),
        q => Check(q.Title.Length <= 100, "Title must be at most 100 characters."),
        q => Check(q.Questions.Length >= 1, "A quiz needs at least one question."),
        q => Check(q.Questions.Length <= 100, "A quiz can have at most 100 questions."),
        q => Check(q.SecondsPerQuestion == 0 || q.SecondsPerQuestion is >= 5 and <= 600,
            "Time per question must be 0 (no limit) or 5-600 seconds."),
        q => Check(q.Questions.Select(x => x.Id).Distinct().Count() == q.Questions.Length, "Question ids must be unique.")
    };

    public static Validated<Quiz> Validate(Quiz quiz)
    {
        var quizErrors = QuizRules.Select(rule => rule(quiz)).Choose();
        var questionErrors = quiz.Questions.SelectMany((q, i) => QuestionErrors(i, q));
        return Validated.FromErrors(quiz, quizErrors.Concat(questionErrors));
    }

    private static IEnumerable<string> QuestionErrors(int index, Question q)
    {
        var n = $"Question {index + 1}";
        var common = new[]
        {
            Check(!string.IsNullOrWhiteSpace(q.Text), $"{n}: text is empty."),
            Check(q.Points is >= 1 and <= 100, $"{n}: points must be 1-100.")
        }.Choose();

        IEnumerable<string> specific = q switch
        {
            MultipleChoiceQuestion mc => new[]
            {
                Check(mc.Options.Length is >= 2 and <= 6, $"{n}: needs 2-6 options."),
                Check(mc.CorrectIndex >= 0 && mc.CorrectIndex < mc.Options.Length, $"{n}: correct option is out of range."),
                Check(mc.Options.Select(Grader.Normalize).Distinct().Count() == mc.Options.Length, $"{n}: options must be distinct."),
                Check(mc.Options.All(o => !string.IsNullOrWhiteSpace(o)), $"{n}: options must not be empty.")
            }.Choose(),
            ShortAnswerQuestion sa => new[]
            {
                Check(sa.AcceptedAnswers.Length >= 1 && sa.AcceptedAnswers.All(a => !string.IsNullOrWhiteSpace(a)),
                    $"{n}: needs at least one accepted answer.")
            }.Choose(),
            NumericQuestion nq => new[]
            {
                Check(nq.Tolerance >= 0, $"{n}: tolerance cannot be negative."),
                Check(!double.IsNaN(nq.Answer) && !double.IsInfinity(nq.Answer), $"{n}: answer must be a finite number.")
            }.Choose(),
            _ => Enumerable.Empty<string>()
        };

        return common.Concat(specific);
    }
}
