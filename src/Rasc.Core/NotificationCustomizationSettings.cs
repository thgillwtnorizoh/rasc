using System.Security.Cryptography;
using System.Text.Json;

namespace Rasc.Core;

/// <summary>
/// Presentation-only choices. Original preserves each owner's accepted font:
/// Classic Helvetica/Arial fallback, Modern WPF's existing inherited UI font.
/// No font files are redistributed with RASC.
/// </summary>
public enum PopupFontChoice { Original, MotivaSans, Arial, Helvetica, SansSerif }
public enum NotificationSoundChoice { DesktopDefault, BigPicture, Custom, Off }

// A separate debug dimension from font family. Existing owner-accepted
// Modern card typography remains default and must survive upgrades.
public enum ModernTypographyProfile { RascAccepted, SteamCssDesktop, SteamCssBasicUi, OwnerObservedMixed }

public sealed record NotificationCustomizationSettings(
    PopupFontChoice Font = PopupFontChoice.Original,
    NotificationSoundChoice Sound = NotificationSoundChoice.DesktopDefault,
    string CustomPath = "",
    ModernTypographyProfile ModernTypography = ModernTypographyProfile.RascAccepted)
{
    public NotificationCustomizationSettings Validated()
    {
        var font = Enum.IsDefined(Font) ? Font : PopupFontChoice.Original;
        var sound = Enum.IsDefined(Sound) ? Sound : NotificationSoundChoice.DesktopDefault;
        var typography = Enum.IsDefined(ModernTypography)
            ? ModernTypography : ModernTypographyProfile.RascAccepted;
        var path = CustomPath ?? "";
        if (!NotificationSoundFiles.IsSupportedCustomPath(path)) path = "";
        return new(font, sound, path, typography);
    }
}

/// <summary>
/// Separate JSON file so old appearance.json (FPS and scaling) is unchanged.
/// Settings never touch Steam/GC/source files or the unlock ledger.
/// </summary>
public sealed class NotificationCustomizationStore(string path)
{
    public NotificationCustomizationSettings Load()
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length > 16_384) return new();
            return (JsonSerializer.Deserialize<NotificationCustomizationSettings>(
                File.ReadAllText(path)) ?? new()).Validated();
        }
        catch (IOException) { return new(); }
        catch (UnauthorizedAccessException) { return new(); }
        catch (JsonException) { return new(); }
    }

    public bool TrySave(NotificationCustomizationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
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
/// Two precisely identified user-provided Steam notification PCM WAV fixtures.
/// Files are imported locally by the owner; NO Valve audio is redistributed by
/// the public RASC repository/build. Audio is only read in the application.
/// </summary>
public static class NotificationSoundFiles
{
    public const string DesktopFile = "desktop_toast_default.wav";
    public const string BigPictureFile = "deck_ui_achievement_toast.wav";
    public const string DesktopSha256 = "c03fc4796f78001b8d9e279c053d859b6e68ca97bdcb4bc657e45af3d17a3ef7";
    public const string BigPictureSha256 = "f96a1ab8ff7668b3663a465749e517be98d54515b2dd314ccc291fbf3645c22a";
    public const long MaxCustomBytes = 16 * 1024 * 1024;

    public static string? ExpectedFile(NotificationSoundChoice choice) => choice switch
    {
        NotificationSoundChoice.DesktopDefault => DesktopFile,
        NotificationSoundChoice.BigPicture => BigPictureFile,
        _ => null
    };

    private static string? ExpectedHash(NotificationSoundChoice choice) => choice switch
    {
        NotificationSoundChoice.DesktopDefault => DesktopSha256,
        NotificationSoundChoice.BigPicture => BigPictureSha256,
        _ => null
    };

    public static bool IsSupportedCustomPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 2048) return false;
        if (!Path.IsPathFullyQualified(path)) return false;
        return Path.GetExtension(path).ToLowerInvariant() is ".wav" or ".mp3" or ".wma";
    }

    public static bool IsValidCustomFile(string? path)
    {
        if (!IsSupportedCustomPath(path)) return false;
        try
        {
            var file = new FileInfo(path!);
            return file.Exists && file.Length is > 0 and <= MaxCustomBytes;
        }
        catch (ArgumentException) { return false; }
        catch (NotSupportedException) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static bool IsApprovedPresetFile(string path, NotificationSoundChoice choice)
    {
        var expected = ExpectedHash(choice);
        if (expected is null) return false;
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length is < 44 or > 2_000_000 ||
                !file.Extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                return false;
            using var stream = file.OpenRead();
            var hash = Convert.ToHexString(SHA256.HashData(stream));
            return string.Equals(hash, expected, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException) { return false; }
        catch (NotSupportedException) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    /// <summary>
    /// Local owner import of the two provided WAVs. A wrong WAV is rejected,
    /// even if it has the same filename. Never modifies Steam's originals.
    /// </summary>
    public static bool TryImportPreset(string sourcePath, string localRascDirectory,
        NotificationSoundChoice choice, out string message)
    {
        var filename = ExpectedFile(choice);
        if (filename is null || !IsApprovedPresetFile(sourcePath, choice))
        {
            message = "This WAV is not the supplied preset sound (SHA-256 mismatch).";
            return false;
        }
        var output = Path.Combine(localRascDirectory, "sounds", filename);
        var temporary = output + "." + Guid.NewGuid().ToString("N") + ".wav"; // Must retain WAV extension for the verified-format gate.
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.Copy(sourcePath, temporary);
            if (!IsApprovedPresetFile(temporary, choice))
            {
                message = "Imported sound changed during copying.";
                return false;
            }
            File.Move(temporary, output, overwrite: true);
            message = "Sound imported into RASC private application data.";
            return true;
        }
        catch (IOException ex) { message = "Sound import failed: " + ex.Message; return false; }
        catch (UnauthorizedAccessException) { message = "Sound import: access denied."; return false; }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>
    /// Supports either local owner-imported sounds or the optional offline
    /// sound pack placed beside Rasc.Desktop.exe. Do not auto-copy from Steam.
    /// </summary>
    public static string? Resolve(NotificationCustomizationSettings settings,
        string localRascDirectory, string applicationBaseDirectory)
    {
        settings = settings.Validated();
        if (settings.Sound == NotificationSoundChoice.Off) return null;
        if (settings.Sound == NotificationSoundChoice.Custom)
            return IsValidCustomFile(settings.CustomPath) ? settings.CustomPath : null;

        var name = ExpectedFile(settings.Sound);
        if (name is null) return null;
        foreach (var path in new[]
        {
            Path.Combine(localRascDirectory, "sounds", name),
            Path.Combine(applicationBaseDirectory, "Assets", "NotificationSounds", name)
        })
            if (IsApprovedPresetFile(path, settings.Sound)) return path;
        return null;
    }
}
