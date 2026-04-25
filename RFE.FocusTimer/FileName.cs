using System;
using System.Collections.Generic;
using System.Linq;

namespace RFE.FocusTimer;

public class MyFocusTimer(IStopwatch countdown)
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
        countdown.Restart();

        reminderDuration = config.ReminderDuration;
        TotalDuration = config.TotalDuration;
        reminders = config.Reminders
            .Distinct()
            .ToList();

        Remaining = TotalDuration - countdown.Elapsed;

        State = TimerState.RunningHidden;
    }

    public bool CanPeek() => State == TimerState.RunningHidden;
    public void Peek()
    {
        State = TimerState.Peeking;
    }

    public bool CanHide() => State == TimerState.Peeking;
    public void Hide()
    {
        State = TimerState.RunningHidden;
    }

    public bool CanPause() => State == TimerState.Peeking || State == TimerState.Reminder;
    public void Pause()
    {
        countdown.Stop();

        if (State == TimerState.Peeking)
            State = TimerState.PeekingPaused;

        else if (State == TimerState.Reminder)
            State = TimerState.ReminderPaused;
    }

    public bool CanResume() => State == TimerState.PeekingPaused || State == TimerState.ReminderPaused;
    public void Resume()
    {
        countdown.Resume();

        if (State == TimerState.PeekingPaused)
            State = TimerState.Peeking;

        else if (State == TimerState.ReminderPaused)
            State = TimerState.Reminder;
    }

    public bool CanStop()
        => State == TimerState.Peeking
        || State == TimerState.PeekingPaused
        || State == TimerState.Reminder
        || State == TimerState.ReminderPaused
        || State == TimerState.Interrupting;
    public void Stop()
    {
        countdown.Stop();

        State = TimerState.Stopped;
    }

    public bool CanUpdate() => State switch
    {
        TimerState.Reminder
        or TimerState.RunningHidden
        or TimerState.Peeking
        => true,

        _ => false,
    };
    public void Update()
    {
        Remaining = TotalDuration - countdown.Elapsed;

        if (Remaining <= TimeSpan.Zero)
        {
            countdown.Stop();
            State = TimerState.Interrupting;
        }
        else if (State != TimerState.Peeking)
        {
            if (reminders.Any(x => x >= Remaining && x <= Remaining + reminderDuration))
                State = TimerState.Reminder;
            else
                State = TimerState.RunningHidden;
        }
    }
}