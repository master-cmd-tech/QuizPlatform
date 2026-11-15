namespace QuizPlatform.Web.Components.Shared;

/// <summary>Cascaded to every page by <c>ToastHost</c>; pages call Show() to display a notification.</summary>
public sealed class ToastApi
{
    private readonly Action<string, string> _show;

    public ToastApi(Action<string, string> show) => _show = show;

    public void Success(string message) => _show(message, "ok");
    public void Error(string message) => _show(message, "error");
    public void Info(string message) => _show(message, "info");
}
