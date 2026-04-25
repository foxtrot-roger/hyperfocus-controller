using System;

namespace RFE.FocusTimer;

public interface IStopwatch
{
    void Restart();
    void Resume();
    void Stop();
    TimeSpan Elapsed { get; }
}
