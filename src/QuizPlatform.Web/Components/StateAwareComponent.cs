using Microsoft.AspNetCore.Components;
using QuizPlatform.Web.Services;

namespace QuizPlatform.Web.Components;

/// <summary>
/// Base class for components that depend on the signed-in user: it subscribes to the AppState container
/// and re-renders automatically, and unsubscribes on dispose (no leaks).
/// </summary>
public abstract class StateAwareComponent : ComponentBase, IDisposable
{
    [Inject] protected AppState State { get; set; } = default!;

    protected override void OnInitialized() => State.Changed += HandleChanged;

    private void HandleChanged()
    {
        OnStateChanged();
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Hook for components that must reload data when the user changes.</summary>
    protected virtual void OnStateChanged() { }

    public virtual void Dispose() => State.Changed -= HandleChanged;
}
