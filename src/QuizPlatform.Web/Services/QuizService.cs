using System.Collections.Immutable;
using QuizPlatform.Core.Content;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;
using QuizPlatform.Core.Logic;

namespace QuizPlatform.Web.Services;

/// <summary>Application service: thin imperative shell around the pure core.</summary>
public sealed class QuizService(IDataStore store, LiveFeed feed, IClock clock)
{
    private static string NewId() => Guid.NewGuid().ToString("N")[..10];

    public ImmutableArray<Quiz> Quizzes => store.Current.Quizzes;
    public ImmutableArray<Attempt> Attempts => store.Current.Attempts;

    public Option<Quiz> FindQuiz(string id) => Option.OfNullable(Quizzes.FirstOrDefault(q => q.Id == id));
    public Option<Attempt> FindAttempt(string id) => Option.OfNullable(Attempts.FirstOrDefault(a => a.Id == id));

    public ImmutableArray<Attempt> AttemptsBy(string player) =>
        Attempts.Where(a => string.Equals(a.Player, player, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.FinishedAt).ToImmutableArray();

    public ImmutableArray<LeaderboardEntry> Leaderboard(string? quizId, int take = 10) =>
        Rankings.Top(Attempts.Where(a => quizId is null || a.QuizId == quizId), take);

    public async Task<Attempt> CompleteAsync(QuizSession session, CancellationToken ct = default)
    {
        var attempt = session.ToAttempt(NewId(), clock.Now, ScoringRules.Default);
        await store.UpdateAsync(d => d with { Attempts = d.Attempts.Add(attempt) }, ct);
        feed.Publish(attempt);
        return attempt;
    }

    // ---------- teacher features (authorization is enforced HERE, not only in the UI) ----------

    public Validated<Quiz> Preview(string text, string author) =>
        QuizTextParser.ParseAndValidate(text, author, NewId);

    public async Task<Result<Quiz>> ImportAsync(string text, string author, Role role, CancellationToken ct = default)
    {
        if (!Permissions.CanCreateQuiz(role))
            return Result.Fail<Quiz>(DomainError.Forbidden("Only teachers can create quizzes."));

        var parsed = Preview(text, author).ToResult();
        return await parsed.Match(
            async quiz =>
            {
                if (FindQuiz(quiz.Id).IsSome)
                    return Result.Fail<Quiz>(DomainError.Conflict($"A quiz with id '{quiz.Id}' already exists."));
                await store.UpdateAsync(d => d with { Quizzes = d.Quizzes.Add(quiz) }, ct);
                return Result.Ok(quiz);
            },
            error => Task.FromResult(Result.Fail<Quiz>(error)));
    }

    public async Task<Result<Unit>> DeleteQuizAsync(string id, Role role, CancellationToken ct = default)
    {
        if (!Permissions.CanDeleteQuiz(role))
            return Result.Fail<Unit>(DomainError.Forbidden("Only teachers can delete quizzes."));
        if (FindQuiz(id).IsNone)
            return Result.Fail<Unit>(DomainError.NotFound("Quiz not found."));

        await store.UpdateAsync(d => d with { Quizzes = d.Quizzes.RemoveAll(q => q.Id == id) }, ct);
        return Result.Ok(Unit.Value);
    }

    // ---------- flashcards (spaced repetition) ----------

    private DateOnly Today => DateOnly.FromDateTime(clock.Now.UtcDateTime);

    public ImmutableArray<Flashcard> DueCards(string player) =>
        Sm2.Due(SeedData.Flashcards, store.Current.Progress, player, Today);

    public int TotalCards => SeedData.Flashcards.Length;

    public async Task ReviewCardAsync(string player, string cardId, int quality, CancellationToken ct = default)
    {
        var today = Today;
        await store.UpdateAsync(d =>
        {
            var existing = d.Progress.FirstOrDefault(p => p.Player == player && p.CardId == cardId)
                           ?? Sm2.Initial(player, cardId, today);
            var updated = Sm2.Review(existing, quality, today);
            return d with { Progress = d.Progress.RemoveAll(p => p.Player == player && p.CardId == cardId).Add(updated) };
        }, ct);
    }
}
