namespace QuizPlatform.Core.Functional;

/// <summary>The "no value" type, so that side-effecting steps can be expressed as functions.</summary>
public readonly record struct Unit
{
    public static Unit Value => default;
}
