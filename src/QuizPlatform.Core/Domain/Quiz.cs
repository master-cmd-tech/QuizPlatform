using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace QuizPlatform.Core.Domain;

/// <summary>An immutable quiz. SecondsPerQuestion = 0 means "no time limit".</summary>
public sealed record Quiz(
    string Id, string Title, string Description, string Category,
    int SecondsPerQuestion, string Author, ImmutableArray<Question> Questions)
{
    [JsonIgnore] public int MaxScore => Questions.Sum(q => q.Points);
}

public sealed record GradedAnswer(
    string QuestionId, string Topic, Difficulty Difficulty,
    bool IsCorrect, int Points, int MaxPoints, Answer? Given, double Seconds);

public sealed record Attempt(
    string Id, string QuizId, string QuizTitle, string Player,
    DateTimeOffset StartedAt, DateTimeOffset FinishedAt,
    ImmutableArray<GradedAnswer> Answers, int Score, int MaxScore, int Bonus)
{
    [JsonIgnore] public int TotalPoints => Score + Bonus;
    [JsonIgnore] public double Percent => MaxScore == 0 ? 0 : 100.0 * Score / MaxScore;
    [JsonIgnore] public TimeSpan Duration => FinishedAt - StartedAt;
}

public sealed record Flashcard(string Id, string Topic, string Front, string Back);

public sealed record CardProgress(string Player, string CardId, double Ease, int IntervalDays, int Repetitions, DateOnly Due);

public sealed record Lesson(string Id, string Title, string Topic, string Summary, ImmutableArray<string> Paragraphs, string Code);

public sealed record SignedInUser(string Name, Role Role);

/// <summary>The whole persisted state - an immutable snapshot; updates produce a new snapshot.</summary>
public sealed record AppData(
    ImmutableArray<Quiz> Quizzes, ImmutableArray<Attempt> Attempts, ImmutableArray<CardProgress> Progress)
{
    public static AppData Empty { get; } = new(
        ImmutableArray<Quiz>.Empty, ImmutableArray<Attempt>.Empty, ImmutableArray<CardProgress>.Empty);
}

public static class Permissions
{
    public static bool CanCreateQuiz(Role role) => role == Role.Teacher;
    public static bool CanDeleteQuiz(Role role) => role == Role.Teacher;
    public static bool CanSeeGlobalAnalytics(Role role) => role == Role.Teacher;
}
