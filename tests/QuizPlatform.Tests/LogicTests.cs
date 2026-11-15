using System.Collections.Immutable;
using System.Text.Json;
using QuizPlatform.Core.Content;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;
using QuizPlatform.Core.Logic;

namespace QuizPlatform.Tests;

internal static class Make
{
    public static MultipleChoiceQuestion Mc(string id = "q1", Difficulty d = Difficulty.Easy, int points = 10) =>
        new(id, "Pick B", "Topic", d, points, "why", ImmutableArray.Create("A", "B", "C"), 1);

    public static GradedAnswer Graded(bool correct, int points = 10, string topic = "T", string id = "q") =>
        new(id, topic, Difficulty.Easy, correct, correct ? points : 0, points, null, 1);

    public static Attempt Att(string player, int score, int minutes = 1, DateTimeOffset? at = null, int bonus = 0, params bool[] flags)
    {
        var finished = at ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var answers = flags.Select((f, i) => Graded(f, 10, "T", $"q{i}")).ToImmutableArray();
        return new Attempt($"a-{player}-{score}-{finished.Ticks}", "quiz", "Quiz", player,
            finished.AddMinutes(-minutes), finished, answers, score, 100, bonus);
    }

    public static Answer CorrectAnswerFor(Question q) => q switch
    {
        MultipleChoiceQuestion mc => new ChoiceAnswer(mc.CorrectIndex),
        TrueFalseQuestion tf => new BoolAnswer(tf.CorrectAnswer),
        ShortAnswerQuestion sa => new TextAnswer(sa.AcceptedAnswers[0]),
        NumericQuestion n => new TextAnswer(n.Answer.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        _ => throw new InvalidOperationException()
    };
}

public class GraderTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void Multiple_choice(int index, bool expected) =>
        Assert.Equal(expected, Grader.IsCorrect(Make.Mc(), new ChoiceAnswer(index)));

    [Fact]
    public void Wrong_answer_kind_is_incorrect_not_an_exception() =>
        Assert.False(Grader.IsCorrect(Make.Mc(), new BoolAnswer(true)));

    [Fact]
    public void Short_answer_ignores_case_and_extra_spaces()
    {
        var q = new ShortAnswerQuestion("s", "t", "T", Difficulty.Easy, 5, "", ImmutableArray.Create("tail call", "tail recursion"));
        Assert.True(Grader.IsCorrect(q, new TextAnswer("  Tail   RECURSION ")));
        Assert.False(Grader.IsCorrect(q, new TextAnswer("loop")));
    }

    [Theory]
    [InlineData("3.14", true)]
    [InlineData("3,14", true)]
    [InlineData("3.2", false)]
    [InlineData("abc", false)]
    public void Numeric_with_tolerance(string input, bool expected)
    {
        var q = new NumericQuestion("n", "pi?", "T", Difficulty.Easy, 5, "", 3.14, 0.01);
        Assert.Equal(expected, Grader.IsCorrect(q, new TextAnswer(input)));
    }

    [Fact]
    public void Skipped_question_scores_zero()
    {
        var g = Grader.Grade(Make.Mc(), null, 5);
        Assert.False(g.IsCorrect);
        Assert.Equal(0, g.Points);
    }
}

public class ScoringTests
{
    [Fact]
    public void Streak_bonus_grows_and_resets()
    {
        var all = new[] { true, true, true }.Select(f => Make.Graded(f)).ToArray();
        Assert.Equal(3, Scoring.Bonus(all, ScoringRules.Default));   // 0 + 1 + 2

        var broken = new[] { true, true, false, true }.Select(f => Make.Graded(f)).ToArray();
        Assert.Equal(1, Scoring.Bonus(broken, ScoringRules.Default));
    }

    [Fact]
    public void Bonus_is_capped()
    {
        var many = Enumerable.Repeat(true, 20).Select(f => Make.Graded(f)).ToArray();
        
        Assert.True(Scoring.Bonus(many, ScoringRules.Default) <= 20 * 5);
    }

    [Theory]
    [InlineData(100, "A")] [InlineData(95, "A")] [InlineData(94.9, "A-")] [InlineData(80, "B")]
    [InlineData(50, "D")] [InlineData(49, "FX")] [InlineData(25, "FX")] [InlineData(24.9, "F")]
    public void Letter_follows_the_university_scale(double percent, string letter) =>
        Assert.Equal(letter, Scoring.Letter(percent));
}

public class QuizSessionTests
{
    private static Quiz TwoQuestionQuiz() => new("quiz", "Q", "", "C", 0, "me",
        ImmutableArray.Create<Question>(Make.Mc("a"), Make.Mc("b")));

    [Fact]
    public void Transitions_return_new_sessions_and_never_mutate()
    {
        var start = QuizSession.Start(TwoQuiz(), "ann", DateTimeOffset.UnixEpoch, Option<int>.None);
        var after = start.Submit(new ChoiceAnswer(1), 2);

        Assert.Empty(start.Answers);
        Assert.Single(after.Answers);
        Assert.False(after.IsFinished);

        var done = after.Skip(1);
        Assert.True(done.IsFinished);
        Assert.True(done.Current.IsNone);
        Assert.Equal(10, done.Answers.Sum(a => a.Points));
    }

    private static Quiz TwoQuiz() => TwoQuestionQuiz();

    [Fact]
    public void ToAttempt_sums_points_and_bonus()
    {
        var s = QuizSession.Start(TwoQuiz(), "ann", DateTimeOffset.UnixEpoch, Option<int>.None)
            .Submit(new ChoiceAnswer(1), 1).Submit(new ChoiceAnswer(1), 1);
        var attempt = s.ToAttempt("id", DateTimeOffset.UnixEpoch.AddMinutes(1), ScoringRules.Default);
        Assert.Equal(20, attempt.Score);
        Assert.Equal(20, attempt.MaxScore);
        Assert.Equal(1, attempt.Bonus);
        Assert.Equal(100, attempt.Percent);
    }

    [Fact]
    public void Shuffle_is_deterministic_per_seed_and_keeps_all_items()
    {
        var items = Enumerable.Range(1, 20).ToArray();
        var a = Shuffle.Seeded(items, 7);
        var b = Shuffle.Seeded(items, 7);
        Assert.Equal(a.ToArray(), b.ToArray());
        Assert.Equal(items, a.OrderBy(x => x).ToArray());
        Assert.NotEqual(items, a.ToArray());
    }
}

public class AnalyticsTests
{
    [Fact]
    public void ByTopic_lists_weakest_first()
    {
        var attempt = new Attempt("a", "q", "Q", "p", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch,
            ImmutableArray.Create(Make.Graded(true, topic: "Easy"), Make.Graded(false, topic: "Hard"), Make.Graded(true, topic: "Hard")),
            20, 30, 0);
        var stats = Analytics.ByTopic(new[] { attempt });
        Assert.Equal("Hard", stats[0].Topic);
        Assert.Equal(50, stats[0].Accuracy);
    }

    [Fact]
    public void Trend_compares_adjacent_attempts()
    {
        var t = DateTimeOffset.UnixEpoch;
        var trend = Analytics.Trend(new[]
        {
            Make.Att("p", 40, at: t), Make.Att("p", 70, at: t.AddDays(1)), Make.Att("p", 60, at: t.AddDays(2))
        });
        Assert.Equal(new[] { 30.0, -10.0 }, trend.ToArray());
    }

    [Fact]
    public void LongestRun_handles_a_million_answers_without_stack_overflow()
    {
        var flags = Enumerable.Repeat(true, 1_000_000).Concat(new[] { false, true }).ToArray();
        Assert.Equal(1_000_000, Analytics.LongestRun(flags));
        Assert.Equal(3, Analytics.LongestRun(new[] { true, false, true, true, true, false }));
        Assert.Equal(0, Analytics.LongestRun(Array.Empty<bool>()));
    }

    [Fact]
    public void Leaderboard_keeps_best_attempt_per_player_and_ranks_by_points_then_speed()
    {
        var board = Rankings.Top(new[]
        {
            Make.Att("Ann", 50), Make.Att("ann", 80), Make.Att("Bob", 80, minutes: 5), Make.Att("Cy", 20)
        }, 10);
        Assert.Equal(3, board.Length);
        Assert.Equal(new[] { "ann", "Bob", "Cy" }, board.Select(e => e.Player).ToArray());
        Assert.Equal(2, board[0].Attempts);
        Assert.Equal(1, board[0].Rank);
    }
}

public class AdaptiveAndSm2Tests
{
    [Theory]
    [InlineData(Difficulty.Easy, true, 1, Difficulty.Easy)]
    [InlineData(Difficulty.Easy, true, 2, Difficulty.Medium)]
    [InlineData(Difficulty.Medium, true, 3, Difficulty.Hard)]
    [InlineData(Difficulty.Hard, true, 9, Difficulty.Hard)]
    [InlineData(Difficulty.Hard, false, 0, Difficulty.Medium)]
    [InlineData(Difficulty.Easy, false, 0, Difficulty.Easy)]
    public void Difficulty_moves_with_performance(Difficulty from, bool correct, int streak, Difficulty expected) =>
        Assert.Equal(expected, AdaptiveEngine.Next(from, correct, streak));

    [Fact]
    public void PickQuestion_prefers_target_difficulty_and_skips_seen()
    {
        var easy = Make.Mc("e", Difficulty.Easy);
        var hard = Make.Mc("h", Difficulty.Hard);
        Assert.Equal("h", AdaptiveEngine.PickQuestion(new Question[] { easy, hard }, Difficulty.Hard, Array.Empty<string>(), 1).GetOrDefault()!.Id);
        Assert.Equal("e", AdaptiveEngine.PickQuestion(new Question[] { easy, hard }, Difficulty.Hard, new[] { "h" }, 1).GetOrDefault()!.Id);
        Assert.True(AdaptiveEngine.PickQuestion(new Question[] { easy, hard }, Difficulty.Hard, new[] { "h", "e" }, 1).IsNone);
    }

    [Fact]
    public void Sm2_intervals_grow_and_failures_reset()
    {
        var day = new DateOnly(2026, 1, 1);
        var p = Sm2.Initial("ann", "c1", day);

        p = Sm2.Review(p, 4, day);
        Assert.Equal(1, p.IntervalDays);
        p = Sm2.Review(p, 4, day.AddDays(1));
        Assert.Equal(6, p.IntervalDays);
        p = Sm2.Review(p, 4, day.AddDays(7));
        Assert.Equal(15, p.IntervalDays);
        Assert.Equal(new DateOnly(2026, 1, 23), p.Due);

        var failed = Sm2.Review(p, 1, day.AddDays(30));
        Assert.Equal(0, failed.Repetitions);
        Assert.Equal(1, failed.IntervalDays);
    }

    [Fact]
    public void Sm2_ease_never_drops_below_minimum()
    {
        var p = Sm2.Initial("a", "c", new DateOnly(2026, 1, 1));
        for (var i = 0; i < 30; i++) p = Sm2.Review(p, 3, new DateOnly(2026, 1, 1).AddDays(i));
        Assert.True(p.Ease >= 1.3);
    }

    [Fact]
    public void Due_returns_new_cards_and_cards_whose_date_has_come()
    {
        var today = new DateOnly(2026, 5, 10);
        var cards = new[] { new Flashcard("1", "T", "f", "b"), new Flashcard("2", "T", "f", "b"), new Flashcard("3", "T", "f", "b") };
        var progress = new[]
        {
            new CardProgress("ann", "1", 2.5, 6, 2, today.AddDays(3)),   // not due
            new CardProgress("ann", "2", 2.5, 1, 1, today)               // due today
        };
        Assert.Equal(new[] { "2", "3" }, Sm2.Due(cards, progress, "ann", today).Select(c => c.Id).ToArray());
        Assert.Equal(3, Sm2.Due(cards, progress, "bob", today).Length);
    }
}

public class ParserAndValidatorTests
{
    private const string Valid = """
        title: Demo
        category: Test
        time: 20

        q: Pick B
        d: hard
        p: 20
        - A
        + B
        - C

        tf: Sky is blue
        a: yes

        sa: Say hi
        a: hi | hello

        num: 2 + 2
        a: 4
        tol: 0.5
        """;

    private static Result<Quiz> Parse(string text) => QuizTextParser.Parse(text, "me", () => "gen");

    [Fact]
    public void Parses_all_four_question_kinds()
    {
        var quiz = Parse(Valid).GetOrElse(null!);
        Assert.Equal("Demo", quiz.Title);
        Assert.Equal(20, quiz.SecondsPerQuestion);
        Assert.Equal(4, quiz.Questions.Length);
        var mc = Assert.IsType<MultipleChoiceQuestion>(quiz.Questions[0]);
        Assert.Equal(1, mc.CorrectIndex);
        Assert.Equal(Difficulty.Hard, mc.Difficulty);
        Assert.Equal(20, mc.Points);
        Assert.IsType<TrueFalseQuestion>(quiz.Questions[1]);
        Assert.Equal(2, Assert.IsType<ShortAnswerQuestion>(quiz.Questions[2]).AcceptedAnswers.Length);
        Assert.Equal(0.5, Assert.IsType<NumericQuestion>(quiz.Questions[3]).Tolerance);
        Assert.True(QuizValidator.Validate(quiz).IsValid);
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("category: x\n\nq: a\n+ x\n- y", "title")]
    [InlineData("title: T\n\nq: a\n- x\n- y", "exactly one")]
    [InlineData("title: T\n\ntf: a\na: maybe", "true or false")]
    [InlineData("title: T\n\nnum: a\na: abc", "number")]
    [InlineData("title: T\n\nhello world", "start the block")]
    public void Bad_input_gives_a_descriptive_error_never_an_exception(string text, string expectedFragment)
    {
        var message = Parse(text).Match(_ => "", e => e.Message);
        Assert.Contains(expectedFragment, message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validator_reports_every_problem_at_once()
    {
        var bad = new Quiz("id", "", "", "c", 3, "me", ImmutableArray.Create<Question>(
            new MultipleChoiceQuestion("a", "", "T", Difficulty.Easy, 0, "", ImmutableArray.Create("x", "x"), 5)));
        var result = QuizValidator.Validate(bad);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Length >= 5, string.Join(" | ", result.Errors));
    }

    [Fact]
    public void Plain_text_can_be_parsed_and_validated_in_one_step()
    {
        var v = QuizTextParser.ParseAndValidate("title: T\n\nq: a\n+ x\n- y", "me", () => "id");
        Assert.True(v.IsValid);
        Assert.False(QuizTextParser.ParseAndValidate("junk", "me", () => "id").IsValid);
    }
}

public class SeedAndJsonTests
{
    [Fact]
    public void Every_seed_quiz_is_valid_and_every_question_is_answerable()
    {
        Assert.Equal(3, SeedData.Quizzes.Length);
        foreach (var quiz in SeedData.Quizzes)
        {
            Assert.True(QuizValidator.Validate(quiz).IsValid, quiz.Title);
            foreach (var q in quiz.Questions)
                Assert.True(Grader.IsCorrect(q, Make.CorrectAnswerFor(q)), q.Id);
        }
    }

    [Fact]
    public void Persistence_format_round_trips_including_polymorphic_questions()
    {
        var data = SeedData.Initial() with
        {
            Attempts = ImmutableArray.Create(Make.Att("ann", 30, flags: new[] { true, false }))
        };
        var json = JsonSerializer.Serialize(data, JsonDefaults.Options);
        var back = JsonSerializer.Deserialize<AppData>(json, JsonDefaults.Options)!;

        Assert.Equal(data.Quizzes.Length, back.Quizzes.Length);
        Assert.IsType<NumericQuestion>(back.Quizzes[0].Questions.First(q => q is NumericQuestion));
        Assert.Equal(json, JsonSerializer.Serialize(back, JsonDefaults.Options));
    }

    [Fact]
    public void Player_names_are_validated()
    {
        Assert.True(PlayerNames.Validate("Ann_01").IsOk);
        Assert.Equal("Ann", PlayerNames.Validate("  Ann ").GetOrElse(""));
        Assert.True(PlayerNames.Validate("A").IsError);
        Assert.True(PlayerNames.Validate("<script>").IsError);
        Assert.True(PlayerNames.Validate(null).IsError);
    }
}
