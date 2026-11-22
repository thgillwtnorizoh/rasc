namespace Rasc.Core;

/// <summary>
/// Read-only Windows installation selection. A manually supplied Steam root
/// takes precedence; otherwise the default Program Files (x86) location,
/// then Program Files, then a deterministic order of external discoveries.
/// Do not silently switch to another Steam client when the selected one is
/// missing an asset: that would mix incompatible client installations.
/// </summary>
public static class InstallationDiscovery
{
    public const string SteamDesktopWav = "steamui/sounds/desktop_toast_default.wav";
    public const string SteamDeckWav = "steamui/sounds/deck_ui_achievement_toast.wav";
    public const string GcFolder = "csgo_gc";
    public const string GcFilename = "achievements.txt";
    public const string GcJournalFilename = "achievement_log";

    public static SteamInstallReport FindSteam(
        string? programFilesX86, string? programFiles,
        IEnumerable<string> otherRoots, string? manualRoot = null)
    {
        ArgumentNullException.ThrowIfNull(otherRoots);
        var roots = new List<string>();
        void Add(string? path)
        {
            var full = Normalize(path);
            if (full is not null && !roots.Contains(full, StringComparer.OrdinalIgnoreCase))
                roots.Add(full);
        }
        // Explicit manual selection wins even when files are missing. The UI
        // must tell the owner what is missing rather than use a different client.
        if (!string.IsNullOrWhiteSpace(manualRoot))
        {
            var full = Normalize(manualRoot);
            return full is null ? new(null, true, ["Steam installation folder"])
                : ExamineSteam(full, true);
        }
        Add(programFilesX86 is null ? null : Path.Combine(programFilesX86, "Steam"));
        Add(programFiles is null ? null : Path.Combine(programFiles, "Steam"));
        foreach (var root in otherRoots.Order(StringComparer.OrdinalIgnoreCase)) Add(root);
        foreach (var root in roots)
        {
            var report = ExamineSteam(root, false);
            if (report.Root is not null) return report;
        }
        return new(null, false, []);
    }

    private static SteamInstallReport ExamineSteam(string root, bool manual)
    {
        var exe = File.Exists(Path.Combine(root, "steam.exe"));
        var ui = Directory.Exists(Path.Combine(root, "steamui"));
        if (!manual && !exe && !ui)
            return new(null, false, []);
        var missing = new List<string>();
        if (!exe) missing.Add("steam.exe");
        if (!ui) missing.Add("steamui/");
        if (!File.Exists(Path.Combine(root, "steamui", "sounds", "desktop_toast_default.wav")))
            missing.Add(SteamDesktopWav);
        if (!File.Exists(Path.Combine(root, "steamui", "sounds", "deck_ui_achievement_toast.wav")))
            missing.Add(SteamDeckWav);
        return new(root, manual, missing);
    }

    public static GcInstallReport FindGc(IEnumerable<string> gameRoots,
        string? manualGameRoot = null)
    {
        ArgumentNullException.ThrowIfNull(gameRoots);
        if (!string.IsNullOrWhiteSpace(manualGameRoot))
        {
            var root = Normalize(manualGameRoot);
            return root is null ? new(null, GcInstallState.NoInstallation, true)
                : ExamineGc(root, true);
        }
        GcInstallReport? missingFile = null;
        GcInstallReport? missingLauncher = null;
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in gameRoots)
        {
            var root = Normalize(candidate);
            if (root is null || !known.Add(root)) continue;
            var found = ExamineGc(root, false);
            if (found.State == GcInstallState.Ready) return found;
            if (found.State == GcInstallState.MissingAchievements)
                missingFile ??= found;
            if (found.State == GcInstallState.MissingLauncher)
                missingLauncher ??= found;
        }
        return missingFile ?? missingLauncher ??
            new(null, GcInstallState.NoInstallation, false);
    }

    public static GcInstallReport ExamineGc(string gameRoot, bool manual = false)
    {
        var root = Normalize(gameRoot);
        if (root is null) return new(null, GcInstallState.NoInstallation, manual);
        var folder = Directory.Exists(Path.Combine(root, GcFolder));
        var exe = File.Exists(Path.Combine(root, "csgo.exe"));
        if (!folder && !exe && !manual)
            return new(null, GcInstallState.NoInstallation, false);
        if (!folder || !exe)
            return new(root, GcInstallState.MissingLauncher, manual);
        if (!File.Exists(Path.Combine(root, GcFolder, GcFilename))
            && !File.Exists(Path.Combine(root, GcFolder, GcJournalFilename)))
            return new(root, GcInstallState.MissingAchievements, manual);
        return new(root, GcInstallState.Ready, manual);
    }

    public static string? GameRootFromAchievementsPath(string? path)
    {
        var file = Normalize(path);
        if (file is null ||
            (!Path.GetFileName(file).Equals(GcFilename, StringComparison.OrdinalIgnoreCase)
             && !Path.GetFileName(file).Equals(GcJournalFilename, StringComparison.OrdinalIgnoreCase)))
            return null;
        var parent = Path.GetDirectoryName(file);
        if (parent is null || !Path.GetFileName(parent).Equals(GcFolder, StringComparison.OrdinalIgnoreCase))
            return null;
        return Path.GetDirectoryName(parent);
    }

    private static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try { return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim())); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        { return null; }
    }
}

public sealed record SteamInstallReport(
    string? Root, bool Manual, IReadOnlyList<string> Missing)
{
    public bool Found => Root is not null;
    public bool Ready => Found && Missing.Count == 0;
}

public enum GcInstallState
{
    NoInstallation,
    MissingLauncher,
    MissingAchievements,
    Ready
}

public sealed record GcInstallReport(
    string? GameRoot, GcInstallState State, bool Manual)
{
    public string? AchievementsPath => State == GcInstallState.Ready && GameRoot is not null
        ? Path.Combine(GameRoot, InstallationDiscovery.GcFolder,
            InstallationDiscovery.GcFilename) : null;
}
