using System;
using System.Collections.Generic;

namespace RFE.FocusTimer;

public record TimerConfig(TimeSpan TotalDuration, TimeSpan ReminderDuration, IEnumerable<TimeSpan> Reminders);
