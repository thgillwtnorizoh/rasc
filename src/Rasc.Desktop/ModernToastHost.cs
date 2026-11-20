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
    private readonly ModernWaveCueGate _cueGate = new();
    // ONE shared topmost window represents the full Modern wave, rather than
    // one DWM-composited HWND per card (the source of inter-card seams).
    private ModernToastWindow? _window;
    private readonly Dictionary<string, ModernToastContent> _content = new(StringComparer.Ordinal);
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _holdWake;
    private readonly DispatcherTimer _batchWake;
    private readonly DispatcherTimer _anchorWake;
    private readonly ClassicAnimationFrameGate _frameGate = new();
    private TimeSpan? _lastRenderTime;
    private TimeSpan? _scheduledWakeAt;
    private PopupFontChoice _fontChoice = PopupFontChoice.Original;
    private ModernTypographyProfile _typography = ModernTypographyProfile.RascAccepted;
    private Action? _playCue;
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

    internal bool IsIdle => _schedule.IsIdle && _window is null && !_batchWake.IsEnabled;

    internal void SetSettings(ClassicPresentationSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var valid = settings.Validated();
        _fpsCap = valid.AnimationFps;
        _sizePercent = valid.PopupSizePercent;
        _frameGate.Reset();
        if (!_batchWake.IsEnabled) Tick(); // only relayout, never restart a wave
    }

    internal void ConfigureCustomization(PopupFontChoice fontChoice,
        ModernTypographyProfile typography, Action? playCue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _fontChoice = fontChoice;
        _typography = Enum.IsDefined(typography)
            ? typography : ModernTypographyProfile.RascAccepted;
        _playCue = playCue;
        if (!_batchWake.IsEnabled) Tick(); // restyle existing text; no new event
    }

    internal void Enqueue(UnlockEvent evt) =>
        Enqueue(evt, ModernToastContent.Unlock(evt.AchievementId, ""));

    internal void Enqueue(UnlockEvent evt, ResolvedAchievementToast toast) =>
        Enqueue(evt, ModernToastContent.Unlock(
            toast.Name, toast.Description, toast.IsRare, toast.IconJpeg),
            allowLiveLateAdmission: true);

    private void Enqueue(UnlockEvent evt, ModernToastContent content,
        bool allowLiveLateAdmission = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_schedule.IsIdle && !_batchWake.IsEnabled)
            Tick(); // rebase monotonic time before first arrival

        // A later GC file write may follow the first award by hundreds of
        // milliseconds or seconds. The historical 45ms initial admission
        // window alone could not group those live events. For REAL awards,
        // join the still-visible stack if room remains, with each newcomer
        // sliding in rather than freezing for a 5.6s next-wave wait.
        bool hasActive = _schedule.ActiveCount > 0;
        if (hasActive && allowLiveLateAdmission)
            Tick(); // retire overdue wave before testing live admission

        bool joined = allowLiveLateAdmission && hasActive &&
            _schedule.TryAdmitLiveDuringHold(evt, _clock.Elapsed);
        if (!joined && !_schedule.Enqueue(evt)) return;
        _content.Add(evt.EventId, content);

        if (joined)
        {
            Tick(); // show entrant immediately; preserve old card and cue
        }
        else if (_schedule.ActiveCount == 0 && !_batchWake.IsEnabled)
        {
            _batchWake.Start();
        }
        // Full / past-window live arrivals remain strictly FIFO in the next
        // automatic wave; previews keep established immutable-wave behavior.
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
        if (!_disposed && _window is not null) Tick();
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
        _scheduledWakeAt = null;
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

    private void ScheduleNextTransitionWake()
    {
        var next = _schedule.NextTransitionDeadline;
        if (next is null)
        {
            _holdWake.Stop();
            _scheduledWakeAt = null;
            return;
        }
        // Do not restart the same deadline on each WPF render callback.
        // Otherwise repeated renders can starve a timer indefinitely.
        if (_holdWake.IsEnabled && _scheduledWakeAt == next) return;
        _scheduledWakeAt = next;
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
        // A queued replacement wave may be admitted on the SAME Tick that
        // retires its predecessor, without a frame-empty interval or a new
        // window. Cue identity therefore follows the presented wave, not HWND
        // creation. Observe on every render to keep per-wave state accurate.
        var waveJustPresented = _cueGate.Observe(frames);
        var fullscreen = ForegroundWindowPlacement.CaptureFullscreen();
        var work = SystemParameters.WorkArea;
        var scale = fullscreen is { } target
            ? target.ModernScale(_sizePercent)
            : LegacyDesktopScale(work, _sizePercent);

        if (frames.Count == 0)
        {
            if (_window is not null)
            {
                _window.Close();
                _window = null;
            }
            if (_schedule.IsIdle) _content.Clear();
        }
        else
        {
            var newlyCreated = _window is null;
            _window ??= new ModernToastWindow();
            var retired = _window.Apply(frames, _content, scale, _fontChoice, _typography, work, fullscreen);
            foreach (var id in retired)
                _content.Remove(id);
            if (newlyCreated)
                _window.Show();
            // Fire AFTER the new wave is in the visual tree (and after
            // Show for initial waves). A queued replacement reuses this
            // same HWND but must still play one audio cue.
            if (waveJustPresented)
                _playCue?.Invoke();
        }

        if (_schedule.IsIdle)
        {
            StopRendering();
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
        }
        // WPF compositor may not render while fullscreen CS:GO is active;
        // timer-driven transitions must still retire and present EVERY wave.
        ScheduleNextTransitionWake();
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
        _window?.Close();
        _window = null;
        _content.Clear();
        _schedule.Clear();
    }
}
