using System.Text.Json;

namespace Rasc.Core;

/// <summary>
/// Classic popup presentation only. Auto FPS means every WPF composition
/// opportunity; a positive FPS value is a maximum requested visual update rate,
/// not a promise of hardware-presented frames. PopupSizePercent=0 means Auto.
/// Never affects source reconciliation or Steam/GC achievements.
/// </summary>
public sealed record ClassicPresentationSettings(int AnimationFps = 0, int PopupSizePercent = 0)
{
    public static readonly int[] SupportedFps = [0, 30, 60, 90, 120, 144, 165, 240];
    public static readonly int[] SupportedSizes = [0, 100, 125, 150, 175, 200, 250];
    public ClassicPresentationSettings Validated() => new(
        SupportedFps.Contains(AnimationFps) ? AnimationFps : 0,
        SupportedSizes.Contains(PopupSizePercent) ? PopupSizePercent : 0);
}

/// <summary>
/// Atomic, bounded, best-effort persistence. Invalid or truncated files fall
/// back to safe defaults; no private GC data appears in this JSON.
/// </summary>
public sealed class ClassicPresentationSettingsStore(string path)
{
    public ClassicPresentationSettings Load()
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length > 65536) return new();
            var content = File.ReadAllText(path);
            return (JsonSerializer.Deserialize<ClassicPresentationSettings>(content)
                ?? new ClassicPresentationSettings()).Validated();
        }
        catch (IOException) { return new(); }
        catch (UnauthorizedAccessException) { return new(); }
        catch (JsonException) { return new(); }
    }

    public bool TrySave(ClassicPresentationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings.Validated()));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

/// <summary>
/// Scale the entire EXISTING 240x94 design uniformly. In Auto, 1080 logical
/// pixels of screen height is the baseline; at 1440 100%-DPI pixels this is
/// 1.333x, at 2160 100%-DPI 2x. OS DPI already reduces logical screen height,
/// avoiding an additional naive DPI multiplier. Never exceed the work area.
/// </summary>
public static class ClassicPopupScale
{
    public static double Resolve(
        double primaryScreenHeightDips,
        double workAreaWidthDips,
        double workAreaHeightDips,
        int requestedPercent)
    {
        if (!double.IsFinite(primaryScreenHeightDips) || primaryScreenHeightDips <= 0 ||
            !double.IsFinite(workAreaWidthDips) || workAreaWidthDips <= 0 ||
            !double.IsFinite(workAreaHeightDips) || workAreaHeightDips <= 0)
            return 1.0;

        var request = ClassicPresentationSettings.SupportedSizes.Contains(requestedPercent)
            ? requestedPercent : 0;
        var scale = request == 0
            ? Math.Clamp(primaryScreenHeightDips / 1080.0, 1.0, 2.0)
            : request / 100.0;
        var maxFit = Math.Min(workAreaWidthDips / ClassicToastScheduler.CardWidth,
            workAreaHeightDips / ClassicToastScheduler.ViewportHeight);
        // If the desktop is exceptionally small, still remain entirely on it.
        return Math.Clamp(scale, Math.Min(1.0, maxFit), maxFit);
    }
}

/// <summary>
/// Samples available WPF Rendering callbacks at a user-chosen maximum rate;
/// the slide's physical timeline remains driven by monotonic elapsed time.
/// </summary>
public sealed class ClassicAnimationFrameGate
{
    private TimeSpan? _lastApplied;
    public void Reset() => _lastApplied = null;

    public bool ShouldApply(TimeSpan now, int fps)
    {
        if (now < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(now));
        if (!ClassicPresentationSettings.SupportedFps.Contains(fps))
            throw new ArgumentOutOfRangeException(nameof(fps));
        if (fps == 0 || !_lastApplied.HasValue)
        {
            _lastApplied = now;
            return true;
        }
        if (now < _lastApplied.Value)
            throw new ArgumentOutOfRangeException(nameof(now), "Rendering time must be monotonic.");
        var minTicks = TimeSpan.TicksPerSecond / fps;
        if (now.Ticks - _lastApplied.Value.Ticks < minTicks) return false;
        _lastApplied = now;
        return true;
    }
}
