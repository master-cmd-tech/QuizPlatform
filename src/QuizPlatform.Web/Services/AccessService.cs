using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using QuizPlatform.Core.Domain;
using QuizPlatform.Core.Functional;
using QuizPlatform.Core.Logic;

namespace QuizPlatform.Web.Services;

/// <summary>Role-based access: students sign in with a name, teachers additionally need the teacher code.</summary>
public sealed class AccessService(IOptions<AuthOptions> options)
{
    public Result<SignedInUser> SignIn(string? name, Role role, string? teacherCode) =>
        PlayerNames.Validate(name).Bind(valid =>
            role == Role.Teacher && !CodeMatches(teacherCode)
                ? Result.Fail<SignedInUser>(DomainError.Forbidden("The teacher code is not correct."))
                : Result.Ok(new SignedInUser(valid, role)));

    private bool CodeMatches(string? given)
    {
        var a = Encoding.UTF8.GetBytes(given ?? "");
        var b = Encoding.UTF8.GetBytes(options.Value.TeacherCode);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
