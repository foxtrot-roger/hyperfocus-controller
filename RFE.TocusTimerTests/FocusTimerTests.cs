using RFE.FocusTimer;

namespace RFE.TocusTimerTests;

public class FocusTimerTests
{
    readonly FakeStopwatch duration;
    readonly MyFocusTimer sut;
    readonly TimerConfig config;

    public FocusTimerTests()
    {
        duration = new FakeStopwatch();

        sut = new MyFocusTimer(duration);

        config = new TimerConfig(
            TotalDuration: TimeSpan.FromHours(1),
            ReminderDuration: TimeSpan.FromSeconds(1),
            Reminders: [
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(1)]);
    }

    void SeekBeforeReminder()
    {
        duration.Elapsed = config.TotalDuration - config.Reminders.First();
        duration.Elapsed /= 2;

        sut.Update();
    }
    void SeekToTimerEnd()
    {
        duration.Elapsed = config.TotalDuration;

        sut.Update();
    }
    void SeekToReminderStart(int reminder)
    {
        duration.Elapsed = config.TotalDuration - config.Reminders.ElementAt(reminder);

        sut.Update();
    }
    void SeekHalfReminderDuration()
    {
        duration.Elapsed += config.ReminderDuration / 2;

        sut.Update();
    }
    void SeekToReminderEnd(int reminder)
    {
        duration.Elapsed = config.TotalDuration - config.Reminders.ElementAt(reminder);
        duration.Elapsed += config.ReminderDuration;

        sut.Update();
    }
    void SeekAfterReminderEnd(int reminder)
    {
        duration.Elapsed = config.TotalDuration - config.Reminders.ElementAt(reminder);
        duration.Elapsed += config.ReminderDuration;
        duration.Elapsed += TimeSpan.FromTicks(1);

        sut.Update();
    }

    [Fact]
    public void InitialState_Stopped()
    {
        AssertStopped();
    }

    [Fact]
    public void ChangeState_Stopped_To_Hidden()
    {
        sut.Start(config);
        Assert.Equal(TimerState.RunningHidden, sut.State);
        Assert.Equal(config.TotalDuration, sut.TotalDuration);
        Assert.Equal(config.TotalDuration, sut.Remaining);
        Assert.Equal(1, duration.RestartCount);

        AssertHidden();
    }

    [Fact]
    public void ChangeState_Hidden_To_Peeking()
    {
        sut.Start(config);
        SeekBeforeReminder();

        sut.Peek();
        AssertPeeking();

        sut.Update();
        AssertPeeking();
    }
    [Fact]
    public void ChangeState_Hidden_To_Reminder()
    {
        sut.Start(config);

        SeekToReminderStart(0);
        AssertReminder();

        SeekHalfReminderDuration();
        AssertReminder();

        SeekToReminderEnd(0);
        AssertReminder();
    }
    [Fact]
    public void ChangeState_Hidden_To_Interrupting()
    {
        sut.Start(config);
        SeekToTimerEnd();

        AssertInterrupting();
    }

    [Fact]
    public void ChangeState_Peeking_To_Hidden()
    {
        sut.Start(config);
        SeekToTimerEnd();
        sut.Peek();

        sut.Hide();
        AssertHidden();
    }
    [Fact]
    public void ChangeState_Peeking_To_PeekingPaused()
    {
        sut.Start(config);
        SeekToTimerEnd();
        sut.Peek();

        sut.Pause();
        AssertPeekingPaused();
    }
    [Fact]
    public void ChangeState_Peeking_To_Interrupting()
    {
        sut.Start(config);
        SeekToTimerEnd();
        sut.Peek();

        SeekToTimerEnd();
        AssertInterrupting();
    }
    [Fact]
    public void ChangeState_Peeking_To_Stopped()
    {
        sut.Start(config);
        SeekToTimerEnd();
        sut.Peek();

        sut.Stop();
        AssertStopped();
    }

    [Fact]
    public void ChangeState_PeekingPaused_To_Peeking()
    {
        sut.Start(config);
        SeekToTimerEnd();
        sut.Peek();
        sut.Pause();

        sut.Resume();
        AssertPeeking();
    }
    [Fact]
    public void ChangeState_PeekingPaused_To_Stopped()
    {
        sut.Start(config);
        SeekToTimerEnd();
        sut.Peek();
        sut.Pause();

        sut.Stop();
        AssertStopped();
    }

    [Fact]
    public void ChangeState_Reminder_To_ReminderPaused()
    {
        sut.Start(config);
        SeekToReminderStart(0);
        SeekHalfReminderDuration();

        sut.Pause();
        AssertReminderPaused();
    }
    [Fact]
    public void ChangeState_Reminder_To_Stopped()
    {
        sut.Start(config);
        SeekToReminderStart(0);
        SeekHalfReminderDuration();

        sut.Stop();
        AssertStopped();
    }

    [Fact]
    public void ChangeState_Interrupting_To_Stopped()
    {
        sut.Start(config);
        SeekToTimerEnd();

        sut.Stop();
        AssertStopped();
    }

    void AssertStopped()
    {
        Assert.Equal(TimerState.Stopped, sut.State);
        Assert.False(sut.CanPause());
        Assert.False(sut.CanResume());
        Assert.True(sut.CanStart());
        Assert.False(sut.CanStop());
        Assert.False(sut.CanUpdate());
        Assert.False(sut.CanPeek());
        Assert.False(sut.CanHide());
    }
    void AssertHidden()
    {
        Assert.Equal(TimerState.RunningHidden, sut.State);
        Assert.False(sut.CanPause());
        Assert.False(sut.CanResume());
        Assert.False(sut.CanStart());
        Assert.False(sut.CanStop());
        Assert.True(sut.CanUpdate());
        Assert.True(sut.CanPeek());
        Assert.False(sut.CanHide());
    }
    void AssertPeeking()
    {
        Assert.Equal(TimerState.Peeking, sut.State);
        Assert.True(sut.CanPause());
        Assert.False(sut.CanResume());
        Assert.False(sut.CanStart());
        Assert.True(sut.CanStop());
        Assert.True(sut.CanUpdate());
        Assert.False(sut.CanPeek());
        Assert.True(sut.CanHide());
    }
    void AssertPeekingPaused()
    {
        Assert.Equal(TimerState.PeekingPaused, sut.State);
        Assert.False(sut.CanPause());
        Assert.True(sut.CanResume());
        Assert.False(sut.CanStart());
        Assert.True(sut.CanStop());
        Assert.False(sut.CanUpdate());
        Assert.False(sut.CanPeek());
        Assert.False(sut.CanHide());
    }
    void AssertReminder()
    {
        Assert.Equal(TimerState.Reminder, sut.State);
        Assert.True(sut.CanPause());
        Assert.False(sut.CanResume());
        Assert.False(sut.CanStart());
        Assert.True(sut.CanStop());
        Assert.True(sut.CanUpdate());
        Assert.False(sut.CanPeek());
        Assert.False(sut.CanHide());
    }
    void AssertReminderPaused()
    {
        Assert.Equal(TimerState.ReminderPaused, sut.State);
        Assert.False(sut.CanPause());
        Assert.True(sut.CanResume());
        Assert.False(sut.CanStart());
        Assert.True(sut.CanStop());
        Assert.False(sut.CanUpdate());
        Assert.False(sut.CanPeek());
        Assert.False(sut.CanHide());
    }
    void AssertInterrupting()
    {
        Assert.Equal(TimerState.Interrupting, sut.State);
        Assert.False(sut.CanPause());
        Assert.False(sut.CanResume());
        Assert.False(sut.CanStart());
        Assert.True(sut.CanStop());
        Assert.False(sut.CanUpdate());
        Assert.False(sut.CanPeek());
        Assert.False(sut.CanHide());
    }

    [Fact]
    public void Update_When_Hidden()
    {
        sut.Start(config);

        duration.Elapsed = TimeSpan.FromMinutes(20);
        sut.Update();

        Assert.Equal(TimeSpan.FromMinutes(40), sut.Remaining);
        Assert.Equal(1, duration.RestartCount);
    }
    [Fact]
    public void Update_When_Peeking()
    {
        sut.Start(config);

        duration.Elapsed = TimeSpan.FromMinutes(20);
        sut.Peek();

        sut.Update();

        Assert.Equal(TimeSpan.FromMinutes(40), sut.Remaining);
        Assert.Equal(1, duration.RestartCount);
    }
    [Fact]
    public void Update_When_Remininder()
    {
        sut.Start(config);

        // when we reach the start of the reminder
        duration.Elapsed = TimeSpan.FromMinutes(45);
        sut.Update();
        Assert.Equal(TimerState.Reminder, sut.State);

        // during the reminder
        var updatesDuringReminder = 2;
        for (var i = 0; i < updatesDuringReminder; i++)
        {
            var updateNumber = i + 1;
            duration.Elapsed += config.ReminderDuration / updatesDuringReminder;
            sut.Update();
            Assert.Equal(TimerState.Reminder, sut.State);
        }

        // after the reminder
        duration.Elapsed += TimeSpan.FromTicks(1);
        sut.Update();
        Assert.Equal(TimerState.RunningHidden, sut.State);
    }

    public class FakeStopwatch : IStopwatch
    {
        public TimeSpan Elapsed { get; set; }

        public int RestartCount { get; private set; }
        public void Restart()
        {
            RestartCount++;
            Elapsed = TimeSpan.Zero;
        }

        public int ResumeCount { get; private set; }
        public void Resume() => ResumeCount++;

        public int StopCount { get; private set; }
        public void Stop() => StopCount++;
    }
}
