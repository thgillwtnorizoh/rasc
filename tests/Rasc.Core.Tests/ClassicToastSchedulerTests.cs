using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class ClassicToastSchedulerTests
{
    private static UnlockEvent E(int n) =>
        new($"event-{n}", "test-source", $"ACHIEVEMENT_{n}", (uint)(100 + n),
            DateTimeOffset.FromUnixTimeSeconds(100 + n));

    private static ClassicToastFrame Only(ClassicToastScheduler s, int ms) =>
        Assert.Single(s.Advance(TimeSpan.FromMilliseconds(ms)));

    private static ClassicToastFrame ById(IEnumerable<ClassicToastFrame> frames, int id) =>
        Assert.Single(frames, x => x.Event.EventId == $"event-{id}");

    [Fact]
    public void SourceDurationsAndCapacityRemainUnchanged()
    {
        Assert.Equal(3, ClassicToastScheduler.Capacity);
        Assert.Equal(240, ClassicToastScheduler.CardWidth);
        Assert.Equal(94, ClassicToastScheduler.CardHeight);
        Assert.Equal(TimeSpan.FromMilliseconds(450), ClassicToastScheduler.SlideIn);
        Assert.Equal(TimeSpan.FromSeconds(6), ClassicToastScheduler.Display);
        Assert.Equal(TimeSpan.FromMilliseconds(450), ClassicToastScheduler.SlideOut);
    }

    [Fact]
    public void EmptyTimelineStaysIdle()
    {
        var s = new ClassicToastScheduler();
        Assert.Empty(s.Advance(TimeSpan.Zero));
        Assert.True(s.IsIdle);
    }

    [Fact]
    public void R01OneCardSlidesUpFromTaskbarAndNeverFades()
    {
        var s = new ClassicToastScheduler();
        Assert.True(s.Enqueue(E(1)));
        var f = Only(s, 0);
        Assert.Equal(ClassicToastStage.Entering, f.Stage);
        Assert.Equal(282, f.Y);
        Assert.Equal(0, f.VisibleHeight);
        Assert.Equal(1, f.Opacity);
        f = Only(s, 225);
        Assert.Equal(235, f.Y, 5);
        Assert.Equal(47, f.VisibleHeight, 5);
        Assert.Equal(1, f.Opacity);
        f = Only(s, 450);
        Assert.Equal(ClassicToastStage.Visible, f.Stage);
        Assert.Equal(188, f.Y);
        Assert.Equal(94, f.VisibleHeight);
        Assert.Equal(1, f.Opacity);
    }

    [Fact]
    public void R02NextArrivalFromTaskbarPushesFirstCardUpWithoutGap()
    {
        var s = new ClassicToastScheduler();
        s.Enqueue(E(1));
        s.Advance(TimeSpan.Zero);
        s.Advance(TimeSpan.FromMilliseconds(450));
        s.Enqueue(E(2));
        var midway = s.Advance(TimeSpan.FromMilliseconds(675));
        var a = ById(midway,1);
        var b = ById(midway,2);
        Assert.Equal(141, a.Y, 5);
        Assert.Equal(235, b.Y, 5);
        Assert.Equal(a.Y + 94, b.Y, 5);
        Assert.Equal(282, b.Y + b.VisibleHeight, 5);
        var settled = s.Advance(TimeSpan.FromMilliseconds(900));
        Assert.Equal(94, ById(settled,1).Y);
        Assert.Equal(188, ById(settled,2).Y);
    }

    [Fact]
    public void R03ThreeArrivalsAlwaysPushThePriorCards()
    {
        var s = new ClassicToastScheduler();
        for (var i = 1; i <= 3; i++) s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        var mid = s.Advance(TimeSpan.FromMilliseconds(1125)); // third entry halfway
        Assert.Equal(3, mid.Count);
        Assert.Equal(3, s.ActiveCount);
        Assert.Equal(47, ById(mid,1).Y, 5);
        Assert.Equal(141, ById(mid,2).Y,5);
        Assert.Equal(235, ById(mid,3).Y,5);
        var at = s.Advance(TimeSpan.FromMilliseconds(1350));
        Assert.Equal(0, ById(at,1).Y);
        Assert.Equal(94, ById(at,2).Y);
        Assert.Equal(188, ById(at,3).Y);
    }

    [Fact]
    public void R04ThreeFullQueueThenTopSinksBehindMiddleAndFourthRises()
    {
        var s = new ClassicToastScheduler();
        for (int i=1;i<=4;i++) s.Enqueue(E(i));
        Assert.Single(s.Advance(TimeSpan.Zero));
        Assert.Equal(3, s.Advance(TimeSpan.FromMilliseconds(1350)).Count);
        Assert.Equal(1, s.WaitingCount);
        var begin = s.Advance(TimeSpan.FromMilliseconds(6450));
        Assert.Equal(ClassicToastStage.Exiting, ById(begin,1).Stage);
        var half = s.Advance(TimeSpan.FromMilliseconds(6675));
        var top = ById(half,1);
        Assert.Equal(47, top.Y,5);
        Assert.Equal(47, top.VisibleHeight,5);
        Assert.Equal(94, top.Y+top.VisibleHeight,5); // hides behind middle
        Assert.Equal(1, top.Opacity);

        var next = s.Advance(TimeSpan.FromMilliseconds(6900));
        Assert.DoesNotContain(next,x=>x.Event.EventId=="event-1");
        Assert.Equal(ClassicToastStage.Entering, ById(next,4).Stage);
        Assert.Equal(282, ById(next,4).Y);
        var inserted = s.Advance(TimeSpan.FromMilliseconds(7350));
        Assert.Equal(188, ById(inserted,4).Y);
        Assert.Equal(3,s.ActiveCount);
    }

    [Fact]
    public void R05BottomCardSinksIntoTaskbarAndIsClipped()
    {
        var s = new ClassicToastScheduler();
        s.Enqueue(E(1));
        s.Advance(TimeSpan.Zero);
        s.Advance(TimeSpan.FromMilliseconds(450));
        s.Advance(TimeSpan.FromMilliseconds(6450));
        var f = Only(s,6675);
        Assert.Equal(ClassicToastStage.Exiting,f.Stage);
        Assert.Equal(235,f.Y,5);
        Assert.Equal(47,f.VisibleHeight,5);
        Assert.Equal(282,f.Y + f.VisibleHeight,5);
        Assert.Empty(s.Advance(TimeSpan.FromMilliseconds(6900)));
    }

    [Fact]
    public void R06MiddleCardSinksBehindTheBottomCard()
    {
        var s = new ClassicToastScheduler();
        for(var i=1;i<=2;i++)s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        s.Advance(TimeSpan.FromMilliseconds(900));
        var half=s.Advance(TimeSpan.FromMilliseconds(6675));
        var leaving=ById(half,1);
        Assert.Equal(ClassicToastStage.Exiting,leaving.Stage);
        Assert.Equal(141,leaving.Y,5);
        Assert.Equal(47,leaving.VisibleHeight,5);
        Assert.Equal(188,leaving.Y+leaving.VisibleHeight,5);
        Assert.Equal(188,ById(half,2).Y,5);
    }

    [Fact]
    public void R07TopNeverExitsUpwardOutOfHost()
    {
        var s = new ClassicToastScheduler();
        for(var i=1;i<=3;i++)s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        s.Advance(TimeSpan.FromMilliseconds(1350));
        var half=s.Advance(TimeSpan.FromMilliseconds(6675));
        var leaving=ById(half,1);
        Assert.Equal(ClassicToastStage.Exiting,leaving.Stage);
        Assert.Equal(47,leaving.Y,5);
        Assert.Equal(94,leaving.Y+leaving.VisibleHeight,5);
        Assert.Equal(0,ById(half,2).ClipTop);
    }

    [Fact]
    public void R08SixBurstStaysBoundedAndArrivalIsAlwaysFromTaskbar()
    {
        var s = new ClassicToastScheduler();
        for(var i=1;i<=6;i++)Assert.True(s.Enqueue(E(i)));
        var started=new HashSet<string>();
        for(var ms=0;ms<=35000;ms+=25)
        {
            var frames=s.Advance(TimeSpan.FromMilliseconds(ms));
            Assert.InRange(frames.Count,0,3);
            foreach(var frame in frames)
            {
                Assert.Equal(1,frame.Opacity);
                Assert.InRange(frame.ClipTop,0,94);
                Assert.InRange(frame.ClipBottom,frame.ClipTop,94);
                if(frame.Stage == ClassicToastStage.Entering && started.Add(frame.Event.EventId))
                    Assert.Equal(282,frame.Y,5);
            }
        }
        Assert.Equal(6,started.Count);
        Assert.Equal(0,s.WaitingCount);
        Assert.True(s.IsIdle);
    }

    [Fact]
    public void R09DuplicateSuppressedWhileTrackedAndReleasedAfterExit()
    {
        var s=new ClassicToastScheduler();
        Assert.True(s.Enqueue(E(1)));
        Assert.False(s.Enqueue(E(1)));
        s.Advance(TimeSpan.Zero);
        Assert.False(s.Enqueue(E(1)));
        s.Advance(TimeSpan.FromMilliseconds(6900));
        Assert.True(s.Enqueue(E(1))); // durable ledger owns long-term source dedup
    }

    [Fact]
    public void R10RejectsNonMonotonicTimeAndBadId()
    {
        var s = new ClassicToastScheduler();
        s.Advance(TimeSpan.FromSeconds(1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>s.Advance(TimeSpan.Zero));
        Assert.Throws<ArgumentException>(()=>s.Enqueue(E(1) with {EventId=""}));
    }

    [Fact]
    public void R13ClearDestroysPresentationWithoutTouchingSource()
    {
        var s=new ClassicToastScheduler();
        for(var i=1;i<=5;i++)s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        s.Clear();
        Assert.True(s.IsIdle);
        Assert.Empty(s.Advance(TimeSpan.FromSeconds(1)));
        Assert.True(s.Enqueue(E(1)));
    }
}
