using QuizPlatform.Core.Domain;

namespace QuizPlatform.Web.Services;

/// <summary>
/// State container (scoped per Blazor circuit). Components subscribe to <see cref="Changed"/>
/// so the whole UI stays consistent when the user signs in or out.
/// </summary>
public sealed class AppState
{
    public string Player { get; private set; } = "";
    public Role Role { get; private set; } = Role.Student;

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Player);
    public bool IsTeacher => IsSignedIn && Role == Role.Teacher;

    public event Action? Changed;

    public void SignIn(SignedInUser user)
    {
        Player = user.Name;
        Role = user.Role;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Player = "";
        Role = Role.Student;
        Changed?.Invoke();
    }
}
