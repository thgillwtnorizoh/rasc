using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

/// <summary>
/// Permanent acceptance gates for scheduler-owned content and transactional
/// snapshots. Renderer consumers must never reconstruct text/artwork through a
/// second mutable achievement dictionary.
/// </summary>
public sealed class ClassicToastOwnershipTests
{
    private static UnlockEvent Award(int i) =>
        new($"owner-{i}", "test", $"MEDAL_{i}", (uint)(100 + i),
            DateTimeOffset.FromUnixTimeSeconds(200 + i));

    [Fact]
    public void CapturedFrameRemainsCompleteAfterSchedulerRetiresItsCard()
    {
        var scheduler = new ClassicToastScheduler();
        var icon = new byte[] { 1, 2, 3, 4 };
        var payload = new ClassicToastContent("Title", "Original description", icon);
        Assert.True(scheduler.Enqueue(Award(1), payload));
        icon[0] = 99; // caller does not own scheduler metadata

        var snapshot = scheduler.AdvancePresentation(TimeSpan.Zero);
        var frame = Assert.Single(snapshot.Frames);
        Assert.Equal("Title", frame.Content!.Title);
        Assert.Equal("Original description", frame.Content.Description);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, frame.Content.CopyIconJpeg());
        Assert.Single(snapshot.Transitions, x => x.Kind == ClassicToastTransitionKind.Started);
        Assert.Empty(scheduler.AdvancePresentation(TimeSpan.Zero).Transitions);

        var retired = scheduler.AdvancePresentation(TimeSpan.FromSeconds(20));
        Assert.True(retired.IsIdle);
        Assert.Empty(retired.Frames);
        Assert.Equal(ClassicToastTransitionKind.Retired, retired.Transitions.Last().Kind);

        // The old outer render's snapshot is self-contained even after the
        // scheduler has pruned the original card from every live collection.
        Assert.Equal(282, frame.Y);
        Assert.Equal("Title", frame.Content.Title);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, frame.Content.CopyIconJpeg());
        var secondCopy = frame.Content.CopyIconJpeg()!;
        secondCopy[0] = 77;
        Assert.Equal(1, frame.Content.CopyIconJpeg()![0]);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ClassicToastFrame>)snapshot.Frames)[0] = frame);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(20)]
    [InlineData(100)]
    public void RapidBurstsHaveExactlyOneStartAndRetirementPerCard(int count)
    {
        var scheduler = new ClassicToastScheduler();
        for (var i = 0; i < count; i++)
            Assert.True(scheduler.Enqueue(Award(i),
                new ClassicToastContent($"Title {i}", $"Goal {i}", [1, 2, 3])));
        Assert.False(scheduler.Enqueue(Award(0),
            new ClassicToastContent("duplicate", "duplicate")));

        var starts = new HashSet<string>(StringComparer.Ordinal);
        var settled = new HashSet<string>(StringComparer.Ordinal);
        var retired = new HashSet<string>(StringComparer.Ordinal);
        var displayed = new HashSet<string>(StringComparer.Ordinal);
        var audible = new HashSet<string>(StringComparer.Ordinal);
        for (var step = 0; step <= 2400; step++)
        {
            var snapshot = scheduler.AdvancePresentation(
                TimeSpan.FromMilliseconds(step * 250L));
            Assert.InRange(snapshot.Frames.Count, 0, ClassicToastScheduler.Capacity);
            Assert.Equal(snapshot.Frames.Count,
                snapshot.Frames.Select(x => x.Event.EventId).Distinct().Count());

            foreach (var f in snapshot.Frames)
            {
                Assert.NotNull(f.Content);
                Assert.Equal("Title " + f.Event.EventId["owner-".Length..],
                    f.Content.Title);
                Assert.InRange(f.ClipTop, 0, 94);
                Assert.InRange(f.ClipBottom, f.ClipTop, 94);
                Assert.InRange(f.Y, 0, ClassicToastScheduler.ViewportHeight);
                displayed.Add(f.Event.EventId);
            }
            foreach (var transition in snapshot.Transitions)
            {
                switch (transition.Kind)
                {
                    case ClassicToastTransitionKind.Started:
                        Assert.True(starts.Add(transition.Event.EventId),
                            "A start transition was emitted twice");
                        // Sound is an exactly-once effect for an admission that
                        // has an actual visual in this snapshot, never each WPF
                        // frame, and never while merely queued.
                        if (snapshot.Frames.Any(f =>
                            f.Event.EventId == transition.Event.EventId))
                            Assert.True(audible.Add(transition.Event.EventId));
                        break;
                    case ClassicToastTransitionKind.Settled:
                        Assert.True(settled.Add(transition.Event.EventId));
                        break;
                    case ClassicToastTransitionKind.Retired:
                        Assert.True(retired.Add(transition.Event.EventId));
                        break;
                }
            }
            if (snapshot.IsIdle && retired.Count == count) break;
        }
        Assert.Equal(count, starts.Count);
        Assert.Equal(count, settled.Count);
        Assert.Equal(count, retired.Count);
        Assert.Equal(count, displayed.Count);
        Assert.Equal(count, audible.Count);
        Assert.True(scheduler.IsIdle);
        Assert.Equal(0, scheduler.WaitingCount);
    }

    [Fact]
    public void DuplicateAndClockJumpNeverReturnFramesWithMissingContent()
    {
        var scheduler = new ClassicToastScheduler();
        for (var i = 0; i < 20; i++)
            scheduler.Enqueue(Award(i),
                new ClassicToastContent($"Name {i}", $"Details {i}"));
        var zero = scheduler.AdvancePresentation(TimeSpan.Zero);
        var first = Assert.Single(zero.Frames);
        Assert.Equal("Name 0", first.Content!.Title);
        var jump = scheduler.AdvancePresentation(TimeSpan.FromMinutes(30));
        Assert.True(jump.IsIdle);
        Assert.Empty(jump.Frames);
        Assert.Equal(19, jump.Transitions.Count(t =>
            t.Kind == ClassicToastTransitionKind.Started));
        Assert.Equal(20, jump.Transitions.Count(t =>
            t.Kind == ClassicToastTransitionKind.Retired));
        Assert.Equal("Name 0", first.Content.Title);
        Assert.True(scheduler.Enqueue(Award(0),
            new ClassicToastContent("New life", "A re-award after retirement")));
        Assert.Equal("New life",
            Assert.Single(scheduler.AdvancePresentation(TimeSpan.FromMinutes(30)).Frames).Content!.Title);
    }

    [Fact]
    public void ClearRemovesAllOwnedPayloadsButCannotChangeCapturedFrames()
    {
        var scheduler = new ClassicToastScheduler();
        scheduler.Enqueue(Award(5), new ClassicToastContent("Safe", "Retain"));
        var before = Assert.Single(scheduler.AdvancePresentation(TimeSpan.Zero).Frames);
        scheduler.Clear();
        Assert.Empty(scheduler.AdvancePresentation(TimeSpan.FromSeconds(1)).Frames);
        Assert.True(scheduler.IsIdle);
        Assert.Equal("Safe", before.Content!.Title);
    }
}
