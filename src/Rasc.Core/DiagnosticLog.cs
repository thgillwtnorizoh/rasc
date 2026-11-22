using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Rasc.Core;

/// <summary>
/// Local-only, bounded diagnostic session. Intentionally does not collect
/// game files, Steam IDs, achievement names, filenames or window titles.
/// Crashes retain the last successful trace. Best-effort IO never throws.
/// </summary>
public sealed class DiagnosticLog : IDisposable
{
    public const int DefaultMaxFiles = 8;
    public const long DefaultMaxBytes = 2 * 1024 * 1024;

    private readonly object _sync = new();
    private readonly long _maxBytes;
    private readonly FileStream? _stream;
    private readonly StreamWriter? _writer;
    private bool _full;
    private bool _disposed;
    public string? FilePath { get; }

    public DiagnosticLog(string directory, long maxBytes = DefaultMaxBytes,
        int maxFiles = DefaultMaxFiles)
    {
        if (maxBytes < 1024 || maxFiles < 1) throw new ArgumentOutOfRangeException();
        _maxBytes = maxBytes;
        try
        {
            Directory.CreateDirectory(directory);
            // Include milliseconds and process ID. CreateNew prevents collisions
            // between overlapping RASC instances without shared file locks.
            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var candidate = Path.Combine(directory,
                    $"rasc-{stamp}-{Environment.ProcessId}-{attempt:D2}.log");
                try
                {
                    _stream = new FileStream(candidate, FileMode.CreateNew,
                        FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.None);
                    _writer = new StreamWriter(_stream, new UTF8Encoding(false)) { AutoFlush = true };
                    FilePath = candidate;
                    break;
                }
                catch (IOException) { /* unique filename retry */ }
            }
            if (FilePath is not null)
            {
                Info("session", $"started; pid={Environment.ProcessId} runtime={Environment.Version} " +
                    $"os={Environment.OSVersion.Version} is64bit={Environment.Is64BitProcess}");
                Prune(directory, maxFiles, FilePath);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (ArgumentException) { }
    }

    public void Info(string category, string message) => Write("INFO", category, message);
    public void Warning(string category, string message) => Write("WARN", category, message);
    public void Error(string category, string message) => Write("ERROR", category, message);
    public void Fatal(string category, Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Write("FATAL", category, error.ToString(), forceFlush: true);
    }

    private void Write(string level, string category, string message, bool forceFlush = false)
    {
        lock (_sync)
        {
            if (_disposed || _full || _writer is null || _stream is null) return;
            try
            {
                // A single line per event, even for exception stacks.
                // Never log unbounded attacker-controlled message contents.
                var safeCategory = OneLine(category, 64);
                var safeMessage = OneLine(message, 16000);
                var record = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) +
                    $" [{level}] [{safeCategory}] {safeMessage}";
                if (_stream.Length + Encoding.UTF8.GetByteCount(record) + 2 > _maxBytes)
                {
                    _writer.WriteLine(DateTimeOffset.UtcNow.ToString("O") +
                        " [WARN] [diagnostics] maximum log size reached; further events suppressed");
                    _writer.Flush();
                    _full = true;
                    return;
                }
                _writer.WriteLine(record);
                if (forceFlush) _stream.Flush(true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ObjectDisposedException) { }
        }
    }

    private static string OneLine(string? source, int max)
    {
        var text = source ?? "";
        if (text.Length > max) text = text[..max] + " [TRUNCATED]";
        return text.Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\0", "?", StringComparison.Ordinal);
    }

    private static void Prune(string directory, int maxFiles, string current)
    {
        try
        {
            var logs = Directory.GetFiles(directory, "rasc-*.log")
                .OrderByDescending(Path.GetFileName, StringComparer.Ordinal).ToArray();
            foreach (var old in logs.Skip(maxFiles))
            {
                if (!string.Equals(old, current, StringComparison.OrdinalIgnoreCase))
                    try { File.Delete(old); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            try { _writer?.Flush(); _stream?.Flush(true); }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            _writer?.Dispose();
            _stream?.Dispose();
        }
    }
}
