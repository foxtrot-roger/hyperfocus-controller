using RFE.FocusTimer;

namespace RFE.TocusTimerTests;

public class FocusTimerTests
{
    [Fact]
    public void Start()
    {
        var duration = new FakeStopwatch();
        var reminder = new FakeStopwatch();

        var sut = new MyFocusTimer(duration, reminder);

        var config = new TimerConfig(
            TotalDuration: TimeSpan.FromHours(1),
            ReminderDuration: TimeSpan.FromSeconds(1),
            Reminders: [
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(1)]);

        sut.Start(config);
        Assert.Equal(TimerState.Running, sut.State);
        Assert.Equal(config.TotalDuration, sut.TotalDuration);
        Assert.Equal(config.TotalDuration, sut.Remaining);
    }

    [Fact]
    public void UpdateBeforeReminder()
    {
        var duration = new FakeStopwatch();
        var reminder = new FakeStopwatch();

        var sut = new MyFocusTimer(duration, reminder);

        var config = new TimerConfig(
            TotalDuration: TimeSpan.FromHours(1),
            ReminderDuration: TimeSpan.FromSeconds(1),
            Reminders: [
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(1)]);

        sut.Start(config);

        duration.Ellapsed = TimeSpan.FromMinutes(20);
        sut.Update();
        Assert.Equal(TimeSpan.FromMinutes(40), sut.Remaining);
    }
    [Fact]
    public void UpdateReachReminder()
    {
        var duration = new FakeStopwatch();
        var reminder = new FakeStopwatch();

        var sut = new MyFocusTimer(duration, reminder);

        var config = new TimerConfig(
            TotalDuration: TimeSpan.FromHours(1),
            ReminderDuration: TimeSpan.FromSeconds(1),
            Reminders: [
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(1)]);

        sut.Start(config);

        duration.Ellapsed = TimeSpan.FromMinutes(45);
        sut.Update();
        Assert.Equal(TimerState.Reminder, sut.State);

        duration.Ellapsed = duration.Ellapsed + TimeSpan.FromSeconds(1);
        sut.Update();
        Assert.Equal(TimerState.Running, sut.State);
    }

    [Fact]
    public void UpdateReachEnd()
    {
        var duration = new FakeStopwatch();
        var reminder = new FakeStopwatch();

        var sut = new MyFocusTimer(duration, reminder);

        var config = new TimerConfig(
            TotalDuration: TimeSpan.FromHours(1),
            ReminderDuration: TimeSpan.FromSeconds(1),
            Reminders: [
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(1)]);

        sut.Start(config);

        duration.Ellapsed = TimeSpan.FromMinutes(45);
        sut.Update();

        duration.Ellapsed = duration.Ellapsed + TimeSpan.FromSeconds(1);
        sut.Update();

        duration.Ellapsed = TimeSpan.FromHours(1);
        sut.Update();
        Assert.Equal(TimerState.Interrupting, sut.State);

        sut.Stop();
        Assert.Equal(TimerState.Stopped, sut.State);
    }

    public class FakeStopwatch : IStopwatch
    {
        public TimeSpan Ellapsed { get; set; }

        public void Reset() => Ellapsed = TimeSpan.Zero;
        public void Start() { }
        public void Stop() { }
    }
}
