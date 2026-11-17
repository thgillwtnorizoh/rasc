namespace Rasc.Core;

/// <summary>Presentation selection only; never modifies GC or Steam state.</summary>
public enum NotificationProfile { Off, Classic, Modern }

/// <summary>
/// Independent from the accepted Classic appearance.json schema. The simple
/// text format intentionally rejects unknown values and oversized files.
/// </summary>
public sealed class NotificationProfileStore(string path)
{
    public NotificationProfile Load()
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length > 128) return NotificationProfile.Classic;
            var raw = File.ReadAllText(path).Trim();
            return Enum.TryParse<NotificationProfile>(raw, ignoreCase: false,
                out var profile) && Enum.IsDefined(profile)
                ? profile : NotificationProfile.Classic;
        }
        catch (IOException) { return NotificationProfile.Classic; }
        catch (UnauthorizedAccessException) { return NotificationProfile.Classic; }
    }

    public bool TrySave(NotificationProfile profile)
    {
        if (!Enum.IsDefined(profile)) return false;
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temp, profile.ToString());
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
