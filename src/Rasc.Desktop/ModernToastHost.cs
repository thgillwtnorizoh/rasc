using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Modern NORMAL desktop achievement presentation. Uses one non-activating
/// window per toast and time-based scheduling. There is no Steam injection,
/// sound playback, display-alone handling or claim of original raster parity.
/// </summary>
internal sealed class ModernToastHost : IDisposable
{
    private sealed record Content(string Title, string Description);
    private readonly ModernToastScheduler _schedule = new();
    private readonly Dictionary<string, ModernToastWindow> _windows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Content> _content = new(StringComparer.Ordinal);
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _holdWake;
    private readonly ClassicAnimationFrameGate _frameGate = new();
    private TimeSpan? _lastRenderTime;
    private int _fpsCap;
    private int _sizePercent;
    private bool _renderSubscribed;
    private bool _disposed;

    internal ModernToastHost(Dispatcher dispatcher)
    {
        _holdWake = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
        _holdWake.Tick += OnHoldWake;
        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    internal void SetSettings(ClassicPresentationSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var valid = settings.Validated();
        _fpsCap = valid.AnimationFps;
        _sizePercent = valid.PopupSizePercent;
        _frameGate.Reset();
        Tick(); // Relayout, not a new notification.
    }

    internal void Enqueue(UnlockEvent evt) =>
        Enqueue(evt, "Achievement unlocked", evt.AchievementId);

    private void Enqueue(UnlockEvent evt, string title, string description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Tick(); // Rebase an idle scheduler to the current monotonic clock.
        if (!_schedule.Enqueue(evt)) return;
        _content[evt.EventId] = new Content(title, description);
        Tick();
    }

    internal void PreviewUnlock() =>
        Enqueue(PreviewEvent(), "Achievement unlocked", "Someone Set Up Us The Bomb");

    internal void PreviewProgress() =>
        Enqueue(PreviewEvent(), "Achievement progress",
            "Paranatural Collection (20,010/100,000)");

    private static UnlockEvent PreviewEvent() =>
        new("modern-preview-" + Guid.NewGuid().ToString("N"),
            "rasc-local-preview", "REFERENCE-FIXTURE-NOT-AN-AWARD",
            0, DateTimeOffset.UtcNow);

    private void OnRendering(object? sender, EventArgs args)
    {
        if (_disposed || !_renderSubscribed) return;
        if (args is RenderingEventArgs render)
        {
            if (_lastRenderTime == render.RenderingTime) return;
            _lastRenderTime = render.RenderingTime;
        }
        if (_frameGate.ShouldApply(_clock.Elapsed, _fpsCap)) Tick();
    }

    private void OnHoldWake(object? sender, EventArgs args)
    {
        _holdWake.Stop();
        Tick();
    }

    private void OnDisplayChanged(object? sender, EventArgs args)
    {
        if (!_disposed) _ = _holdWake.Dispatcher.BeginInvoke(new Action(Tick));
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs args)
    {
        if (!_disposed) _ = _holdWake.Dispatcher.BeginInvoke(new Action(Tick));
    }

    private void StartRendering()
    {
        _holdWake.Stop();
        if (_renderSubscribed) return;
        _lastRenderTime = null;
        _frameGate.Reset();
        CompositionTarget.Rendering += OnRendering;
        _renderSubscribed = true;
    }

    private void StopRendering()
    {
        if (!_renderSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderSubscribed = false;
        _lastRenderTime = null;
    }

    private void SleepUntilExpiry()
    {
        var next = _schedule.NextHoldDeadline;
        if (next is null) return;
        var remaining = next.Value - _clock.Elapsed;
        _holdWake.Interval = TimeSpan.FromMilliseconds(
            Math.Clamp(Math.Ceiling(remaining.TotalMilliseconds), 1.0, 60000.0));
        _holdWake.Stop();
        _holdWake.Start();
    }

    private static double Scale(Rect work, int requested)
    {
        var scale = requested == 0
            ? Math.Clamp(SystemParameters.PrimaryScreenHeight / 1080.0, 1.0, 2.0)
            : requested / 100.0;
        return Math.Max(0.1, Math.Min(scale, Math.Min(
            work.Width / ModernToastScheduler.CardWidth,
            work.Height / (ModernToastScheduler.CardHeight * ModernToastScheduler.Capacity))));
    }

    private void Tick()
    {
        if (_disposed) return;
        var frames = _schedule.Advance(_clock.Elapsed);
        var visible = new HashSet<string>(frames.Select(f => f.Event.EventId),
            StringComparer.Ordinal);
        foreach (var id in _windows.Keys.Where(id => !visible.Contains(id)).ToArray())
        {
            _windows[id].Close();
            _windows.Remove(id);
            _content.Remove(id);
        }

        var work = SystemParameters.WorkArea;
        var scale = Scale(work, _sizePercent);
        foreach (var frame in frames)
        {
            if (!_windows.TryGetValue(frame.Event.EventId, out var window))
            {
                var text = _content[frame.Event.EventId];
                window = new ModernToastWindow(text.Title, text.Description);
                _windows.Add(frame.Event.EventId, window);
                window.Apply(frame, scale, work);
                window.Show();
            }
            else window.Apply(frame, scale, work);
        }

        if (_schedule.IsIdle)
        {
            StopRendering();
            _holdWake.Stop();
        }
        else if (_schedule.IsAnimating) StartRendering();
        else
        {
            StopRendering();
            SleepUntilExpiry();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopRendering();
        _holdWake.Stop();
        _holdWake.Tick -= OnHoldWake;
        SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        foreach (var window in _windows.Values) window.Close();
        _windows.Clear();
        _content.Clear();
        _schedule.Clear();
    }
}
