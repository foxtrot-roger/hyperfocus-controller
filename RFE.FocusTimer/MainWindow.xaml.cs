using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace RFE.FocusTimer;

public partial class MainWindow : Window
{
    readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    readonly Stopwatch countdown = new();

    double initialWidth;
    double initialHeight;
    double initialLeft;
    double initialTop;

    const double warningWidth = 300;
    const double warningHeight = 200;
    const double warningMargin = 20;

    TimeSpan startDuration;
    TimeSpan warningDuration = TimeSpan.FromSeconds(1);

    List<TimeSpan> warnings = new();

    void Start()
    {
        if (double.TryParse(TimeInput.Text, out var delay))
        {
            Func<double, TimeSpan>? convertDuration = UnitSelector.Text.ToLower() switch
            {
                "seconds" => TimeSpan.FromSeconds,
                "minutes" => TimeSpan.FromMinutes,
                "hours" => TimeSpan.FromHours,
                _ => null
            };
            if (convertDuration is null)
                return;

            // Setup warnings
            var values = Regex.Split(WarningInput.Text, @"[^0-9.,]+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s =>
                {
                    var sanitized = s.Trim().Replace(',', '.');

                    return double.TryParse(sanitized, CultureInfo.InvariantCulture, out var val)
                        ? val
                        : -1;
                })
                .Distinct()
                .Where(val => val > 0)
                .ToArray();

            // TK : could be better but will work for now
            const int ABSOLUTE = 0;
            const int PERCENTS = 1;
            var validReminders = ReminderModeSelector.SelectedIndex switch
            {
                ABSOLUTE => values
                    .Select(convertDuration)
                    .Where(x => x >= warningDuration)
                    .ToList(),

                PERCENTS => values
                    .Select(x => convertDuration(x * delay / 100))
                    .Where(x => x >= warningDuration)
                    .ToList(),

                _ => null
            };
            if (validReminders is null)
                return;

            warnings = validReminders!;

            startDuration = convertDuration(delay);

            initialWidth = Width;
            initialHeight = Height;
            initialLeft = Left;
            initialTop = Top;

            countdown.Restart();
            EnterRunningHidden();
        }
    }
    void StartWarning() { EnterRunningPopup(); }
    void Restore() { EnterPeekingRunning(); }
    void Timeout() { EnterVisibleTimeout(); }
    void EndWarning() { EnterRunningHidden(); }
    void Pause()
    {
        countdown.Stop();
        PauseBtn.Content = "RESUME";
        EnterPeekingPaused();
    }
    void Minimize()
    {
        EnterRunningHidden();
    }
    void Resume()
    {
        PauseBtn.Content = "PAUSE";
        EnterPeekingRunning();
    }
    void NewTimer()
    {
        Width = initialWidth;
        Height = initialHeight;
        Left = initialLeft;
        Top = initialTop;

        EnterVisibleConfiguration();
    }

    Action update = DoNothing;
    static void DoNothing() { }

    enum CurrentState
    {
        VisibleConfiguration,
        RunningHidden,
        RunningPopup,
        PeekingRunning,
        PeekingPaused,
        VisibleTimeout,
    }
    CurrentState currentState = CurrentState.VisibleConfiguration;

    void EnterVisibleConfiguration()
    {
        currentState = CurrentState.VisibleConfiguration;

        WindowStyle = WindowStyle.SingleBorderWindow;
        WindowState = WindowState.Normal;
        Topmost = false;

        StartTimerScreen.Visibility = Visibility.Visible;
        WarningScreen.Visibility = Visibility.Collapsed;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;

        SizeToContent = SizeToContent.Height;
        update = DoNothing;
    }

    void EnterRunningHidden()
    {
        currentState = CurrentState.RunningHidden;

        WindowState = WindowState.Minimized;

        StartTimerScreen.Visibility = Visibility.Collapsed;
        WarningScreen.Visibility = Visibility.Visible;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;

        update = RunningHidden;
    }
    void RunningHidden()
    {
        var remainingTime = startDuration - countdown.Elapsed;
        if (remainingTime <= TimeSpan.Zero)
        {
            Timeout();
        }
        else if (warnings.Any(x => x >= remainingTime))
        {
            warnings.RemoveAll(x => x >= remainingTime);
            StartWarning();
        }
        else
        {
            DisplayPercent();
        }
    }

    TimeSpan popupDuration = TimeSpan.FromSeconds(1);
    Stopwatch popup = new();
    void EnterRunningPopup()
    {
        currentState = CurrentState.RunningPopup;

        StartTimerScreen.Visibility = Visibility.Collapsed;
        WarningScreen.Visibility = Visibility.Visible;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;
        SizeToContent = SizeToContent.Height;

        WindowState = WindowState.Normal;
        Topmost = true;

        Width = warningWidth;
        Height = warningHeight;

        var screen = GetActiveScreenInfo();
        Left = screen.WorkingArea.Left + screen.WorkingArea.Width - Width - warningMargin;
        Top = screen.WorkingArea.Top + warningMargin;

        update = RunningPopup;
        popup.Restart();
    }
    void RunningPopup()
    {
        var remainingTime = startDuration - countdown.Elapsed;
        warnings.RemoveAll(x => x >= remainingTime);

        if (remainingTime <= TimeSpan.Zero)
        {
            Timeout();
        }
        else
        {
            var remainingPopup = popupDuration - popup.Elapsed;
            if (remainingPopup <= TimeSpan.Zero)
            {
                EndWarning();
            }
            else
            {
                DisplayTime();
            }
        }
    }

    void EnterPeekingRunning()
    {
        currentState = CurrentState.PeekingRunning;

        countdown.Start();
        update = DisplayPercent;
    }

    void EnterPeekingPaused()
    {
        currentState = CurrentState.PeekingPaused;

        countdown.Stop();
        DisplayPercent();
        update = DoNothing;
    }

    void EnterVisibleTimeout()
    {
        currentState = CurrentState.VisibleTimeout;

        countdown.Stop();

        StartTimerScreen.Visibility = Visibility.Collapsed;
        WarningScreen.Visibility = Visibility.Collapsed;
        TimeIsUpScreen.Visibility = Visibility.Visible;
        SizeToContent = SizeToContent.Manual;

        var screen = GetActiveScreenInfo();
        Left = screen.WorkingArea.Left;
        Top = screen.WorkingArea.Top;

        WindowState = WindowState.Maximized;
        Topmost = true;

        DisplayArea.Text = MessageInput.Text;
        Activate();
    }


    static char[] heartbeatGlyphs = { '▪', '▫' };
    static char GetCurrentGlyph()
    {
        return heartbeatGlyphs[DateTime.Now.Second % heartbeatGlyphs.Length];
    }
    void DisplayPercent()
    {
        var currentGlyph = GetCurrentGlyph();

        var remainingTime = startDuration - countdown.Elapsed;
        var percent = remainingTime.TotalSeconds * 100 / startDuration.TotalSeconds;
        TimerProgressBar.Value = percent;

        if (percent >= 95)
            RemainingTime.Text = $"{currentGlyph}⚡";
        else if (percent >= 5)
            RemainingTime.Text = $"{currentGlyph}🚴";
        else
            RemainingTime.Text = $"{currentGlyph}🚩";
    }
    void DisplayTime()
    {
        var currentGlyph = GetCurrentGlyph();

        var remainingTime = startDuration - countdown.Elapsed;
        var percent = remainingTime.TotalSeconds * 100 / startDuration.TotalSeconds;
        TimerProgressBar.Value = percent;

        if (remainingTime >= TimeSpan.FromHours(1))
            RemainingTime.Text = $"{currentGlyph}{remainingTime:hh\\:mm}";

        else if (remainingTime >= TimeSpan.FromMinutes(1))
            RemainingTime.Text = $"{currentGlyph}{Math.Ceiling(remainingTime.TotalMinutes):00} min.";

        else
            RemainingTime.Text = $"{currentGlyph}{Math.Ceiling(remainingTime.TotalSeconds):00} sec.";
    }

    public MainWindow()
    {
        InitializeComponent();

        EnterVisibleConfiguration();

        timer.Tick += (s, e) => update();
        timer.Start();
    }

    void StartBtn_Click(object sender, RoutedEventArgs e) => Start();
    void PauseBtn_Click(object sender, RoutedEventArgs e)
    {
        if (currentState == CurrentState.PeekingRunning)
            Pause();
        else if (currentState == CurrentState.PeekingPaused)
            Resume();
    }
    void ResetBtn_Click(object sender, RoutedEventArgs e) => NewTimer();

    void Window_StateChanged(object sender, EventArgs e)
    {
        switch (currentState)
        {
            case CurrentState.RunningHidden:
                Restore();
                break;

            case CurrentState.PeekingRunning:
            case CurrentState.PeekingPaused:
                if (WindowState == WindowState.Minimized)
                    Minimize();
                break;
        }
    }
    void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox focusedTextBox)
        {
            BindingOperations.GetBindingExpression(focusedTextBox, TextBox.TextProperty)
                ?.UpdateSource();
        }

        Properties.Settings.Default.Save();
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    public System.Windows.Forms.Screen GetActiveScreenInfo()
    {
        var activeWindowHandle = GetForegroundWindow();
        return System.Windows.Forms.Screen.FromHandle(activeWindowHandle);
    }


}