using System.Diagnostics;
using System.Windows.Threading;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Main-window-owned WPF presentation adapter. Only NEW monitor events enqueue.
/// One childless, click-through stack host owns all animated card visuals.
/// Caller must use the UI dispatcher; source store/GC file are never modified.
/// </summary>
internal sealed class ClassicToastHost : IDisposable
{
    private sealed record Content(string Title, string Description);
    private readonly ClassicToastScheduler _schedule = new();
    private readonly Dictionary<string, ClassicToastCard> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Content> _text = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private ClassicToastStackWindow? _window;
    private bool _disposed;

    internal ClassicToastHost(Dispatcher dispatcher)
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnTick;
    }

    internal void Enqueue(UnlockEvent evt) =>
        Enqueue(evt, "Achievement Unlocked!", evt.AchievementId);

    private void Enqueue(UnlockEvent evt, string title, string description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Synchronize the clock BEFORE enqueueing. Otherwise, an item received
        // after an idle interval might be incorrectly time-traveled into the past.
        Tick();
        if (!_schedule.Enqueue(evt)) return;
        _text[evt.EventId] = new Content(title, description);
        if (!_timer.IsEnabled) _timer.Start();
        Tick();
    }

    internal void Preview() => PreviewUnlock();

    internal void PreviewUnlock()
    {
        Enqueue(PreviewEvent(), "Achievement Unlocked!", "Someone Set Up Us The Bomb");
    }

    internal void PreviewProgress()
    {
        // Reference fixture ONLY. GT-610's stats file has no durable progress stream.
        Enqueue(PreviewEvent(), "Achievement Progress",
            "Paranatural Collection (20,010/100,000)");
    }

    private static UnlockEvent PreviewEvent()
    {
        var now = DateTimeOffset.UtcNow;
        return new UnlockEvent("preview-" + Guid.NewGuid().ToString("N"),
            "rasc-local-preview", "REFERENCE-FIXTURE-NOT-AN-AWARD", 0, now);
    }

    private void OnTick(object? sender, EventArgs e) => Tick();

    private void Tick()
    {
        if (_disposed) return;
        var frames = _schedule.Advance(_clock.Elapsed);
        var visible = new HashSet<string>(frames.Select(x => x.Event.EventId),
            StringComparer.Ordinal);
        foreach (var id in _cards.Keys.Where(id => !visible.Contains(id)).ToArray())
        {
            if (_window is not null) _window.Remove(_cards[id]);
            _cards.Remove(id);
            _text.Remove(id);
        }

        if (frames.Count == 0)
        {
            _window?.Close();
            _window = null;
        }
        else
        {
            if (_window is null)
            {
                _window = new ClassicToastStackWindow();
                _window.PositionOnPrimaryWorkArea();
                _window.Show();
            }
            foreach (var f in frames)
            {
                if (!_cards.TryGetValue(f.Event.EventId, out var card))
                {
                    var content = _text[f.Event.EventId];
                    card = new ClassicToastCard(content.Title, content.Description);
                    _cards.Add(f.Event.EventId, card);
                }
                _window.Apply(card, f);
            }
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
        if (_window is not null)
        {
            _window.Close();
            _window = null;
        }
        _cards.Clear();
        _text.Clear();
    }
}
