using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RFE.FocusTimer;

public class MyFocusTimer(IStopwatch countdown, IStopwatch reminder)
{
    TimeSpan reminderDuration;
    List<TimeSpan> reminders = [];

    public TimeSpan TotalDuration { get; private set; }
    public TimeSpan Remaining { get; private set; }

    TimerState state;
    public TimerState State
    {
        get => state;
        private set
        {
            if (value == state)
                return;

            state = value;
            StateChanged?.Invoke();
        }
    }

    public event Action? StateChanged;

    public bool CanStart() => State == TimerState.Stopped;
    public void Start(TimerConfig config)
    {
        countdown.Reset();
        countdown.Start();

        TotalDuration = config.TotalDuration;
        reminders = config.Reminders
            .Distinct()
            .ToList();

        Remaining = TotalDuration - countdown.Ellapsed;

        State = TimerState.Running;
    }

    public bool CanPause() => State == TimerState.Running || State == TimerState.Reminder;
    public void Pause()
    {
        countdown.Stop();

        if (State == TimerState.Running)
            State = TimerState.Paused;

        else if (State == TimerState.Reminder)
            State = TimerState.PausedReminder;
    }

    public bool CanResume() => State == TimerState.Paused || State == TimerState.PausedReminder;
    public void Resume()
    {
        countdown.Start();

        if (State == TimerState.Paused)
            State = TimerState.Running;

        else if (State == TimerState.PausedReminder)
            State = TimerState.Reminder;
    }

    public bool CanStop() => State != TimerState.Stopped;
    public void Stop()
    {
        countdown.Stop();

        State = TimerState.Stopped;
    }

    public bool CanUpdate() => State != TimerState.Stopped;
    public void Update()
    {
        Remaining = TotalDuration - countdown.Ellapsed;

        if (Remaining <= TimeSpan.Zero)
        {
            countdown.Stop();

            State = TimerState.Interrupting;
        }
        else if (reminders.Any(x => x >= Remaining && x <= Remaining + reminderDuration))
        {
            reminder.Reset();
            reminder.Start();

            State = TimerState.Reminder;
        }
        else
        {
            reminder.Stop();

            State = TimerState.Running;
        }
    }
}

public enum TimerState
{
    Stopped,
    Running,
    Paused,
    Reminder,
    PausedReminder,
    Interrupting
}

public record TimerConfig(TimeSpan TotalDuration, TimeSpan ReminderDuration, IEnumerable<TimeSpan> Reminders);

public interface IStopwatch
{
    void Start();
    void Stop();
    void Reset();
    TimeSpan Ellapsed { get; }
}
public class MyStopwatch : IStopwatch
{
    readonly Stopwatch stopwatch = new();

    public TimeSpan Ellapsed => stopwatch.Elapsed;

    public void Reset() => stopwatch.Reset();
    public void Start() => stopwatch.Start();
    public void Stop() => stopwatch.Stop();
}