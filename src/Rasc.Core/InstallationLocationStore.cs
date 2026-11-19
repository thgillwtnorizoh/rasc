using System.Text.Json;

namespace Rasc.Core;

/// <summary>
/// Persist only explicit manual override choices. Auto-detected roots are
/// intentionally not sticky, so changing Steam installs is detected at launch.
/// Existing history/last-source data is never modified by this store.
/// </summary>
public sealed record InstallationLocations(
    string SteamRootOverride = "", string GcGameRootOverride = "");

public sealed class InstallationLocationStore(string path)
{
    public InstallationLocations Load()
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length > 16_384) return new();
            return JsonSerializer.Deserialize<InstallationLocations>(
                File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return new(); }
    }

    public bool TrySave(InstallationLocations locations)
    {
        ArgumentNullException.ThrowIfNull(locations);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temp, JsonSerializer.Serialize(locations));
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return false; }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
