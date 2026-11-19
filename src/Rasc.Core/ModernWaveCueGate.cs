namespace Rasc.Core;

/// <summary>
/// Emits one presentation cue for each newly rendered Modern wave, including
/// consecutive queued waves that reuse the same WPF HWND. A wave's first
/// event has a stable ID and cannot occur in another wave in one session.
/// This is independent of window creation, text changes and frame cadence.
/// </summary>
public sealed class ModernWaveCueGate
{
    private string? _presentedWaveFirstEventId;

    /// <summary>
    /// Call for every scheduler Advance result used to render the window.
    /// Returns true once for each new nonempty wave. Empty results reset the
    /// gate; neither a queued-only event nor repeated rendering triggers it.
    /// </summary>
    public bool Observe(IReadOnlyList<ModernToastFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count == 0)
        {
            _presentedWaveFirstEventId = null;
            return false;
        }

        var firstId = frames[0].Event.EventId;
        if (firstId == _presentedWaveFirstEventId)
            return false;

        _presentedWaveFirstEventId = firstId;
        return true;
    }
}
