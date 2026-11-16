using System.Text;

namespace Rasc.Core;

/// <summary>Only opens the GC's finished snapshot for reading; never opens with write access.</summary>
public sealed class GcFileSource
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    public const int MaxBytes = 2_097_152;
    public string FilePath { get; }
    public string SourceKey { get; }

    public GcFileSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Select an achievements.txt path.", nameof(path));
        FilePath = Path.GetFullPath(path);
        SourceKey = FilePath.ToUpperInvariant();
    }

    public async Task<FileReadResult> ReadAsync(CancellationToken ct = default)
    {
        try
        {
            await using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 16_384,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length > MaxBytes)
                return FileReadResult.Failure(ReadStatus.Invalid, "File exceeds the 2 MiB input limit.");
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            if (memory.Length > MaxBytes)
                return FileReadResult.Failure(ReadStatus.Invalid, "File changed to exceed the 2 MiB input limit.");
            var text = StrictUtf8.GetString(memory.ToArray());
            return FileReadResult.Valid(GcKeyValuesParser.Parse(text));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (FileNotFoundException) { return FileReadResult.Failure(ReadStatus.Missing, "Waiting for achievements.txt."); }
        catch (DirectoryNotFoundException) { return FileReadResult.Failure(ReadStatus.Missing, "Waiting for the source directory."); }
        catch (FormatException ex) { return FileReadResult.Failure(ReadStatus.Invalid, ex.Message); }
        catch (DecoderFallbackException ex) { return FileReadResult.Failure(ReadStatus.Invalid, ex.Message); }
        catch (IOException ex) { return FileReadResult.Failure(ReadStatus.Unavailable, ex.Message); }
        catch (UnauthorizedAccessException ex) { return FileReadResult.Failure(ReadStatus.Unavailable, ex.Message); }
    }
}
