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
    private sealed record Content(string Title, string Description, byte[]? IconJpeg);
    private readonly ClassicToastScheduler _schedule = new();
    private readonly Dictionary<string, ClassicToastCard> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Content> _text = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _holdWake;
    private readonly DispatcherTimer _anchorWake;
    private readonly ClassicAnimationFrameGate _frameGate = new();
    private PopupFontChoice _fontChoice = PopupFontChoice.Original;
    private Action? _playCue;
    private int _fpsCap;
    private int _popupSizePercent;
    private long _appliedAnimationUpdates;
    private long _appliedIntervals;
    private double _appliedIntervalSeconds;
    private TimeSpan? _lastAppliedAt;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
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
        _holdWake = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
        _holdWake.Tick += OnHoldWake;
        _anchorWake = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _anchorWake.Tick += OnAnchorWake;
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
        if (!_schedule.Enqueue(evt)) return;
        _text[evt.EventId] = new Content(title, description, jpeg);
        Tick();
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
        if (_disposed || !_renderSubscribed) return;
        // Rendering may be raised more than once for the same composition time
        // if layout is invalidated during a frame. Avoid counting duplicate
        // callbacks as distinct displayed frames.
        if (args is RenderingEventArgs render)
        {
            if (render.RenderingTime == _previousRenderTime) return;
            _previousRenderTime = render.RenderingTime;
        }
        var at = _clock.Elapsed;
        if (_previousCallbackAt is { } last && at > last)
        {
            var gap = at - last;
            _validIntervals++;
            _totalIntervalSeconds += gap.TotalSeconds;
            _maxFrameIntervalMilliseconds = Math.Max(_maxFrameIntervalMilliseconds,
                gap.TotalMilliseconds);
        }
        _previousCallbackAt = at;
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
        CompositionTarget.Rendering += OnRendering;
        _renderSubscribed = true;
    }

    private void StopFrameUpdates()
    {
        if (!_renderSubscribed) return;
        CompositionTarget.Rendering -= OnRendering;
        _renderSubscribed = false;
        _previousRenderTime = null;
        _previousCallbackAt = null;
        _lastAppliedAt = null;
        _frameGate.Reset();
    }

    private void SleepUntilNextExpiry()
    {
        var next = _schedule.NextHoldDeadline;
        if (!next.HasValue) return;
        var remaining = next.Value - _clock.Elapsed;
        _holdWake.Interval = TimeSpan.FromMilliseconds(
            Math.Clamp(Math.Ceiling(remaining.TotalMilliseconds), 1.0, 60_000.0));
        _holdWake.Stop();
        _holdWake.Start();
    }

    private void Tick()
    {
        if (_disposed) return;
        var frames = _schedule.Advance(_clock.Elapsed);
        _window?.PositionOnForegroundApp();
        var visible = new HashSet<string>(frames.Select(f => f.Event.EventId),
            StringComparer.Ordinal);
        foreach (var id in _cards.Keys.Where(id => !visible.Contains(id)).ToArray())
        {
            _window?.Remove(_cards[id]);
            _cards.Remove(id);
            _text.Remove(id);
        }

        if (frames.Count == 0)
        {
            if (_window is not null)
            {
                _window.DisplayMetricsChanged -= OnDisplayMetricsChanged;
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
                _window.SetPopupSizePercent(_popupSizePercent);
                _window.DisplayMetricsChanged += OnDisplayMetricsChanged;
                _window.Show();
                _anchorWake.Start();
            }
            foreach (var frame in frames)
            {
                if (!_cards.TryGetValue(frame.Event.EventId, out var card))
                {
                    var content = _text[frame.Event.EventId];
                    card = new ClassicToastCard(content.Title, content.Description, content.IconJpeg);
                    card.ApplyFontChoice(_fontChoice);
                    _cards.Add(frame.Event.EventId, card);
                    // Playback begins with the actual visual entrance,
                    // not while a toast is queued or on every animation frame.
                    if (frame.Stage == ClassicToastStage.Entering) _playCue?.Invoke();
                }
                _window.Apply(card, frame);
            }
        }

        if (_schedule.IsIdle)
        {
            StopFrameUpdates();
            _holdWake.Stop();
        }
        else if (_schedule.IsAnimating)
        {
            StartFrameUpdates();
        }
        else
        {
            StopFrameUpdates();
            SleepUntilNextExpiry();
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
        _schedule.Clear();
        if (_window is not null)
        {
            _window.DisplayMetricsChanged -= OnDisplayMetricsChanged;
            _window.Close();
            _window = null;
        }
        _cards.Clear();
        _text.Clear();
        WriteRenderDiagnostics();
    }
}
