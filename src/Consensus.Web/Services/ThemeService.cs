using Microsoft.JSInterop;

namespace Consensus.Web.Services;

/// <summary>
/// Holds the current dark-mode preference and persists it to the browser's
/// localStorage via JS interop. Scoped per Blazor Server circuit. UI components
/// (MainLayout and the app-bar switch) subscribe to <see cref="Changed"/> and
/// re-render when the value flips.
///
/// Note: the *first* render of any page runs server-side (SSR) where
/// IJSRuntime calls would throw. Components must hydrate after the first
/// interactive render, typically inside <c>OnAfterRenderAsync(firstRender)</c>.
/// </summary>
public class ThemeService
{
    private const string StorageKey = "consensus.theme.dark";

    private readonly IJSRuntime _js;
    private bool _isDark;
    private bool _hydrated;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public event Action? Changed;

    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (_isDark == value) return;
            _isDark = value;
            Changed?.Invoke();
            // Fire-and-forget persistence; failures (e.g. private-mode browsers)
            // shouldn't break the toggle.
            _ = PersistAsync(value);
        }
    }

    /// <summary>
    /// Call once from a top-level component's <c>OnAfterRenderAsync(firstRender)</c>
    /// (passing the <c>firstRender</c> flag) to load the persisted preference
    /// from localStorage. Subsequent calls are no-ops.
    /// </summary>
    public async Task HydrateAsync(bool firstRender)
    {
        if (!firstRender || _hydrated) return;
        _hydrated = true;
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (stored == "true" && !_isDark)
            {
                _isDark = true;
                Changed?.Invoke();
            }
            else if (stored == "false" && _isDark)
            {
                _isDark = false;
                Changed?.Invoke();
            }
        }
        catch
        {
            // localStorage may be disabled; treat as light default.
        }
    }

    private async Task PersistAsync(bool value)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, value ? "true" : "false");
        }
        catch (Microsoft.JSInterop.JSDisconnectedException)
        {
            // Toggling dark mode while another interop call is in flight
            // (e.g. a large simulation export) can race with a SignalR
            // reconnect and throw JSDisconnectedException before the
            // localStorage write reaches the browser. Eat it explicitly so
            // the unobserved-task exception doesn't bubble out to Blazor's
            // error banner — the in-memory `_isDark` already changed.
        }
        catch (TaskCanceledException)
        {
            // Same root cause, surfaces as task-cancelled when the circuit
            // is mid-reset rather than fully disconnected.
        }
        catch
        {
            // Last-resort: private-mode browsers, localStorage quota
            // exhaustion, etc. We never want the theme toggle to crash.
        }
    }
}
