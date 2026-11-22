using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class GcAchievementJournalTests
{
    private static string Event(string session, int seq, string type,
        string extra = "") =>
        $$"""{"v":1,"session":"{{session}}","seq":{{seq}},"time_ms":1795260000125,"steady_us":42,"type":"{{type}}"{{extra}}}""" + "\n";

    [Fact]
    public async Task MissingJournalAllowsFallbackButEmptyJournalIsAuthoritative()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rasc-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var source = new GcFileSource(Path.Combine(dir, "achievements.txt"));
            Assert.Equal(ReadStatus.Missing, (await source.ReadJournalAsync()).Status);
            await File.WriteAllTextAsync(source.JournalPath, "");
            var read = await source.ReadJournalAsync();
            Assert.Equal(ReadStatus.Valid, read.Status);
            Assert.Empty(read.Unlocks);
            await File.WriteAllTextAsync(source.JournalPath, "{\"v\":1");
            read = await source.ReadJournalAsync();
            Assert.Equal(ReadStatus.Valid, read.Status);
            Assert.Empty(read.Unlocks); // partial line is never an award
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task StrictJsonlEmitsOnlyNativeUnlocksAndRejectsFabricatedIds()
    {
        var path = Path.Combine(Path.GetTempPath(),
            "rasc-journal-" + Guid.NewGuid().ToString("N"));
        try
        {
            await File.WriteAllTextAsync(path,
                Event("s", 1, "progress_call", ",\"id\":\"A\",\"current\":25,\"maximum\":100") +
                Event("s", 2, "unlock", ",\"id\":\"A\",\"unlock_seconds\":1795260000") +
                Event("s", 3, "stats_store", ",\"success\":true,\"newly_stored\":1"));
            var read = await GcAchievementJournal.ReadAsync(path);
            Assert.Equal(ReadStatus.Valid, read.Status);
            Assert.Empty(read.MatchStarts); // old compatible journal has no marker
            var award = Assert.Single(read.Unlocks);
            Assert.Equal("A", award.AchievementId);
            Assert.Equal(2UL, award.Sequence);
            Assert.Equal(1795260000U, award.UnlockUnixSeconds);

            await File.AppendAllTextAsync(path,
                Event("s", 2, "unlock", ",\"id\":\"DIFFERENT\",\"unlock_seconds\":42"));
            Assert.Equal(ReadStatus.Invalid, (await GcAchievementJournal.ReadAsync(path)).Status);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task TruncatedLogNeverResurrectsOldMatchSignals()
    {
        var path = Path.Combine(Path.GetTempPath(),
            "rasc-journal-" + Guid.NewGuid().ToString("N"));
        try
        {
            await File.WriteAllTextAsync(path,
                Event("session1", 1, "unlock", ",\"id\":\"OLD\",\"unlock_seconds\":1"));
            Assert.Single((await GcAchievementJournal.ReadAsync(path)).Unlocks);
            await File.WriteAllTextAsync(path, "");
            Assert.Empty((await GcAchievementJournal.ReadAsync(path)).Unlocks);
            await File.WriteAllTextAsync(path,
                Event("session1", 2, "unlock", ",\"id\":\"NEW\",\"unlock_seconds\":2"));
            Assert.Equal("NEW", Assert.Single((await GcAchievementJournal.ReadAsync(path)).Unlocks).AchievementId);
        }
        finally { File.Delete(path); }
    }
    [Fact]
    public async Task ExplicitMatchStartIsNotAnAwardAndCarriesBaselineReason()
    {
        var path = Path.Combine(Path.GetTempPath(),
            "rasc-join-" + Guid.NewGuid().ToString("N"));
        try
        {
            await File.WriteAllTextAsync(path,
                Event("s", 1, "match_start", ",\"reason\":\"join\"") +
                Event("s", 2, "unlock", ",\"id\":\"NEW\",\"unlock_seconds\":33"));
            var read = await GcAchievementJournal.ReadAsync(path);
            Assert.Equal(ReadStatus.Valid, read.Status);
            var join = Assert.Single(read.MatchStarts);
            Assert.Equal("join", join.Reason);
            Assert.Equal(1UL, join.Sequence);
            Assert.Equal("NEW", Assert.Single(read.Unlocks).AchievementId);

            await File.WriteAllTextAsync(path,
                Event("s", 3, "match_start", ",\"reason\":\"reset\""));
            Assert.Equal("reset", Assert.Single(
                (await GcAchievementJournal.ReadAsync(path)).MatchStarts).Reason);
        }
        finally { File.Delete(path); }
    }

}
