using QuizPlatform.Core.Domain;

namespace QuizPlatform.Web.Services;

/// <summary>
/// In-process publish/subscribe bus. Because Blazor Server keeps every browser connected through a
/// SignalR circuit, publishing here makes every open leaderboard update in real time.
/// </summary>
public sealed class LiveFeed
{
    public event Action<Attempt>? AttemptCompleted;

    public void Publish(Attempt attempt)
    {
        var handlers = AttemptCompleted;
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<Attempt>>())
        {
            try { handler(attempt); }
            catch { /* one broken (disconnected) circuit must never affect the others */ }
        }
    }
}
