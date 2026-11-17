using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// P003-C02 normal desktop-only Modern host: one bounded wave at a time,
/// separate click-through HWNDs, and delayed batching of same-burst events.
/// No native Steam client, sound, GC data modification or in-game injection.
/// </summary>
internal sealed class ModernToastHost : IDisposable
{
    private readonly ModernToastScheduler _schedule = new();
    private readonly Dictionary<string, ModernToastWindow> _windows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModernToastContent> _content = new(StringComparer.Ordinal);
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _holdWake;
    private readonly DispatcherTimer _batchWake;
    private readonly DispatcherTimer _anchorWake;
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
        // Coalesce events emitted in one GT-610 reconciliation/burst so that
        // three adjacent awards can begin in the SAME wave. This 45ms delay
        // is an ENGINEERING value; it is not an original Steam constant.
        _batchWake = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(45)
        };
        _batchWake.Tick += OnBatchWake;
        _anchorWake = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _anchorWake.Tick += OnAnchorWake;
        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    internal bool IsIdle => _schedule.IsIdle && _windows.Count == 0 && !_batchWake.IsEnabled;

    internal void SetSettings(ClassicPresentationSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var valid = settings.Validated();
        _fpsCap = valid.AnimationFps;
        _sizePercent = valid.PopupSizePercent;
        _frameGate.Reset();
        if (!_batchWake.IsEnabled) Tick(); // only relayout, never restart a wave
    }

    internal void Enqueue(UnlockEvent evt) =>
        Enqueue(evt, ModernToastContent.Unlock(evt.AchievementId, ""));

    private void Enqueue(UnlockEvent evt, ModernToastContent content)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_schedule.IsIdle && !_batchWake.IsEnabled)
            Tick(); // rebase the monotonic scheduler before any new event
        if (!_schedule.Enqueue(evt)) return;
        _content.Add(evt.EventId, content);

        if (_schedule.ActiveCount == 0 && !_batchWake.IsEnabled)
            _batchWake.Start();
        // During an existing wave, arrivals are queued. The scheduler's
        // next wave cannot be admitted until all active members leave.
    }

    internal void PreviewUnlock() =>
        Enqueue(PreviewEvent(),
            ModernToastContent.Unlock("Champion", "Win 100 games."));

    internal void PreviewRare() =>
        Enqueue(PreviewEvent(),
            ModernToastContent.Unlock("Interstellar",
                "Accumulate one mile of distance.", isRare: true));

    internal void PreviewProgress() =>
        Enqueue(PreviewEvent(), ModernToastContent.Progress("Arrest Warrant", 19, 50));

    internal void PreviewWave(int count)
    {
        if (count is not (1 or 2 or 3 or 6))
            throw new ArgumentOutOfRangeException(nameof(count));
        ModernToastContent[] examples =
        [
            ModernToastContent.Unlock("Champion", "Win 100 games."),
            ModernToastContent.Unlock("Interstellar",
                "Accumulate one mile of distance.", isRare: true),
            ModernToastContent.Unlock("NEW_ACHIEVEMENT_NAME_0_4",
                "NEW_ACHIEVEMENT_DESC_0_4"),
            ModernToastContent.Unlock("Champion", "Win 100 games."),
            ModernToastContent.Unlock("Interstellar",
                "Accumulate one mile of distance.", isRare: true),
            ModernToastContent.Progress("Arrest Warrant", 19, 50)
        ];
        // This callback is synchronous: all N fixtures enter the same 45ms
        // admission batch, and events beyond 3 await the following wave.
        for (var index = 0; index < count; index++)
            Enqueue(PreviewEvent(), examples[index]);
    }

    private static UnlockEvent PreviewEvent() =>
        new("modern-preview-" + Guid.NewGuid().ToString("N"),
            "rasc-local-preview", "REFERENCE-FIXTURE-NOT-AN-AWARD",
            0, DateTimeOffset.UtcNow);

    private void OnAnchorWake(object? sender, EventArgs args)
    {
        // WPF Rendering is unsubscribed during the normal five-second hold.
        // Reposition only: timers remain based on the original monotonic
        // waveStart and never restart when focus/monitor/app bounds change.
        if (!_disposed && _windows.Count > 0) Tick();
    }

    private void OnBatchWake(object? sender, EventArgs args)
    {
        _batchWake.Stop();
        if (_schedule.ActiveCount == 0 && _schedule.WaitingCount > 0)
            _schedule.AlignPendingWaveStart(_clock.Elapsed);
        Tick();
    }

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

    private static double LegacyDesktopScale(Rect work, int requested)
    {
        // EXACT P003-C02 desktop behavior, irrespective of whatever normal
        // app happens to hold focus. Do not shrink to its client rectangle.
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
        // Before the first wave, avoid dispatch events from opening a partial
        // wave before the short burst-collection timer has elapsed.
        if (_batchWake.IsEnabled && _schedule.ActiveCount == 0) return;

        var frames = _schedule.Advance(_clock.Elapsed);
        var visible = new HashSet<string>(
            frames.Select(f => f.Event.EventId), StringComparer.Ordinal);
        foreach (var id in _windows.Keys.Where(id => !visible.Contains(id)).ToArray())
        {
            _windows[id].Close();
            _windows.Remove(id);
            _content.Remove(id);
        }

        var fullscreen = ForegroundWindowPlacement.CaptureFullscreen();
        var work = SystemParameters.WorkArea;
        var scale = fullscreen is { } target
            ? target.ModernScale(_sizePercent)
            : LegacyDesktopScale(work, _sizePercent);
        // Scheduler returns top-to-bottom. Last-created lower windows naturally
        // cover earlier upper ones while entering from the common origin.
        foreach (var frame in frames)
        {
            if (!_windows.TryGetValue(frame.Event.EventId, out var window))
            {
                window = new ModernToastWindow(_content[frame.Event.EventId]);
                _windows.Add(frame.Event.EventId, window);
                window.Apply(frame, scale, work, fullscreen);
                window.Show();
            }
            else window.Apply(frame, scale, work, fullscreen);
        }

        if (_schedule.IsIdle)
        {
            StopRendering();
            _holdWake.Stop();
            _anchorWake.Stop();
        }
        else if (_schedule.IsAnimating)
        {
            _anchorWake.Start();
            StartRendering();
        }
        else
        {
            _anchorWake.Start();
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
        _batchWake.Stop();
        _anchorWake.Stop();
        _holdWake.Tick -= OnHoldWake;
        _anchorWake.Tick -= OnAnchorWake;
        _batchWake.Tick -= OnBatchWake;
        SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        foreach (var window in _windows.Values) window.Close();
        _windows.Clear();
        _content.Clear();
        _schedule.Clear();
    }
}
