using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace QuizPlatform.Core.Domain;

public enum Difficulty { Easy = 1, Medium = 2, Hard = 3 }

public enum Role { Student, Teacher }

/// <summary>Discriminated union of question kinds (abstract record + sealed cases).</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(MultipleChoiceQuestion), "choice")]
[JsonDerivedType(typeof(TrueFalseQuestion), "truefalse")]
[JsonDerivedType(typeof(ShortAnswerQuestion), "short")]
[JsonDerivedType(typeof(NumericQuestion), "numeric")]
public abstract record Question(string Id, string Text, string Topic, Difficulty Difficulty, int Points, string Explanation);

public sealed record MultipleChoiceQuestion(
    string Id, string Text, string Topic, Difficulty Difficulty, int Points, string Explanation,
    ImmutableArray<string> Options, int CorrectIndex)
    : Question(Id, Text, Topic, Difficulty, Points, Explanation);

public sealed record TrueFalseQuestion(
    string Id, string Text, string Topic, Difficulty Difficulty, int Points, string Explanation,
    bool CorrectAnswer)
    : Question(Id, Text, Topic, Difficulty, Points, Explanation);

public sealed record ShortAnswerQuestion(
    string Id, string Text, string Topic, Difficulty Difficulty, int Points, string Explanation,
    ImmutableArray<string> AcceptedAnswers)
    : Question(Id, Text, Topic, Difficulty, Points, Explanation);

public sealed record NumericQuestion(
    string Id, string Text, string Topic, Difficulty Difficulty, int Points, string Explanation,
    double Answer, double Tolerance)
    : Question(Id, Text, Topic, Difficulty, Points, Explanation);

/// <summary>Discriminated union of answers a player can give.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ChoiceAnswer), "choice")]
[JsonDerivedType(typeof(BoolAnswer), "bool")]
[JsonDerivedType(typeof(TextAnswer), "text")]
public abstract record Answer;

public sealed record ChoiceAnswer(int Index) : Answer;
public sealed record BoolAnswer(bool Value) : Answer;
public sealed record TextAnswer(string Text) : Answer;
