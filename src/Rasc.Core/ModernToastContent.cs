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
    bool IsRare = false)
{
    public static ModernToastContent Unlock(string name, string description, bool isRare = false) =>
        new(Nonempty(name), description ?? "", ModernToastKind.Unlock, 0, 0, isRare);

    public static ModernToastContent Progress(string name, int current, int maximum)
    {
        if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        if (current < 0 || current > maximum)
            throw new ArgumentOutOfRangeException(nameof(current));
        return new(Nonempty(name), "", ModernToastKind.Progress, current, maximum);
    }

    public double ProgressFraction =>
        Kind == ModernToastKind.Progress && Maximum > 0
            ? Math.Clamp((double)Current / Maximum, 0.0, 1.0) : 0.0;

    public string NumericProgress =>
        Kind == ModernToastKind.Progress ? $"({Current}/{Maximum})" : "";

    private static string Nonempty(string name) =>
        !string.IsNullOrWhiteSpace(name)
            ? name : throw new ArgumentException("Achievement name required.", nameof(name));
}
