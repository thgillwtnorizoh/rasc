using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class NotificationPresentationGateTests
{
    [Fact]
    public void ModernToClassicPreviewWaitsForModernExitAndRemainsInOrder()
    {
        var idle = new Dictionary<NotificationProfile, bool>
        {
            [NotificationProfile.Modern] = true,
            [NotificationProfile.Classic] = true
        };
        var observed = new List<string>();
        var gate = new NotificationPresentationGate(profile => idle[profile]);

        gate.Enqueue(NotificationProfile.Modern, () =>
        {
            observed.Add("modern");
            idle[NotificationProfile.Modern] = false;
        });
        gate.Enqueue(NotificationProfile.Classic, () =>
        {
            observed.Add("classic-1");
            idle[NotificationProfile.Classic] = false;
        });
        gate.Enqueue(NotificationProfile.Classic, () => observed.Add("classic-2"));
        Assert.Equal(new[] {"modern"}, observed);
        Assert.Equal(2, gate.PendingCount);

        gate.Pump(); // Old animation is still running, no premature overlap.
        Assert.Equal(new[] {"modern"}, observed);
        idle[NotificationProfile.Modern] = true;
        gate.Pump();
        Assert.Equal(new[] {"modern", "classic-1", "classic-2"}, observed);
        Assert.Equal(0, gate.PendingCount);
        Assert.Equal(NotificationProfile.Classic, gate.ActiveProfile);
    }

    [Fact]
    public void ClassicOnlyPreviewAndIdleModernSwitchAdmitImmediately()
    {
        var log = new List<string>();
        var gate = new NotificationPresentationGate(_ => true);
        gate.Enqueue(NotificationProfile.Classic, () => log.Add("classic-only"));
        gate.Enqueue(NotificationProfile.Modern, () => log.Add("modern-idle"));
        gate.Enqueue(NotificationProfile.Classic, () => log.Add("classic-again"));
        Assert.Equal(new[] {"classic-only", "modern-idle", "classic-again"}, log);
        Assert.Equal(0, gate.PendingCount);
    }

    [Fact]
    public void RapidProfileSwitchesCannotOvertakeQueuedStyles()
    {
        var active = new HashSet<NotificationProfile>();
        var order = new List<string>();
        var gate = new NotificationPresentationGate(profile => !active.Contains(profile));
        gate.Enqueue(NotificationProfile.Modern, () =>
        {
            order.Add("modern-1");
            active.Add(NotificationProfile.Modern);
        });
        gate.Enqueue(NotificationProfile.Classic, () =>
        {
            order.Add("classic-1");
            active.Add(NotificationProfile.Classic);
        });
        gate.Enqueue(NotificationProfile.Modern, () => order.Add("modern-2"));
        Assert.Equal(new[] {"modern-1"}, order);

        active.Remove(NotificationProfile.Modern);
        gate.Pump();
        Assert.Equal(new[] {"modern-1", "classic-1"}, order);
        active.Remove(NotificationProfile.Classic);
        gate.Pump();
        Assert.Equal(new[] {"modern-1", "classic-1", "modern-2"}, order);
        Assert.Equal(0, gate.PendingCount);
    }

    [Fact]
    public void OffIsSilentAndShutdownClearsPendingWithoutRunningCallbacks()
    {
        var gate = new NotificationPresentationGate(_ => false);
        int called = 0;
        gate.Enqueue(NotificationProfile.Off, () => called++);
        Assert.Equal(0, gate.PendingCount);
        gate.Enqueue(NotificationProfile.Modern, () => called++);
        gate.Enqueue(NotificationProfile.Classic, () => called++);
        Assert.Equal(1, called);
        Assert.Equal(1, gate.PendingCount);
        gate.Clear();
        gate.Pump();
        Assert.Equal(1, called);
        Assert.Equal(0, gate.PendingCount);
    }

    [Fact]
    public void ReentrantQueueDoesNotInterruptSingleStyleBurst()
    {
        var log = new List<string>();
        var idle = new Dictionary<NotificationProfile, bool>
        {
            [NotificationProfile.Classic] = true,
            [NotificationProfile.Modern] = true
        };
        NotificationPresentationGate? gate = null;
        gate = new NotificationPresentationGate(profile => idle[profile]);
        gate.Enqueue(NotificationProfile.Classic, () =>
        {
            log.Add("classic-1");
            idle[NotificationProfile.Classic] = false;
            gate.Enqueue(NotificationProfile.Classic, () => log.Add("classic-2"));
            gate.Enqueue(NotificationProfile.Modern, () => log.Add("modern"));
        });
        Assert.Equal(new[] {"classic-1", "classic-2"}, log);
        Assert.Equal(1, gate.PendingCount);
        idle[NotificationProfile.Classic] = true;
        gate.Pump();
        Assert.Equal(new[] {"classic-1", "classic-2", "modern"}, log);
    }
}
