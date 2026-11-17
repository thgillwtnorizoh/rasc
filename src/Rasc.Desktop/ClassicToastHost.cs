using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// WPF presentation adapter. All methods must be called on the owning UI thread.
/// It consumes only new in-session unlocks; history rehydration never calls Enqueue.
/// </summary>
internal sealed class ClassicToastHost : IDisposable
{
    private const double EdgeMarginDips = 16; // ENGINEERING, original stack geometry unknown
    private const double InterToastGapDips = 6; // ENGINEERING, original stack geometry unknown
    private readonly ClassicToastScheduler _schedule = new();
    private readonly Dictionary<string, ClassicToastWindow> _windows = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _disposed;

    internal ClassicToastHost(Dispatcher dispatcher)
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _timer.Tick += OnTick;
    }

    internal void Enqueue(UnlockEvent evt)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_schedule.Enqueue(evt)) return;
        if (!_timer.IsEnabled) _timer.Start();
        Tick();
    }

    internal void Preview()
    {
        var now = DateTimeOffset.UtcNow;
        Enqueue(new UnlockEvent("preview-" + Guid.NewGuid().ToString("N"),
            "rasc-local-preview", "RASC PREVIEW — NOT AN AWARD", 0, now));
    }

    private void OnTick(object? sender, EventArgs e) => Tick();

    private void Tick()
    {
        if (_disposed) return;
        var frames = _schedule.Advance(_clock.Elapsed);
        var alive = new HashSet<string>(frames.Select(f => f.Event.EventId), StringComparer.Ordinal);
        foreach (var id in _windows.Keys.Where(id => !alive.Contains(id)).ToArray())
        {
            _windows[id].Close();
            _windows.Remove(id);
        }

        var work = SystemParameters.WorkArea;
        foreach (var f in frames)
        {
            if (!_windows.TryGetValue(f.Event.EventId, out var window))
            {
                window = new ClassicToastWindow(f.Event.AchievementId) { Opacity = 0 };
                _windows.Add(f.Event.EventId, window);
                window.Show();
            }
            window.Left = work.Right - EdgeMarginDips - 240;
            window.Top = work.Bottom - EdgeMarginDips - 94 -
                         f.Slot * (94 + InterToastGapDips) + f.VerticalOffsetDips;
            window.Opacity = f.Opacity;
        }

        if (_schedule.IsIdle) _timer.Stop();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _schedule.Clear();
        foreach (var window in _windows.Values) window.Close();
        _windows.Clear();
    }
}
