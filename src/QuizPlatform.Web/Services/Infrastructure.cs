namespace QuizPlatform.Web.Services;

public interface IClock
{
    DateTimeOffset Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}

public sealed class StorageOptions
{
    public string Path { get; set; } = "App_Data/quizlab.json";
}

public sealed class AuthOptions
{
    public string TeacherCode { get; set; } = "teacher2026";
}
