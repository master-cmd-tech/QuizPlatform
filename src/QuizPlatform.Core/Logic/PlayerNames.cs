using System.Text.RegularExpressions;
using QuizPlatform.Core.Functional;

namespace QuizPlatform.Core.Logic;

public static class PlayerNames
{
    private static readonly Regex Pattern = new(@"^[\p{L}\p{N} _.\-]{2,30}$", RegexOptions.Compiled);

    public static Result<string> Validate(string? raw)
    {
        var name = (raw ?? "").Trim();
        return Pattern.IsMatch(name)
            ? Result.Ok(name)
            : Result.Fail<string>(DomainError.Validation("Name must be 2-30 characters: letters, digits, space, _ . -"));
    }
}
