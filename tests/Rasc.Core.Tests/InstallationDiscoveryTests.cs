using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class InstallationDiscoveryTests
{
    private sealed class Sandbox : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(),
            "rasc-discovery-" + Guid.NewGuid().ToString("N"));
        public string NewSteam(string name, bool exe = true, bool ui = true,
            bool desktop = true, bool deck = true)
        {
            var root = Path.Combine(Root, name);
            Directory.CreateDirectory(root);
            if (exe) File.WriteAllBytes(Path.Combine(root, "steam.exe"), [1]);
            if (ui)
            {
                Directory.CreateDirectory(Path.Combine(root, "steamui", "sounds"));
                if (desktop) File.WriteAllBytes(Path.Combine(root,
                    "steamui", "sounds", "desktop_toast_default.wav"), [1]);
                if (deck) File.WriteAllBytes(Path.Combine(root,
                    "steamui", "sounds", "deck_ui_achievement_toast.wav"), [1]);
            }
            return root;
        }
        public string NewGame(string name, bool gc = true, bool exe = true, bool file = true)
        {
            var root = Path.Combine(Root, name);
            Directory.CreateDirectory(root);
            if (exe) File.WriteAllBytes(Path.Combine(root, "csgo.exe"), [1]);
            if (gc)
            {
                Directory.CreateDirectory(Path.Combine(root, "csgo_gc"));
                if (file) File.WriteAllText(Path.Combine(root, "csgo_gc", "achievements.txt"), "fixture");
            }
            return root;
        }
        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }

    [Fact]
    public void SteamPrefersProgramFilesX86ThenProgramFilesThenOtherRegardlessOfEnumerationOrder()
    {
        using var t = new Sandbox();
        var x86 = t.NewSteam(Path.Combine("x86", "Steam"));
        var x64 = t.NewSteam(Path.Combine("x64", "Steam"));
        var elsewhere = t.NewSteam("elsewhere");
        var found = InstallationDiscovery.FindSteam(
            Path.Combine(t.Root, "x86"), Path.Combine(t.Root, "x64"), [elsewhere, x64]);
        Assert.Equal(x86, found.Root);
        Assert.True(found.Ready);
        Assert.False(found.Manual);
        Assert.Equal(x64, InstallationDiscovery.FindSteam(
            Path.Combine(t.Root, "not-installed"), Path.Combine(t.Root, "x64"),
            [elsewhere]).Root);
        Assert.Equal(elsewhere, InstallationDiscovery.FindSteam(
            null, null, [elsewhere]).Root);
    }

    [Fact]
    public void MissingFilesInSelectedSteamDoNotSilentlyFallbackToAnotherInstall()
    {
        using var t = new Sandbox();
        var preferred = t.NewSteam(Path.Combine("x86", "Steam"), desktop: false);
        var other = t.NewSteam("other");
        var found = InstallationDiscovery.FindSteam(
            Path.Combine(t.Root, "x86"), null, [other]);
        Assert.Equal(preferred, found.Root);
        Assert.False(found.Ready);
        Assert.Contains(InstallationDiscovery.SteamDesktopWav, found.Missing);
        Assert.DoesNotContain(InstallationDiscovery.SteamDeckWav, found.Missing);
        Assert.Equal(other, InstallationDiscovery.FindSteam(null, null,
            [preferred], manualRoot: other).Root);
    }

    [Fact]
    public void SteamHalfInstalledAndAbsentStatesReportExactMissingResources()
    {
        using var t = new Sandbox();
        var onlyUi = t.NewSteam("half", exe: false, desktop: false, deck: false);
        var found = InstallationDiscovery.FindSteam(null, null, [onlyUi]);
        Assert.Equal(onlyUi, found.Root);
        Assert.Equal(3, found.Missing.Count);
        Assert.Contains("steam.exe", found.Missing);
        Assert.False(InstallationDiscovery.FindSteam(null, null, [t.Root]).Found);
        var forced = InstallationDiscovery.FindSteam(null, null, [],
            manualRoot: Path.Combine(t.Root, "manual"));
        Assert.True(forced.Manual);
        Assert.Equal(4, forced.Missing.Count);
    }

    [Fact]
    public void GcRequiresSiblingLauncherAndFileUnderCsgoGc()
    {
        using var t = new Sandbox();
        var invalid = t.NewGame("invalid", exe: false);
        var missing = t.NewGame("missing", file: false);
        var ready = t.NewGame("valid");
        Assert.Equal(GcInstallState.MissingLauncher,
            InstallationDiscovery.ExamineGc(invalid).State);
        Assert.Equal(GcInstallState.MissingAchievements,
            InstallationDiscovery.ExamineGc(missing).State);
        var found = InstallationDiscovery.FindGc([invalid, missing, ready]);
        Assert.Equal(ready, found.GameRoot);
        Assert.Equal(Path.Combine(ready, "csgo_gc", "achievements.txt"),
            found.AchievementsPath);
        Assert.Equal(missing, InstallationDiscovery.FindGc([invalid, missing]).GameRoot);
        Assert.Equal(GcInstallState.NoInstallation,
            InstallationDiscovery.FindGc([Path.Combine(t.Root, "none")]).State);
    }

    [Fact]
    public void ManualInstallationPreferencesPersistWithNoHistoryModification()
    {
        using var t = new Sandbox();
        Directory.CreateDirectory(t.Root);
        var file = Path.Combine(t.Root, "installation-locations.json");
        var store = new InstallationLocationStore(file);
        Assert.Equal(new InstallationLocations(), store.Load());
        var chosen = new InstallationLocations(
            Path.Combine(t.Root, "steam-manual"),
            Path.Combine(t.Root, "csgo-manual"));
        Assert.True(store.TrySave(chosen));
        Assert.Equal(chosen, new InstallationLocationStore(file).Load());
        Assert.False(File.Exists(Path.Combine(t.Root, "history.db")));
        Assert.False(File.Exists(Path.Combine(t.Root, "achievements.txt")));
        File.WriteAllText(file, "{ corrupt");
        Assert.Equal(new InstallationLocations(), store.Load());
    }

    [Fact]
    public void AbsentLauncherOrGcFolderCannotBeUsedAsMonitorSource()
    {
        using var t = new Sandbox();
        var noGc = t.NewGame("game-no-gc", gc: false);
        var noLauncher = t.NewGame("game-no-exe", exe: false);
        Assert.Null(InstallationDiscovery.ExamineGc(noGc).AchievementsPath);
        Assert.Null(InstallationDiscovery.ExamineGc(noLauncher).AchievementsPath);
        Assert.Equal(GcInstallState.MissingLauncher,
            InstallationDiscovery.ExamineGc(noGc).State);
        Assert.Equal(GcInstallState.MissingLauncher,
            InstallationDiscovery.ExamineGc(noLauncher).State);
    }

    [Fact]
    public void ManualGcNeverSilentlyUsesOtherGameAndValidatesAchievementsPath()
    {
        using var t = new Sandbox();
        var missing = t.NewGame("missing", file: false);
        var ready = t.NewGame("ready");
        Assert.Equal(GcInstallState.MissingAchievements,
            InstallationDiscovery.FindGc([ready], missing).State);
        var txt = Path.Combine(ready, "csgo_gc", "achievements.txt");
        Assert.Equal(ready, InstallationDiscovery.GameRootFromAchievementsPath(txt));
        Assert.Null(InstallationDiscovery.GameRootFromAchievementsPath(
            Path.Combine(ready, "achievements.txt")));
        Assert.Null(InstallationDiscovery.GameRootFromAchievementsPath(
            Path.Combine(ready, "csgo_gc", "other.txt")));
    }
}
