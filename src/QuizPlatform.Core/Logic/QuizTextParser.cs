using System.Collections.Immutable;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;

namespace QuizPlatform.Core.Logic;

/// <summary>
/// Parses the plain-text quiz format (see docs/QUIZ_FORMAT.md) into a Quiz.
/// Everything returns Option/Result - the parser never throws on bad input.
/// </summary>
public static class QuizTextParser
{
    private sealed record KeyValue(string Key, string Value);

    private sealed record Common(string Label, string Id, string Text, string Topic, Difficulty Difficulty, int Points, string Explanation);

    private sealed record Choice(bool Correct, string Text);

    /// <summary>Parses and validates; every problem of the quiz is reported.</summary>
    public static Validated<Quiz> ParseAndValidate(string text, string author, Func<string> newId) =>
        Parse(text, author, newId).Match(
            quiz => QuizValidator.Validate(quiz),
            error => Validated<Quiz>.Invalid(new[] { error.Message }));

    public static Result<Quiz> Parse(string? text, string author, Func<string> newId)
    {
        var blocks = SplitBlocks((text ?? "").Split('\n')).ToImmutableArray();
        if (blocks.IsEmpty) return Result.Fail<Quiz>(DomainError.Parse("The quiz text is empty."));

        var header = Fields(blocks[0]);
        var quizId = First(header, "id").GetOrElseLazy(newId);
        var description = First(header, "description").GetOrElse("");
        var category = First(header, "category").GetOrElse("General");
        var seconds = First(header, "time").Bind(s => Parsing.Int(s)).GetOrElse(0);

        return First(header, "title")
            .ToResult(DomainError.Parse("The first block must contain 'title: ...'."))
            .Bind(title =>
                Result.Sequence(blocks.Skip(1).Select((b, i) => ParseQuestion(b, i, quizId)))
                    .Map(questions => new Quiz(quizId, title, description, category, seconds, author, questions)));
    }

    // ---------- blocks and fields ----------

    private static IEnumerable<ImmutableArray<string>> SplitBlocks(IEnumerable<string> lines)
    {
        var current = ImmutableArray.CreateBuilder<string>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith('#')) continue;
            if (line.Length == 0)
            {
                if (current.Count > 0)
                {
                    yield return current.ToImmutable();
                    current.Clear();
                }
            }
            else current.Add(line);
        }
        if (current.Count > 0) yield return current.ToImmutable();
    }

    private static bool IsChoiceLine(string l) => l.StartsWith("+ ") || l.StartsWith("- ");

    private static Option<KeyValue> SplitKey(string line)
    {
        var i = line.IndexOf(':');
        return i > 0
            ? Option<KeyValue>.Some(new KeyValue(line[..i].Trim(), line[(i + 1)..].Trim()))
            : Option<KeyValue>.None;
    }

    private static ILookup<string, string> Fields(IEnumerable<string> lines) =>
        lines.Where(l => !IsChoiceLine(l))
            .Choose(l => SplitKey(l))
            .ToLookup(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

    private static Option<string> First(ILookup<string, string> fields, string key) =>
        fields.Contains(key) ? Option<string>.Some(fields[key].First()) : Option<string>.None;

    // ---------- questions ----------

    private static Result<Question> ParseQuestion(ImmutableArray<string> block, int index, string quizId)
    {
        var f = Fields(block);
        var label = $"Question {index + 1}";
        var id = $"{quizId}-q{index + 1}";

        Common MakeCommon(string text) => new(
            label, id, text,
            First(f, "t").GetOrElse("General"),
            First(f, "d").Bind(s => Parsing.Enum<Difficulty>(s)).GetOrElse(Difficulty.Medium),
            First(f, "p").Bind(s => Parsing.Int(s)).GetOrElse(10),
            First(f, "e").GetOrElse(""));

        var choices = block.Where(IsChoiceLine).Select(l => new Choice(l[0] == '+', l[2..].Trim())).ToImmutableArray();

        return First(f, "q").Map(t => BuildChoice(MakeCommon(t), choices))
            .OrElse(() => First(f, "tf").Map(t => BuildTrueFalse(MakeCommon(t), First(f, "a"))))
            .OrElse(() => First(f, "sa").Map(t => BuildShort(MakeCommon(t), First(f, "a"))))
            .OrElse(() => First(f, "num").Map(t => BuildNumeric(MakeCommon(t), First(f, "a"), First(f, "tol"))))
            .GetOrElseLazy(() => Result.Fail<Question>(DomainError.Parse(
                $"{label}: start the block with 'q:', 'tf:', 'sa:' or 'num:'.")));
    }

    private static Result<Question> BuildChoice(Common c, ImmutableArray<Choice> choices)
    {
        var correct = choices.Select((x, i) => (x, i)).Where(t => t.x.Correct).Select(t => t.i).ToImmutableArray();
        return correct.Length == 1
            ? Result.Ok<Question>(new MultipleChoiceQuestion(c.Id, c.Text, c.Topic, c.Difficulty, c.Points, c.Explanation,
                choices.Select(x => x.Text).ToImmutableArray(), correct[0]))
            : Result.Fail<Question>(DomainError.Parse($"{c.Label}: mark exactly one option with '+' (others with '-')."));
    }

    private static Result<Question> BuildTrueFalse(Common c, Option<string> answer) =>
        answer.Bind(s => Parsing.Bool(s))
            .Map(b => (Question)new TrueFalseQuestion(c.Id, c.Text, c.Topic, c.Difficulty, c.Points, c.Explanation, b))
            .ToResult(DomainError.Parse($"{c.Label}: 'a:' must be true or false."));

    private static Result<Question> BuildShort(Common c, Option<string> answer) =>
        answer.Map(s => s.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToImmutableArray())
            .Filter(a => a.Length > 0)
            .Map(a => (Question)new ShortAnswerQuestion(c.Id, c.Text, c.Topic, c.Difficulty, c.Points, c.Explanation, a))
            .ToResult(DomainError.Parse($"{c.Label}: 'a:' needs at least one answer (separate alternatives with |)."));

    private static Result<Question> BuildNumeric(Common c, Option<string> answer, Option<string> tolerance)
    {
        var tol = tolerance.Bind(s => Parsing.Double(s)).GetOrElse(0);
        return answer.Bind(s => Parsing.Double(s))
            .Map(v => (Question)new NumericQuestion(c.Id, c.Text, c.Topic, c.Difficulty, c.Points, c.Explanation, v, tol))
            .ToResult(DomainError.Parse($"{c.Label}: 'a:' must be a number."));
    }
}
