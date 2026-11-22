using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class DiagnosticLogTests
{
    [Fact]
    public void EmitsSessionAndCriticalEventsAsSingleLinesAndRetainsFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rasc-diag-" + Guid.NewGuid().ToString("N"));
        try
        {
            string? path;
            using (var logger = new DiagnosticLog(dir))
            {
                path = logger.FilePath;
                Assert.NotNull(path);
                logger.Info("classic", "stage=enter count=1");
                logger.Warning("renderer", "callback stall\nrecovered");
                logger.Fatal("unhandled.ui", new InvalidOperationException("synthetic test crash"));
                // File.ReadAllLines uses FileShare.Read on Windows, which
                // denies an active writer. A live diagnostics viewer must
                // request FileShare.ReadWrite to inspect the running log.
                using var liveFile = new FileStream(path, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(liveFile);
                var lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Assert.Contains(lines, l => l.Contains("[INFO] [classic] stage=enter count=1"));
                Assert.Contains(lines, l => l.Contains("[WARN] [renderer] callback stall\\nrecovered"));
                Assert.Contains(lines, l => l.Contains("[FATAL] [unhandled.ui]") &&
                    l.Contains("InvalidOperationException") &&
                    l.Contains("synthetic test crash"));
                Assert.Contains(lines, l => l.Contains("[session] started;"));
                Assert.Equal(4, lines.Length);
            }
            Assert.True(File.Exists(path));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void BoundsEachSessionAndPrunesOldRuns()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rasc-diag-" + Guid.NewGuid().ToString("N"));
        try
        {
            // No synchronization sleeps: unique process-attempt suffixes
            // distinguish sessions created in the same millisecond.
            for (var i = 0; i < 6; i++)
            {
                using var logger = new DiagnosticLog(dir, maxBytes: 1024, maxFiles: 3);
                Assert.NotNull(logger.FilePath);
                for (var j = 0; j < 100; j++)
                    logger.Info("classic", new string('x', 100));
                Assert.True(new FileInfo(logger.FilePath).Length <= 1200);
            }
            Assert.InRange(Directory.GetFiles(dir, "rasc-*.log").Length, 1, 3);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void InvalidOrUnwritableDirectoryCannotCrashHost()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rasc-diag-block-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(dir, "block a directory");
        try
        {
            using var logger = new DiagnosticLog(Path.Combine(dir, "logs"));
            Assert.Null(logger.FilePath);
            logger.Info("test", "nothing should throw");
            logger.Fatal("test", new IOException("not writable"));
        }
        finally { File.Delete(dir); }
    }
}
