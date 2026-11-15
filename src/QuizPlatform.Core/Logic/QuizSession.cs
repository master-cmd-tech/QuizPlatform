using System.Collections.Immutable;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;

namespace QuizPlatform.Core.Logic;

/// <summary>
/// Immutable state machine of one quiz run. Every transition returns a NEW session.
/// </summary>
public sealed record QuizSession(
    Quiz Quiz, string Player, DateTimeOffset StartedAt,
    ImmutableArray<GradedAnswer> Answers, ImmutableArray<Question> Order)
{
    public static QuizSession Start(Quiz quiz, string player, DateTimeOffset now, Option<int> shuffleSeed) =>
        new(quiz, player, now, ImmutableArray<GradedAnswer>.Empty,
            shuffleSeed.Match(seed => Shuffle.Seeded(quiz.Questions, seed), () => quiz.Questions));

    public bool IsFinished => Answers.Length >= Order.Length;

    public Option<Question> Current =>
        IsFinished ? Option<Question>.None : Option<Question>.Some(Order[Answers.Length]);

    public QuizSession Submit(Answer? answer, double seconds) =>
        Current.Match(
            q => this with { Answers = Answers.Add(Grader.Grade(q, answer, seconds)) },
            () => this);

    public QuizSession Skip(double seconds) => Submit(null, seconds);

    public Attempt ToAttempt(string attemptId, DateTimeOffset finishedAt, ScoringRules rules) =>
        new(attemptId, Quiz.Id, Quiz.Title, Player, StartedAt, finishedAt, Answers,
            Answers.Sum(a => a.Points), Order.Sum(q => q.Points), Scoring.Bonus(Answers, rules));
}
