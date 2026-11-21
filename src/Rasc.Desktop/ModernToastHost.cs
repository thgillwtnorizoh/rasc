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
    // One owner of FIFO grouping and source-receipt deadlines; presentation
    // never assigns the arrival time of a queued award to its animation clock.
    private readonly ModernAchievementWaveQueue _waveQueue = new();
    private readonly ModernWaveCueGate _cueGate = new();
    // ONE shared topmost window represents the full Modern wave, rather than
    // one DWM-composited HWND per card (the source of inter-card seams).
    private ModernToastWindow? _window;
    private readonly Dictionary<string, ModernToastContent> _content = new(StringComparer.Ordinal);
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _holdWake;
    private readonly DispatcherTimer _batchWake;
    private readonly DispatcherTimer _anchorWake;
    // Independent wall-clock watchdog while any wave/batch is outstanding.
    // Owner still saw second waves stalled until another event after C02's
    // render-independent WPF DispatcherTimer fix; ThreadPool timer posts a
    // message even if CompositionTarget/DispatcherTimer ticks are throttled.
    private readonly System.Threading.Timer _independentWake;
    private readonly Dispatcher _dispatcher;
    private bool _watchdogArmed;
    private DateTimeOffset? _collectionWakeAt;
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
        _dispatcher = dispatcher;
        _independentWake = new System.Threading.Timer(_ =>
        {
            if (_disposed) return;
            try
            {
                if (!dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
                    _ = dispatcher.BeginInvoke(
                        new Action(OnIndependentWake), DispatcherPriority.Normal);
            }
            catch (InvalidOperationException)
            {
                // Dispatcher shutting down; never run WPF work on the timer thread.
            }
        }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _holdWake = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
        _holdWake.Tick += OnHoldWake;
        // Strict fixed 800ms collection period from the first RASC source
        // signal, not 45ms from an arbitrary UI dispatcher callback.
        _batchWake = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
        {
            Interval = ModernAchievementWaveQueue.CollectionWindow
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

    internal bool IsIdle => _schedule.IsIdle && _waveQueue.PendingWaveCount == 0 &&
        _window is null && !_batchWake.IsEnabled;

    internal void SetSettings(ClassicPresentationSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var valid = settings.Validated();
        _fpsCap = valid.AnimationFps;
        _sizePercent = valid.PopupSizePercent;
        _frameGate.Reset();
        Tick(); // only relayout or advance an actually completed wave
    }

    internal void ConfigureCustomization(PopupFontChoice fontChoice,
        ModernTypographyProfile typography, Action? playCue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _fontChoice = fontChoice;
        _typography = Enum.IsDefined(typography)
            ? typography : ModernTypographyProfile.RascAccepted;
        _playCue = playCue;
        Tick(); // restyle without altering collection deadline or active wave
    }

    internal void Enqueue(UnlockEvent evt) =>
        Enqueue(evt, ModernToastContent.Unlock(evt.AchievementId, ""));

    internal void Enqueue(UnlockEvent evt, ResolvedAchievementToast toast) =>
        Enqueue(evt, ModernToastContent.Unlock(
            toast.Name, toast.Description, toast.IsRare, toast.IconJpeg));

    private void Enqueue(UnlockEvent evt, ModernToastContent content)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_waveQueue.Enqueue(evt)) return;
        _content.Add(evt.EventId, content);
        // This never starts a wave before 800ms from the first SOURCE signal.
        // Subsequent awards cannot enter an already started wave, even if it
        // contains just one card. Overflow is immutable FIFO group order.
        Tick();
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
        _collectionWakeAt = null;
        Tick();
    }

    private void OnIndependentWake()
    {
        if (!_disposed) Tick();
    }

    private void ArmWatchdogIfNeeded()
    {
        if (_disposed) return;
        bool needed = !_schedule.IsIdle ||
            _waveQueue.PendingWaveCount != 0 || _batchWake.IsEnabled;
        if (needed == _watchdogArmed) return;
        _watchdogArmed = needed;
        var cadence = TimeSpan.FromMilliseconds(250);
        _independentWake.Change(
            needed ? cadence : Timeout.InfiniteTimeSpan,
            needed ? cadence : Timeout.InfiniteTimeSpan);
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

    private void ScheduleCollectionWake()
    {
        // Only a completely idle presenter may start the next FIFO wave.
        // Timers may wake late, but no event's presentation lifetime is
        // advanced while it remains in the collection queue.
        var next = _schedule.IsIdle ? _waveQueue.NextReadyAt : null;
        if (next is null)
        {
            _batchWake.Stop();
            _collectionWakeAt = null;
            return;
        }
        if (_batchWake.IsEnabled && _collectionWakeAt == next) return;
        _collectionWakeAt = next;
        var remaining = next.Value - DateTimeOffset.UtcNow;
        _batchWake.Interval = TimeSpan.FromMilliseconds(
            Math.Clamp(Math.Ceiling(remaining.TotalMilliseconds), 1, 60000));
        _batchWake.Stop();
        _batchWake.Start();
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
        var now = _clock.Elapsed;
        // First retire the old wave. The next queued cohort is not allowed to
        // inherit its historical end-time: its own animation starts NOW.
        var frames = _schedule.Advance(now);
        if (_schedule.IsIdle)
        {
            var ready = _waveQueue.TakeReady(DateTimeOffset.UtcNow);
            if (ready is not null)
            {
                _schedule.BeginSealedWave(ready.Achievements, now);
                frames = _schedule.Advance(now);
            }
        }
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
            if (_schedule.IsIdle && _waveQueue.PendingWaveCount == 0)
                _content.Clear();
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
        ScheduleCollectionWake();
        ArmWatchdogIfNeeded();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _independentWake.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _independentWake.Dispose();
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
        _waveQueue.Clear();
    }
}
