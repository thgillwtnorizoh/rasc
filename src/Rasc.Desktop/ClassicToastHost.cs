using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Main-window-owned presentation. Only new monitor events enqueue; previews
/// never write to GC or SQLite. WPF Rendering drives ONLY transitions, while a
/// one-shot dispatcher wake handles six-second holds without wasting frames.
/// Rendering callbacks are counted for diagnostics, not claimed presented FPS.
/// </summary>
internal sealed class ClassicToastHost : IDisposable
{
    // The scheduler owns lifetime + full content; WPF caches only visual objects.
    // No separate ID->title/image dictionary can outlive or race a card's exit.
    private readonly ClassicToastScheduler _schedule = new();
    private readonly Dictionary<string, ClassicToastCard> _cards = new(StringComparer.Ordinal);
    // Never print event IDs or achievement names: stages and geometry are
    // sufficient to diagnose stuck Classic slide/window behavior.
    private readonly Dictionary<string, ClassicToastStage> _loggedStage = new(StringComparer.Ordinal);
    private bool _loggedRenderStall;
    private readonly DispatcherTimer _holdWake;
    private readonly DispatcherTimer _anchorWake;
    // CompositionTarget.Rendering can stop while a fullscreen game owns the
    // foreground desktop. Independently advance 450ms transitions so a card
    // cannot remain partially below the screen when rendering is throttled.
    private readonly DispatcherTimer _animationFallback;
    private readonly ClassicAnimationFrameGate _frameGate = new();
    // Internal WPF smoke seam: simulate no CompositionTarget callbacks.
    internal bool SuppressCompositionCallbacksForSmoke { get; set; }
    private PopupFontChoice _fontChoice = PopupFontChoice.Original;
    private Action? _playCue;
    private int _fpsCap;
    private int _popupSizePercent;
    private long _appliedAnimationUpdates;
    private long _appliedIntervals;
    private double _appliedIntervalSeconds;
    private TimeSpan? _lastAppliedAt;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Dispatcher _dispatcher;
    // WPF may synchronously reenter Rendering during HWND resize/layout;
    // advancing the scheduler while an older frame batch is still being
    // materialized used to retire card content needed by that outer batch.
    private bool _tickActive;
    private bool _tickQueued;
    private long _tickDeferrals;
    internal Func<TimeSpan>? TimeOverrideForSmoke { get; set; }
    internal Action? FramesCapturedForSmoke { get; set; }
    internal long TickDeferralsForSmoke => _tickDeferrals;
    internal void AdvanceForSmoke() => Tick();
    private TimeSpan ClockTime => TimeOverrideForSmoke?.Invoke() ?? _clock.Elapsed;
    private ClassicToastStackWindow? _window;
    private bool _renderSubscribed;
    private bool _disposed;
    private TimeSpan? _previousRenderTime;
    private TimeSpan? _previousCallbackAt;
    private long _renderCallbacks;
    private long _validIntervals;
    private int _animationSegments;
    private double _totalIntervalSeconds;
    private double _maxFrameIntervalMilliseconds;

    internal ClassicToastHost(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _holdWake = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
        _holdWake.Tick += OnHoldWake;
        _anchorWake = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _anchorWake.Tick += OnAnchorWake;
        _animationFallback = new DispatcherTimer(DispatcherPriority.Render, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _animationFallback.Tick += OnAnimationFallback;
    }

    internal bool IsIdle => _schedule.IsIdle && _cards.Count == 0 && _window is null;

    internal void SetSettings(ClassicPresentationSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var valid = settings.Validated();
        if (_fpsCap == valid.AnimationFps && _popupSizePercent == valid.PopupSizePercent)
            return;
        _fpsCap = valid.AnimationFps;
        _popupSizePercent = valid.PopupSizePercent;
        AppLog.Info("classic.settings", $"fps={_fpsCap}; sizePercent={_popupSizePercent}");
        _frameGate.Reset();
        _window?.SetPopupSizePercent(_popupSizePercent);
        Tick(); // Immediately relayout any visible stack, no new unlock is generated.
    }

    internal void ConfigureCustomization(PopupFontChoice fontChoice, Action? playCue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _fontChoice = fontChoice;
        _playCue = playCue;
        foreach (var card in _cards.Values) card.ApplyFontChoice(fontChoice);
    }

    internal void Enqueue(UnlockEvent evt) =>
        Enqueue(evt, "Achievement Unlocked!", evt.AchievementId);

    internal void Enqueue(UnlockEvent evt, ResolvedAchievementToast toast) =>
        // The Classic two-line layout is a fixed notification headline and
        // the localized achievement NAME, not the long goal description.
        Enqueue(evt, "Achievement Unlocked!", toast.Name, toast.IconJpeg);

    private void Enqueue(UnlockEvent evt, string title, string description, byte[]? jpeg = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Bring the scheduler to NOW before enqueue; idle time must not
        // make a newly received achievement appear to have arrived earlier.
        Tick();
        if (!_schedule.Enqueue(evt, new ClassicToastContent(title, description, jpeg))) return;
        AppLog.Info("classic.queue", $"accepted; waiting={_schedule.WaitingCount}; active={_schedule.ActiveCount}");
        Tick();
    }

    internal void EnqueueProgress(UnlockEvent evt, ResolvedAchievementToast toast,
        int current, int goal)
    {
        if (!evt.IsProgress)
            throw new ArgumentException("Progress event kind required.", nameof(evt));
        Enqueue(evt, "Achievement Progress",
            $"{toast.Name} ({current:N0}/{goal:N0})", toast.IconJpeg);
    }

    /// <summary>Source-accepted award cancels waiting, not visible, same-ID progress.</summary>
    internal void MarkEarned(string achievementId)
    {
        if (_disposed) return;
        var canceled = _schedule.MarkEarned(achievementId);
        if (canceled.Count > 0)
        {
            AppLog.Info("classic.progress", $"canceled={canceled.Count}");
            Tick();
        }
    }

    internal void Preview() => PreviewUnlock();

    internal void PreviewUnlock() =>
        Enqueue(PreviewEvent(), "Achievement Unlocked!", "Someone Set Up Us The Bomb");

    internal void PreviewProgress() =>
        Enqueue(PreviewEvent(), "Achievement Progress",
            "Paranatural Collection (20,010/100,000)");

    private static UnlockEvent PreviewEvent()
    {
        var now = DateTimeOffset.UtcNow;
        return new UnlockEvent("preview-" + Guid.NewGuid().ToString("N"),
            "rasc-local-preview", "REFERENCE-FIXTURE-NOT-AN-AWARD", 0, now);
    }

    private void OnRendering(object? sender, EventArgs args)
    {
        if (_disposed || !_renderSubscribed || SuppressCompositionCallbacksForSmoke) return;
        // Rendering may be raised more than once for the same composition time
        // if layout is invalidated during a frame. Avoid counting duplicate
        // callbacks as distinct displayed frames.
        if (args is RenderingEventArgs render)
        {
            if (render.RenderingTime == _previousRenderTime) return;
            _previousRenderTime = render.RenderingTime;
        }
        var at = ClockTime;
        if (_previousCallbackAt is { } last && at > last)
        {
            var gap = at - last;
            _validIntervals++;
            _totalIntervalSeconds += gap.TotalSeconds;
            _maxFrameIntervalMilliseconds = Math.Max(_maxFrameIntervalMilliseconds,
                gap.TotalMilliseconds);
        }
        _previousCallbackAt = at;
        if (_loggedRenderStall)
        {
            AppLog.Info("classic.render", "composition callbacks resumed");
            _loggedRenderStall = false;
        }
        _renderCallbacks++;
        if (!_frameGate.ShouldApply(at, _fpsCap)) return;
        if (_lastAppliedAt is { } previous && at > previous)
        {
            _appliedIntervals++;
            _appliedIntervalSeconds += (at - previous).TotalSeconds;
        }
        _lastAppliedAt = at;
        _appliedAnimationUpdates++;
        Tick();
    }

    private void OnDisplayMetricsChanged(object? sender, EventArgs e)
    {
        // Reapply all clipped card geometry using the new DPI * user scale.
        // Display changes never generate unlock events or restart their timers.
        if (!_disposed) Tick();
    }

    private void OnAnchorWake(object? sender, EventArgs e)
    {
        // Track foreground focus/resize/fullscreen even during quiet holds.
        if (!_disposed) _window?.PositionOnForegroundApp();
    }

    private void OnAnimationFallback(object? sender, EventArgs e)
    {
        if (_disposed || !_renderSubscribed) return;
        var at = ClockTime;
        // Normal WPF composition remains the primary clock. The timer only
        // advances the state after rendering callbacks have gone quiet; it
        // does not impose an additional 60Hz ceiling on healthy renderers.
        if (_previousCallbackAt is { } recent &&
            at - recent < TimeSpan.FromMilliseconds(40)) return;
        if (!_loggedRenderStall)
        {
            AppLog.Warning("classic.render", $"composition stalled; " +
                $"lastCallbackMs={(_previousCallbackAt is { } last ? (at-last).TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture) : "never")}; " +
                $"animationSegments={_animationSegments}");
            _loggedRenderStall = true;
        }
        if (!_frameGate.ShouldApply(at, _fpsCap)) return;
        _appliedAnimationUpdates++;
        Tick();
    }

    private void OnHoldWake(object? sender, EventArgs e)
    {
        _holdWake.Stop();
        Tick();
    }

    private void StartFrameUpdates()
    {
        _holdWake.Stop();
        if (_renderSubscribed) return;
        _previousRenderTime = null;
        _previousCallbackAt = null;
        _lastAppliedAt = null;
        _frameGate.Reset();
        _animationSegments++;
        AppLog.Info("classic.render", $"animation segment started; n={_animationSegments}");
        CompositionTarget.Rendering += OnRendering;
        _renderSubscribed = true;
        _animationFallback.Start();
    }

    private void StopFrameUpdates()
    {
        _animationFallback.Stop();
        if (!_renderSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderSubscribed = false;
        AppLog.Info("classic.render", "animation segment ended");
        _previousRenderTime = null;
        _previousCallbackAt = null;
        _lastAppliedAt = null;
        _frameGate.Reset();
    }

    private void SleepUntilNextExpiry(TimeSpan? next)
    {
        if (!next.HasValue) return;
        var remaining = next.Value - ClockTime;
        _holdWake.Interval = TimeSpan.FromMilliseconds(
            Math.Clamp(Math.Ceiling(remaining.TotalMilliseconds), 1.0, 60_000.0));
        _holdWake.Stop();
        _holdWake.Start();
    }

    private void Tick()
    {
        if (_disposed) return;
        if (_tickActive)
        {
            // Never overwrite/retire a frame's content from a nested callback.
            // Defer the new clock and UI state until the outer batch completes.
            _tickQueued = true;
            _tickDeferrals++;
            if (_tickDeferrals <= 8 || _tickDeferrals % 100 == 0)
                AppLog.Warning("classic.reentry",
                    $"deferred nested Tick; total={_tickDeferrals}");
            return;
        }

        _tickActive = true;
        try
        {
            var passes = 0;
            do
            {
                _tickQueued = false;
                TickCore();
                if (++passes >= 16 && _tickQueued)
                {
                    // Avoid an infinite synchronous resize/render feedback loop.
                    // A queued follow-up happens after the current WPF event.
                    _ = _dispatcher.BeginInvoke(new Action(Tick),
                        DispatcherPriority.Background);
                    break;
                }
            }
            while (_tickQueued && !_disposed);
        }
        finally
        {
            _tickActive = false;
        }
    }

    private void TickCore()
    {
        if (_disposed) return;
        // One serialized scheduler transaction produces complete immutable
        // frames (including metadata), one-shot effects and next wake state.
        // WPF only materializes this snapshot, never advances the scheduler.
        var presentation = _schedule.AdvancePresentation(ClockTime);
        var frames = presentation.Frames;
        FramesCapturedForSmoke?.Invoke();
        _window?.PositionOnForegroundApp();
        var visible = new HashSet<string>(frames.Select(f => f.Event.EventId),
            StringComparer.Ordinal);
        foreach (var id in _cards.Keys.Where(id => !visible.Contains(id)).ToArray())
        {
            AppLog.Info("classic.card", "retired");
            _loggedStage.Remove(id);
            _window?.Remove(_cards[id]);
            _cards.Remove(id);
        }

        if (frames.Count == 0)
        {
            if (_window is not null)
            {
                _window.DisplayMetricsChanged -= OnDisplayMetricsChanged;
                AppLog.Info("classic.window", "closed; stack empty");
                _window.Close();
                _window = null;
                _anchorWake.Stop();
            }
        }
        else
        {
            if (_window is null)
            {
                _window = new ClassicToastStackWindow();
                AppLog.Info("classic.window", "create; popup stack 240x282 design units");
                _window.SetPopupSizePercent(_popupSizePercent);
                _window.DisplayMetricsChanged += OnDisplayMetricsChanged;
                _window.Show();
                _anchorWake.Start();
            }
            foreach (var frame in frames)
            {
                if (!_loggedStage.TryGetValue(frame.Event.EventId, out var previousStage)
                    || previousStage != frame.Stage)
                {
                    _loggedStage[frame.Event.EventId] = frame.Stage;
                    AppLog.Info("classic.card",
                        $"stage={frame.Stage}; slot={frame.Slot}; " +
                        $"y={frame.Y:F1}; clipTop={frame.ClipTop:F1}; clipBottom={frame.ClipBottom:F1}");
                }
                if (!_cards.TryGetValue(frame.Event.EventId, out var card))
                {
                    if (frame.Content is not { } content)
                    {
                        // Only old hand-built geometry fixtures can lack content.
                        // Scheduler-produced frames must always have it.
                        AppLog.Error("classic.invariant",
                            $"snapshot lacked content; stage={frame.Stage}; " +
                            $"slot={frame.Slot}; frames={frames.Count}");
                        continue;
                    }
                    card = new ClassicToastCard(content.Title, content.Description,
                        content.CopyIconJpeg());
                    card.ApplyFontChoice(_fontChoice);
                    _cards.Add(frame.Event.EventId, card);
                    // Sound is owned by the scheduler's Started transition,
                    // not by WPF object construction or repeated render frames.
                }
                _window.Apply(card, frame);
            }
        }

        // Exactly once for each genuinely presented admission. A frame may
        // skip many transitions after a long clock jump; do not play sounds
        // for toasts that were never rendered.
        foreach (var effect in presentation.Transitions)
        {
            if (effect.Kind == ClassicToastTransitionKind.Started &&
                _cards.ContainsKey(effect.Event.EventId))
                _playCue?.Invoke();
        }

        if (presentation.IsIdle)
        {
            StopFrameUpdates();
            _holdWake.Stop();
        }
        else if (presentation.IsAnimating)
        {
            StartFrameUpdates();
        }
        else
        {
            StopFrameUpdates();
            SleepUntilNextExpiry(presentation.NextHoldDeadline);
        }
    }

    private void WriteRenderDiagnostics()
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RASC");
            Directory.CreateDirectory(folder);
            var fps = _validIntervals != 0 && _totalIntervalSeconds > 0
                ? (_validIntervals / _totalIntervalSeconds).ToString("F1", CultureInfo.InvariantCulture)
                : "NOT MEASURED";
            File.WriteAllText(Path.Combine(folder, "last-classic-animation.txt"),
                "RASC Classic animation callback diagnostics (NOT physical presentation FPS)" +
                Environment.NewLine +
                "FPS cap (0 = display-synced): " + _fpsCap + Environment.NewLine +
                "Popup size percent (0 = Auto): " + _popupSizePercent + Environment.NewLine +
                "Animation segments: " + _animationSegments + Environment.NewLine +
                "Unique WPF Rendering callbacks: " + _renderCallbacks + Environment.NewLine +
                "Callback intervals sampled: " + _validIntervals + Environment.NewLine +
                "Applied animation visual updates: " + _appliedAnimationUpdates + Environment.NewLine +
                "Applied update intervals sampled: " + _appliedIntervals + Environment.NewLine +
                "Average applied update rate (not displayed FPS): " +
                (_appliedIntervals > 0 && _appliedIntervalSeconds > 0
                    ? (_appliedIntervals / _appliedIntervalSeconds)
                        .ToString("F1", CultureInfo.InvariantCulture)
                    : "NOT MEASURED") + Environment.NewLine +
                "Average render callback rate (fps approximation): " + fps + Environment.NewLine +
                "Longest callback interval (ms): " +
                _maxFrameIntervalMilliseconds.ToString("F2", CultureInfo.InvariantCulture) +
                Environment.NewLine +
                "Measured over active slide segments only; idle holds excluded. " +
                "Monitor refresh and actual displayed frames have NOT been instrumented." +
                Environment.NewLine);
        }
        catch (IOException) { /* optional diagnostics never block normal shutdown */ }
        catch (UnauthorizedAccessException) { /* optional only */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopFrameUpdates();
        _holdWake.Stop();
        _holdWake.Tick -= OnHoldWake;
        _anchorWake.Stop();
        _anchorWake.Tick -= OnAnchorWake;
        _animationFallback.Stop();
        _animationFallback.Tick -= OnAnimationFallback;
        _schedule.Clear();
        if (_window is not null)
        {
            _window.DisplayMetricsChanged -= OnDisplayMetricsChanged;
            _window.Close();
            _window = null;
        }
        AppLog.Info("classic", $"dispose; callbacks={_renderCallbacks}; " +
            $"appliedUpdates={_appliedAnimationUpdates}; remainingCards={_cards.Count}");
        _cards.Clear();
        _loggedStage.Clear();
        WriteRenderDiagnostics();
    }
}
