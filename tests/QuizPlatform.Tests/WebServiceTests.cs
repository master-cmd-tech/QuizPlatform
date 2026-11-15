using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using QuizPlatform.Core.Content;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;
using QuizPlatform.Core.Logic;
using QuizPlatform.Web.Services;

namespace QuizPlatform.Tests;

public sealed class FakeClock : IClock
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);
}

public sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "quizlab-" + Guid.NewGuid().ToString("N"));
    public string File => System.IO.Path.Combine(Path, "data.json");
    public TempDir() => Directory.CreateDirectory(Path);
    public void Dispose() { try { Directory.Delete(Path, true); } catch { /* ignore */ } }
}

public class JsonDataStoreTests
{
    [Fact]
    public void First_start_creates_the_file_with_seed_data()
    {
        using var dir = new TempDir();
        using var store = new JsonDataStore(dir.File);
        Assert.Equal(SeedData.Quizzes.Length, store.Current.Quizzes.Length);
        Assert.True(System.IO.File.Exists(dir.File));
    }

    [Fact]
    public async Task Updates_survive_a_restart()
    {
        using var dir = new TempDir();
        using (var store = new JsonDataStore(dir.File))
            await store.UpdateAsync(d => d with { Attempts = d.Attempts.Add(Make.Att("ann", 40)) });

        using var reopened = new JsonDataStore(dir.File);
        Assert.Single(reopened.Current.Attempts);
        Assert.Equal("ann", reopened.Current.Attempts[0].Player);
    }

    [Fact]
    public void Corrupt_file_is_quarantined_and_the_app_still_starts()
    {
        using var dir = new TempDir();
        System.IO.File.WriteAllText(dir.File, "{ this is not json");
        using var store = new JsonDataStore(dir.File);

        Assert.Equal(SeedData.Quizzes.Length, store.Current.Quizzes.Length);
        Assert.Contains(Directory.GetFiles(dir.Path), f => f.Contains(".corrupt-"));
    }

    [Fact]
    public async Task Parallel_writers_never_lose_an_update()
    {
        using var dir = new TempDir();
        using var store = new JsonDataStore(dir.File);

        await Task.WhenAll(Enumerable.Range(0, 40).Select(i =>
            store.UpdateAsync(d => d with { Attempts = d.Attempts.Add(Make.Att("p" + i, i)) })));

        Assert.Equal(40, store.Current.Attempts.Length);
        using var reopened = new JsonDataStore(dir.File);
        Assert.Equal(40, reopened.Current.Attempts.Length);
    }

    [Fact]
    public async Task A_failing_update_function_leaves_the_state_untouched()
    {
        using var dir = new TempDir();
        using var store = new JsonDataStore(dir.File);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.UpdateAsync(_ => throw new InvalidOperationException("nope")));
        Assert.Empty(store.Current.Attempts);
        await store.UpdateAsync(d => d);   // the lock was released
    }
}

public class QuizServiceTests
{
    private const string NewQuiz = "id: mine\ntitle: Mine\n\nq: Pick\n+ a\n- b";

    private static (QuizService svc, LiveFeed feed, TempDir dir, JsonDataStore store) Create()
    {
        var dir = new TempDir();
        var store = new JsonDataStore(dir.File);
        var feed = new LiveFeed();
        return (new QuizService(store, feed, new FakeClock()), feed, dir, store);
    }

    [Fact]
    public async Task Students_cannot_create_or_delete_quizzes_but_teachers_can()
    {
        var (svc, _, dir, store) = Create();
        using (dir) using (store)
        {
            var denied = await svc.ImportAsync(NewQuiz, "stu", Role.Student);
            Assert.Equal("forbidden", denied.Match(_ => "", e => e.Code));
            Assert.True(svc.FindQuiz("mine").IsNone);

            var ok = await svc.ImportAsync(NewQuiz, "teach", Role.Teacher);
            Assert.True(ok.IsOk);
            Assert.True(svc.FindQuiz("mine").IsSome);

            var duplicate = await svc.ImportAsync(NewQuiz, "teach", Role.Teacher);
            Assert.Equal("conflict", duplicate.Match(_ => "", e => e.Code));

            Assert.Equal("forbidden", (await svc.DeleteQuizAsync("mine", Role.Student)).Match(_ => "", e => e.Code));
            Assert.True((await svc.DeleteQuizAsync("mine", Role.Teacher)).IsOk);
            Assert.Equal("not_found", (await svc.DeleteQuizAsync("mine", Role.Teacher)).Match(_ => "", e => e.Code));
        }
    }

    [Fact]
    public async Task Invalid_quizzes_are_rejected_with_all_errors()
    {
        var (svc, _, dir, store) = Create();
        using (dir) using (store)
        {
            var r = await svc.ImportAsync("title: T\n\nq: a\n+ x\n- x", "t", Role.Teacher);
            Assert.True(r.IsError);
        }
    }

    [Fact]
    public async Task Completing_a_quiz_saves_the_attempt_and_notifies_subscribers()
    {
        var (svc, feed, dir, store) = Create();
        using (dir) using (store)
        {
            Attempt? seen = null;
            feed.AttemptCompleted += a => seen = a;

            var quiz = SeedData.Quizzes[0];
            var session = QuizSession.Start(quiz, "ann", DateTimeOffset.UnixEpoch, Option<int>.None);
            while (!session.IsFinished)
                session = session.Submit(Make.CorrectAnswerFor(session.Current.GetOrDefault()!), 1);

            var attempt = await svc.CompleteAsync(session);

            Assert.Equal(100, attempt.Percent);
            Assert.Same(attempt, seen);
            Assert.Single(svc.AttemptsBy("ANN"));
            Assert.Equal("ann", svc.Leaderboard(quiz.Id)[0].Player);
        }
    }

    [Fact]
    public void A_throwing_subscriber_does_not_break_the_others()
    {
        var feed = new LiveFeed();
        var reached = false;
        feed.AttemptCompleted += _ => throw new InvalidOperationException("dead circuit");
        feed.AttemptCompleted += _ => reached = true;
        feed.Publish(Make.Att("a", 1));
        Assert.True(reached);
    }

    [Fact]
    public async Task Flashcard_reviews_are_scheduled_per_player()
    {
        var (svc, _, dir, store) = Create();
        using (dir) using (store)
        {
            var total = svc.TotalCards;
            Assert.Equal(total, svc.DueCards("ann").Length);

            await svc.ReviewCardAsync("ann", "c1", 5);
            Assert.Equal(total, svc.DueCards("ann").Length + 1);   // reviewed card is no longer due today
            Assert.Equal(total, svc.DueCards("bob").Length);
        }
    }
}

public class AccessServiceTests
{
    private static AccessService Create() => new(Options.Create(new AuthOptions { TeacherCode = "s3cret" }));

    [Fact]
    public void Students_need_only_a_valid_name()
    {
        var r = Create().SignIn("  Ann ", Role.Student, null);
        Assert.Equal(new SignedInUser("Ann", Role.Student), r.GetOrElse(null!));
    }

    [Fact]
    public void Teachers_need_the_correct_code()
    {
        Assert.Equal("forbidden", Create().SignIn("Tom", Role.Teacher, "wrong").Match(_ => "", e => e.Code));
        Assert.Equal("forbidden", Create().SignIn("Tom", Role.Teacher, null).Match(_ => "", e => e.Code));
        Assert.True(Create().SignIn("Tom", Role.Teacher, "s3cret").IsOk);
    }

    [Fact]
    public void Invalid_names_are_rejected_before_the_code_is_checked() =>
        Assert.Equal("validation", Create().SignIn("", Role.Teacher, "s3cret").Match(_ => "", e => e.Code));
}

public class AppStateTests
{
    [Fact]
    public void Sign_in_and_out_raise_Changed_and_update_permissions()
    {
        var state = new AppState();
        var raised = 0;
        state.Changed += () => raised++;

        state.SignIn(new SignedInUser("Tom", Role.Teacher));
        Assert.True(state.IsTeacher);
        state.SignOut();
        Assert.False(state.IsSignedIn);
        Assert.False(state.IsTeacher);
        Assert.Equal(2, raised);
    }
}
