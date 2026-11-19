using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class NotificationCustomizationTests
{
    [Fact]
    public void DefaultsPreserveExistingAppearanceAndChooseSteamDesktopCue()
    {
        var defaults = new NotificationCustomizationSettings();
        Assert.Equal(PopupFontChoice.Original, defaults.Font);
        Assert.Equal(NotificationSoundChoice.DesktopDefault, defaults.Sound);
        Assert.Equal(ModernTypographyProfile.RascAccepted, defaults.ModernTypography);
        Assert.Empty(defaults.CustomPath);
        Assert.Equal("desktop_toast_default.wav",
            NotificationSoundFiles.ExpectedFile(defaults.Sound));
    }

    [Theory]
    [InlineData(PopupFontChoice.Original)]
    [InlineData(PopupFontChoice.MotivaSans)]
    [InlineData(PopupFontChoice.Arial)]
    [InlineData(PopupFontChoice.Helvetica)]
    [InlineData(PopupFontChoice.SansSerif)]
    public void EachRequestedFontChoiceIsPersistable(PopupFontChoice font)
    {
        Assert.Equal(font, new NotificationCustomizationSettings(Font: font)
            .Validated().Font);
    }

    [Theory]
    [InlineData(NotificationSoundChoice.DesktopDefault)]
    [InlineData(NotificationSoundChoice.BigPicture)]
    [InlineData(NotificationSoundChoice.Custom)]
    [InlineData(NotificationSoundChoice.Off)]
    public void SoundChoicesAreIndependentOfFontAndProfile(NotificationSoundChoice sound)
    {
        var chosen = new NotificationCustomizationSettings(
            PopupFontChoice.Arial, sound).Validated();
        Assert.Equal(PopupFontChoice.Arial, chosen.Font);
        Assert.Equal(sound, chosen.Sound);
    }

    [Theory]
    [InlineData(ModernTypographyProfile.RascAccepted)]
    [InlineData(ModernTypographyProfile.SteamCssDesktop)]
    [InlineData(ModernTypographyProfile.SteamCssBasicUi)]
    public void ModernTextProfilesSurviveValidationIndependentlyOfFont(ModernTypographyProfile profile)
    {
        var settings = new NotificationCustomizationSettings(
            Font: PopupFontChoice.MotivaSans, ModernTypography: profile).Validated();
        Assert.Equal(PopupFontChoice.MotivaSans, settings.Font);
        Assert.Equal(profile, settings.ModernTypography);
    }

    [Fact]
    public void OwnerObservedMixedFontKeepsAcceptedMetricsExceptBoldTitle()
    {
        var spec = ModernTypographySpec.For(ModernTypographyProfile.OwnerObservedMixed);
        Assert.Equal(12, spec.NameSize);
        Assert.Equal(700, spec.NameWeight);
        Assert.Equal("#D3D8DF", spec.NameColor);
        Assert.Equal(11.5, spec.DescriptionSize);
        Assert.Equal(400, spec.DescriptionWeight);
        Assert.Equal("#8C96A4", spec.DescriptionColor);
        Assert.True(double.IsNaN(spec.LineHeight));
        Assert.Equal(ModernTypographyProfile.OwnerObservedMixed,
            new NotificationCustomizationSettings(
                ModernTypography: ModernTypographyProfile.OwnerObservedMixed)
                .Validated().ModernTypography);
    }

    [Fact]
    public void MixedFontProfilePersistsAndOldSettingsRemainBaseline()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "rasc-owner-mixed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "notification-customization.json");
            var store = new NotificationCustomizationStore(path);
            File.WriteAllText(path, """{"Font":1,"Sound":0}""");
            Assert.Equal(ModernTypographyProfile.RascAccepted, store.Load().ModernTypography);
            var next = store.Load() with
            {
                ModernTypography = ModernTypographyProfile.OwnerObservedMixed
            };
            Assert.True(store.TrySave(next));
            Assert.Equal(next, store.Load());
            Assert.Equal(PopupFontChoice.MotivaSans, store.Load().Font);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void SteamAchievementCssFontMetricsAreSourceDeclaredNotPixelFitted()
    {
        var standard = ModernTypographySpec.For(ModernTypographyProfile.SteamCssDesktop);
        Assert.Equal(12, standard.NameSize);
        Assert.Equal(400, standard.NameWeight);
        Assert.Equal("#FFFFFF", standard.NameColor);
        Assert.Equal(12, standard.DescriptionSize);
        Assert.Equal(300, standard.DescriptionWeight);
        Assert.Equal("#8B929A", standard.DescriptionColor);
        Assert.Equal(16, standard.LineHeight);

        var basic = ModernTypographySpec.For(ModernTypographyProfile.SteamCssBasicUi);
        Assert.Equal(12, basic.NameSize);
        Assert.Equal(500, basic.NameWeight);
        Assert.Equal("#FFFFFF", basic.NameColor);
        Assert.Equal(12, basic.DescriptionSize);
        Assert.Equal(400, basic.DescriptionWeight);
        Assert.Equal("#B8BCBF", basic.DescriptionColor);
        Assert.Equal(16, basic.LineHeight);
    }

    [Fact]
    public void ApprovedTypographyBaselineIsUntouchedAndInvalidModeResets()
    {
        var original = ModernTypographySpec.For(ModernTypographyProfile.RascAccepted);
        Assert.Equal(12, original.NameSize);
        Assert.Equal(600, original.NameWeight);
        Assert.Equal("#D3D8DF", original.NameColor);
        Assert.Equal(11.5, original.DescriptionSize);
        Assert.Equal(400, original.DescriptionWeight);
        Assert.Equal("#8C96A4", original.DescriptionColor);
        Assert.True(double.IsNaN(original.LineHeight));
        Assert.Equal(ModernTypographyProfile.RascAccepted,
            new NotificationCustomizationSettings(ModernTypography:
                (ModernTypographyProfile)999).Validated().ModernTypography);
        Assert.Equal(original, ModernTypographySpec.For((ModernTypographyProfile)999));
    }

    [Fact]
    public void LegacySoundAndFontSettingsRemainCompatibleWithNewTypographyChoice()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "rasc-typography-compat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "notification-customization.json");
            var store = new NotificationCustomizationStore(path);
            File.WriteAllText(path, """{"Font":1,"Sound":3,"CustomPath":""}""");
            var restored = store.Load();
            Assert.Equal(PopupFontChoice.MotivaSans, restored.Font);
            Assert.Equal(NotificationSoundChoice.Off, restored.Sound);
            Assert.Equal(ModernTypographyProfile.RascAccepted, restored.ModernTypography);
            var selected = restored with { ModernTypography = ModernTypographyProfile.SteamCssDesktop };
            Assert.True(store.TrySave(selected));
            Assert.Equal(selected, new NotificationCustomizationStore(path).Load());
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void ReadsSelectedSteamWavWithoutCopyingOrAssumingHistoricalSha()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "rasc-steam-wav-" + Guid.NewGuid().ToString("N"));
        try
        {
            var steam = Path.Combine(root, "Steam");
            var sounds = Path.Combine(steam, "steamui", "sounds");
            Directory.CreateDirectory(sounds);
            var original = Path.Combine(sounds, NotificationSoundFiles.DesktopFile);
            var bytes = new byte[44];
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(bytes, 8);
            File.WriteAllBytes(original, bytes);
            Assert.True(NotificationSoundFiles.IsValidSteamWav(original));
            Assert.False(NotificationSoundFiles.IsApprovedPresetFile(
                original, NotificationSoundChoice.DesktopDefault));
            var config = new NotificationCustomizationSettings(
                Sound: NotificationSoundChoice.DesktopDefault);
            var found = NotificationSoundFiles.Resolve(
                config, Path.Combine(root, "local"), Path.Combine(root, "app"), steam);
            Assert.Equal(original, found);
            Assert.False(Directory.Exists(Path.Combine(root, "local")));
            bytes[8] = (byte)'X';
            File.WriteAllBytes(original, bytes);
            Assert.False(NotificationSoundFiles.IsValidSteamWav(original));
            Assert.Null(NotificationSoundFiles.Resolve(
                config, Path.Combine(root, "local"), Path.Combine(root, "app"), steam));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void SoundReferencesMatchTwoOwnerUploadedWavHashes()
    {
        Assert.Equal(
            "c03fc4796f78001b8d9e279c053d859b6e68ca97bdcb4bc657e45af3d17a3ef7",
            NotificationSoundFiles.DesktopSha256);
        Assert.Equal(
            "f96a1ab8ff7668b3663a465749e517be98d54515b2dd314ccc291fbf3645c22a",
            NotificationSoundFiles.BigPictureSha256);
        Assert.Equal("deck_ui_achievement_toast.wav",
            NotificationSoundFiles.ExpectedFile(NotificationSoundChoice.BigPicture));
        Assert.Null(NotificationSoundFiles.ExpectedFile(NotificationSoundChoice.Custom));
    }

    [Fact]
    public void BadEnumValuesAndRelativePathsFailClosed()
    {
        var settings = new NotificationCustomizationSettings(
            (PopupFontChoice)999, (NotificationSoundChoice)999, "relative.wav").Validated();
        Assert.Equal(PopupFontChoice.Original, settings.Font);
        Assert.Equal(NotificationSoundChoice.DesktopDefault, settings.Sound);
        Assert.Empty(settings.CustomPath);
        Assert.False(NotificationSoundFiles.IsSupportedCustomPath("relative.wav"));
        Assert.False(NotificationSoundFiles.IsSupportedCustomPath(
            Path.Combine(Path.GetTempPath(), "not-supported.exe")));
    }

    [Fact]
    public void SeparatePreferencesPersistAndNeverWriteSourceOrHistoryFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "rasc-customization-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new NotificationCustomizationStore(
                Path.Combine(directory, "notification-customization.json"));
            var custom = Path.Combine(directory, "my sound.wav");
            var value = new NotificationCustomizationSettings(
                PopupFontChoice.MotivaSans, NotificationSoundChoice.Custom, custom);
            Assert.True(store.TrySave(value));
            Assert.Equal(value, new NotificationCustomizationStore(
                Path.Combine(directory, "notification-customization.json")).Load());
            Assert.False(File.Exists(Path.Combine(directory, "history.db")));
            Assert.False(File.Exists(Path.Combine(directory, "achievements.txt")));
            Assert.False(File.Exists(Path.Combine(directory, "appearance.json")));
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void CorruptOversizedAndUnknownSoundSelectionsAreSafelyRejected()
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "rasc-custom-bad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var file = Path.Combine(directory, "notification-customization.json");
            var store = new NotificationCustomizationStore(file);
            File.WriteAllText(file, "{bad JSON");
            Assert.Equal(new NotificationCustomizationSettings(), store.Load());
            File.WriteAllText(file, """{"Font":999,"Sound":999,"CustomPath":"bad.mp3"}""");
            Assert.Equal(new NotificationCustomizationSettings(), store.Load());
            File.WriteAllText(file, new string('a', 16_385));
            Assert.Equal(new NotificationCustomizationSettings(), store.Load());
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void InvalidUserWavCannotMasqueradeAsEitherSteamPreset()
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "rasc-custom-audio-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var original = Path.Combine(directory, "desktop_toast_default.wav");
            // File name and length alone cannot authorize a "Steam" preset.
            File.WriteAllBytes(original, new byte[100]);
            Assert.False(NotificationSoundFiles.IsApprovedPresetFile(
                original, NotificationSoundChoice.DesktopDefault));
            Assert.False(NotificationSoundFiles.IsApprovedPresetFile(
                original, NotificationSoundChoice.BigPicture));
            Assert.False(NotificationSoundFiles.TryImportPreset(
                original, directory, NotificationSoundChoice.DesktopDefault, out _));
            Assert.False(File.Exists(Path.Combine(directory, "sounds",
                NotificationSoundFiles.DesktopFile)));
            Assert.Null(NotificationSoundFiles.Resolve(new(), directory, directory));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void LocalCustomAudioResolvesOnlyWhenFileIsSupportedAndBounded()
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "rasc-custom-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "effect.wav");
            File.WriteAllBytes(path, [0, 1, 2, 3]);
            var selected = new NotificationCustomizationSettings(
                Sound: NotificationSoundChoice.Custom, CustomPath: path);
            Assert.Equal(path, NotificationSoundFiles.Resolve(selected, directory, directory));
            File.Delete(path);
            Assert.Null(NotificationSoundFiles.Resolve(selected, directory, directory));
            Assert.Null(NotificationSoundFiles.Resolve(
                selected with { Sound = NotificationSoundChoice.Off }, directory, directory));
        }
        finally { Directory.Delete(directory, true); }
    }
}
