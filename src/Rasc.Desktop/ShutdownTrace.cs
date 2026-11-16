using System.Diagnostics;
using System.IO;

namespace Rasc.Desktop;

/// <summary>
/// Best-effort *local-only* timing trace for the reported slow-exit discrepancy.
/// Includes durations and lifecycle phases, never source paths, achievement IDs,
/// user identifiers or exception messages. Persisted once at Application.Exit.
/// </summary>
internal static class ShutdownTrace
{
    private static readonly Stopwatch Clock = new();
    private static readonly List<string> Entries = [];
    private static bool _begun;
    private static readonly object Sync = new();

    internal static void Begin()
    {
        lock (Sync)
        {
            if (_begun) return;
            _begun = true;
            Clock.Restart();
            Entries.Clear();
        }
    }

    internal static void Mark(string phase)
    {
        lock (Sync)
        {
            if (!_begun) return;
            Entries.Add($"{Clock.Elapsed.TotalMilliseconds:F1}ms: {phase}");
        }
    }

    internal static void Flush()
    {
        string[] lines;
        lock (Sync)
        {
            if (!_begun) return;
            lines = Entries.ToArray();
            _begun = false;
        }
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RASC");
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "last-shutdown.txt"), lines);
        }
        catch (IOException) { /* diagnostic is strictly best-effort */ }
        catch (UnauthorizedAccessException) { /* diagnostic is strictly best-effort */ }
    }
}
