using System;
using System.Diagnostics;

namespace RFE.FocusTimer;

public class MyStopwatch : IStopwatch
{
    readonly Stopwatch stopwatch = new();

    public TimeSpan Elapsed => stopwatch.Elapsed;

    public void Restart() => stopwatch.Restart();

    public void Resume() => stopwatch.Start();
    public void Stop() => stopwatch.Stop();
}