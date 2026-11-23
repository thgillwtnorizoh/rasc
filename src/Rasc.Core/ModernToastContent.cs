namespace Rasc.Core;

public enum ModernToastKind { Unlock, Progress }

/// <summary>
/// Presentation-only content, distinct from trusted GC events. GT-610 does NOT
/// persist progress callbacks or rarity, so live events cannot invent either.
/// </summary>
public sealed record ModernToastContent(
    string Name,
    string Description,
    ModernToastKind Kind = ModernToastKind.Unlock,
    int Current = 0,
    int Maximum = 0,
    bool IsRare = false,
    int Minimum = 0,
    byte[]? IconJpeg = null)
{
    public static ModernToastContent Unlock(string name, string description, bool isRare = false,
        byte[]? iconJpeg = null) =>
        new(Nonempty(name), description ?? "", ModernToastKind.Unlock, 0, 0, isRare,
            0, iconJpeg);

    /// <summary>
    /// Steam's progress component receives current, minimum and maximum:
    /// the filled percentage is (current - minimum) / (maximum - minimum).
    /// Current can be below minimum; the source UI clamps the bar to 0%.
    /// The GT-610 persistence source supplies none of these live callbacks.
    /// </summary>
    public static ModernToastContent Progress(string name, int current, int maximum, int minimum = 0, byte[]? iconJpeg = null)
    {
        if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        if (minimum < 0 || minimum >= maximum)
            throw new ArgumentOutOfRangeException(nameof(minimum));
        if (current < 0 || current > maximum)
            throw new ArgumentOutOfRangeException(nameof(current));
        return new(Nonempty(name), "", ModernToastKind.Progress,
            current, maximum, false, minimum, iconJpeg);
    }

    public double ProgressFraction =>
        Kind == ModernToastKind.Progress && Maximum > Minimum
            ? Math.Clamp((double)(Current - Minimum) / (Maximum - Minimum), 0.0, 1.0)
            : 0.0;

    public string NumericProgress =>
        Kind == ModernToastKind.Progress ? $"({Current}/{Maximum})" : "";

    private static string Nonempty(string name) =>
        !string.IsNullOrWhiteSpace(name)
            ? name : throw new ArgumentException("Achievement name required.", nameof(name));
}
