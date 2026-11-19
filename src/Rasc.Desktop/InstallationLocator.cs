using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Bounded read-only Steam / GC discovery. Windows registry plus common
/// drive roots locate normal Steam installations; game roots come from
/// Steam's own libraryfolders.vdf and common standalone layouts. This is
/// NOT an unrestricted recursive scan of all user disks.
/// </summary>
internal static class InstallationLocator
{
    internal const string GcReleasesUrl = "https://github.com/GT-610/csgo-gc/releases";
    internal const string SteamDownloadUrl = "https://store.steampowered.com/about/";

    internal static SteamInstallReport FindSteam(string? manualRoot) =>
        InstallationDiscovery.FindSteam(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            OtherSteamRoots(), manualRoot);

    private static IEnumerable<string> OtherSteamRoots()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path)) paths.Add(path);
        }
        // Official Steam client registry settings (read only).
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            Add(key?.GetValue("SteamPath")?.ToString());
            var exe = key?.GetValue("SteamExe")?.ToString();
            if (!string.IsNullOrWhiteSpace(exe))
                Add(Path.GetDirectoryName(exe.Replace('/', Path.DirectorySeparatorChar)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { /* continue with other candidates */ }

        foreach (var drive in ReadyDriveRoots())
        {
            Add(Path.Combine(drive, "Steam"));
            Add(Path.Combine(drive, "Games", "Steam"));
            Add(Path.Combine(drive, "SteamLibrary", "Steam"));
            Add(Path.Combine(drive, "Program Files (x86)", "Steam"));
            Add(Path.Combine(drive, "Program Files", "Steam"));
        }
        return paths.Order(StringComparer.OrdinalIgnoreCase);
    }

    internal static GcInstallReport FindGc(SteamInstallReport steam,
        string? manualGameRoot, string? historicAchievementsPath)
    {
        return InstallationDiscovery.FindGc(
            GameRoots(steam, historicAchievementsPath), manualGameRoot);
    }

    private static IEnumerable<string> GameRoots(SteamInstallReport steam,
        string? historicAchievementsPath)
    {
        var roots = new List<string>();
        var dedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? root)
        {
            if (!string.IsNullOrWhiteSpace(root) && dedup.Add(root))
                roots.Add(root);
        }

        // Prefer a recorded user-picked GC location if it follows the
        // approved csgo_gc/achievements.txt layout.
        Add(InstallationDiscovery.GameRootFromAchievementsPath(historicAchievementsPath));

        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (steam.Root is not null)
        {
            libraries.Add(steam.Root);
            foreach (var library in SteamLibraryRoots(steam.Root))
                libraries.Add(library);
        }
        foreach (var drive in ReadyDriveRoots())
        {
            libraries.Add(Path.Combine(drive, "SteamLibrary"));
            libraries.Add(Path.Combine(drive, "Steam"));
            libraries.Add(Path.Combine(drive, "Games", "Steam"));
            // Common user-created standalone game installations. Exact GC
            // sibling markers are still mandatory; no arbitrary games chosen.
            Add(Path.Combine(drive, "CSGO"));
            Add(Path.Combine(drive, "Games", "CSGO"));
            Add(Path.Combine(drive, "Games", "Counter-Strike Global Offensive"));
        }
        foreach (var library in libraries.Order(StringComparer.OrdinalIgnoreCase))
        {
            var common = Path.Combine(library, "steamapps", "common");
            if (!Directory.Exists(common)) continue;
            try
            {
                // One shallow level only, capped to limit pathological
                // installations; directories themselves are never opened
                // for writing or executed.
                foreach (var game in Directory.EnumerateDirectories(common)
                    .Order(StringComparer.OrdinalIgnoreCase).Take(1024))
                    if (Directory.Exists(Path.Combine(game, "csgo_gc")) ||
                        File.Exists(Path.Combine(game, "csgo.exe")))
                        Add(game);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { /* inaccessible Steam library; manual fallback */ }
        }
        return roots;
    }

    private static IEnumerable<string> SteamLibraryRoots(string steamRoot)
    {
        var path = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        string contents;
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length > 1_048_576) yield break;
            contents = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { yield break; }
        // Steam's VDF strings escape Windows backslashes. Match only known
        // library-folder keys; do not execute VDF, parse arbitrary JSON or
        // interpret any VDF entry as a path to an executable.
        foreach (Match line in Regex.Matches(contents,
            @"(?m)^\s*""(?:path|[0-9]+)""\s+""([^""]+)"""))
        {
            var value = line.Groups[1].Value.Replace(@"\\", @"\");
            if (Path.IsPathFullyQualified(value) && Directory.Exists(value))
                yield return value;
        }
    }

    private static IEnumerable<string> ReadyDriveRoots()
    {
        DriveInfo[] drives;
        try { drives = DriveInfo.GetDrives(); }
        catch (IOException) { yield break; }
        foreach (var drive in drives)
        {
            bool ready;
            try { ready = drive.IsReady && drive.DriveType is DriveType.Fixed or DriveType.Removable; }
            catch (IOException) { continue; }
            if (ready) yield return drive.RootDirectory.FullName;
        }
    }
}
