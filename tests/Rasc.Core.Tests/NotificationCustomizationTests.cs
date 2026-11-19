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
    [InlineData(ModernTypographyPreset.Baseline, 12, 600, 11.5)]
    [InlineData(ModernTypographyPreset.MotivaTitle12Regular, 12, 400, 11.5)]
    [InlineData(ModernTypographyPreset.MotivaTitle12Medium, 12, 500, 11.5)]
    [InlineData(ModernTypographyPreset.MotivaTitle11Medium, 11, 500, 11.5)]
    [InlineData(ModernTypographyPreset.MotivaTitle11MediumBody11, 11, 500, 11)]
    public void ModernMotivaVariantsAreBoundedAndReversible(
        ModernTypographyPreset preset, double titleSize, int weight, double bodySize)
    {
        var spec = ModernTypographyCalibration.Resolve(preset);
        Assert.Equal(titleSize, spec.TitleSize);
        Assert.Equal(weight, spec.TitleWeight);
        Assert.Equal(bodySize, spec.BodySize);
        Assert.Equal(400, spec.BodyWeight);
        var choice = new NotificationCustomizationSettings(ModernTypography: preset);
        Assert.Equal(preset, choice.Validated().ModernTypography);
    }

    [Fact]
    public void ModernCalibrationJsonIsBackwardCompatibleAndRejectsUnknownPresets()
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "rasc-modern-font-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "notification-customization.json");
            var store = new NotificationCustomizationStore(path);
            File.WriteAllText(path, """{"Font":1,"Sound":0,"CustomPath":""}""");
            Assert.Equal(ModernTypographyPreset.Baseline, store.Load().ModernTypography);
            Assert.True(store.TrySave(store.Load() with
                { ModernTypography = ModernTypographyPreset.MotivaTitle12Medium }));
            Assert.Equal(ModernTypographyPreset.MotivaTitle12Medium, store.Load().ModernTypography);
            File.WriteAllText(path, """{"ModernTypography":999}""");
            Assert.Equal(ModernTypographyPreset.Baseline, store.Load().ModernTypography);
        }
        finally { Directory.Delete(directory, true); }
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
