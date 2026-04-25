using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RFE.FocusTimer;

public partial class MainWindow : Window
{
    readonly MyFocusTimer model = new MyFocusTimer(new MyStopwatch());
    readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };

    double initialWidth;
    double initialHeight;
    double initialLeft;
    double initialTop;

    const double warningWidth = 300;
    const double warningHeight = 200;
    const double warningMargin = 20;

    public MainWindow()
    {
        InitializeComponent();

        model.StateChanged += Model_StateChanged;
        EnterVisibleConfiguration();

        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (model.CanUpdate())
            model.Update();

        switch (model.State)
        {
            case TimerState.RunningHidden:
            case TimerState.Peeking:
                DisplayPercent();
                break;
            case TimerState.Reminder:
                DisplayTime();
                break;
        }
    }

    void Model_StateChanged()
    {
        switch (model.State)
        {
            case TimerState.Stopped:
                RestorePosition();
                EnterVisibleConfiguration();
                break;
            case TimerState.RunningHidden:
                EnterRunningHidden();
                break;
            case TimerState.Peeking:
                EnterPeekingRunning();
                break;
            case TimerState.PeekingPaused:
                EnterPeekingPaused();
                break;
            case TimerState.Reminder:
                EnterReminder();
                break;
            case TimerState.ReminderPaused:
                EnterPeekingPaused();
                break;
            case TimerState.Interrupting:
                EnterInterrupting();
                break;

            default:
                break;
        }
    }

    void RestorePosition()
    {
        Width = initialWidth;
        Height = initialHeight;
        Left = initialLeft;
        Top = initialTop;
    }
    void EnterVisibleConfiguration()
    {
        WindowStyle = WindowStyle.SingleBorderWindow;
        WindowState = WindowState.Normal;
        Topmost = false;
        Focusable = true;

        StartTimerScreen.Visibility = Visibility.Visible;
        WarningScreen.Visibility = Visibility.Collapsed;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;

        SizeToContent = SizeToContent.Height;

        Title = "Configure timer";
    }

    void EnterRunningHidden()
    {
        WindowState = WindowState.Minimized;
        Focusable = true;

        StartTimerScreen.Visibility = Visibility.Collapsed;
        WarningScreen.Visibility = Visibility.Visible;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;

        DisplayRunning();
    }
    void EnterReminder()
    {
        StartTimerScreen.Visibility = Visibility.Collapsed;
        WarningScreen.Visibility = Visibility.Visible;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;
        SizeToContent = SizeToContent.Height;

        WindowState = WindowState.Normal;
        Topmost = true;
        Focusable = false;

        Width = warningWidth;
        Height = warningHeight;

        var screen = GetActiveScreenInfo();
        Left = screen.WorkingArea.Left + screen.WorkingArea.Width - Width - warningMargin;
        Top = screen.WorkingArea.Top + warningMargin;

        RemainingTime.Visibility = Visibility.Visible;
        DisplayRunning();
    }
    void EnterPeekingRunning()
    {
        RemainingTime.Visibility = Visibility.Collapsed;

        DisplayRunning();
    }
    void EnterPeekingPaused()
    {
        Focusable = true;

        DisplayPaused();
    }
    void EnterInterrupting()
    {
        Title = "Time's up !";

        StartTimerScreen.Visibility = Visibility.Collapsed;
        WarningScreen.Visibility = Visibility.Collapsed;
        TimeIsUpScreen.Visibility = Visibility.Visible;
        SizeToContent = SizeToContent.Manual;

        var screen = GetActiveScreenInfo();
        Left = screen.WorkingArea.Left;
        Top = screen.WorkingArea.Top;

        WindowState = WindowState.Maximized;
        Topmost = true;

        Activate();
    }

    void DisplayRunning()
    {
        Title = MessageInput.Text + " (Running)";
        PauseBtn.Content = "PAUSE";

        PulseBorder.Background = runningBorder;
        PulseCenter.Background = GetCurrentBrush();

        PulseBorder.Opacity = 1;
        TimerProgressBar.Opacity = 1;

        TimerProgressBar.ClearValue(ProgressBar.ForegroundProperty);
    }
    void DisplayPaused()
    {
        Title = "PAUSED : " + MessageInput.Text;
        PauseBtn.Content = "RESUME";

        PulseBorder.Background = pausedBorder;
        PulseCenter.Background = pausedCenter;

        PulseBorder.Opacity = 0.5;
        TimerProgressBar.Opacity = 0.5;

        TimerProgressBar.Foreground = new SolidColorBrush(Colors.Gray);
    }

    Brush pausedBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"));
    Brush pausedCenter = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888"));
    Brush runningBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10"));
    private readonly Brush[] runningCenter = {
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")), // 01 - Start
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")), // 01 - Start
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")), // 01 - Start
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")), // 01 - Start
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")), // 01 - Start
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27B262")), // 02
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28B664")), // 03
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#29BA66")), // 04
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#29BE68")), // 05
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2AC36B")), // 06
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2BC76D")), // 07
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2BCB6F")), // 08
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2CCF71")), // 09
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2DD473")), // 10
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32D777")), // 11 - Peak Light
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32D777")), // 11 - Peak Light
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32D777")), // 11 - Peak Light
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32D777")), // 11 - Peak Light
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32D777")), // 11 - Peak Light
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2DD473")), // 12
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2CCF71")), // 13
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2BCB6F")), // 14
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2BC76D")), // 15
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2AC36B")), // 16
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#29BE68")), // 17
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#29BA66")), // 18
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28B664")), // 19
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27B262")), // 20 - Return
    };

    Brush GetCurrentBrush()
    {
        var cycleDuration = 1.5;
        var colorDuration = cycleDuration / runningCenter.Length;
        var index = (int)(model.Remaining.TotalSeconds / colorDuration);

        return runningCenter[index % runningCenter.Length];
    }
    void DisplayPercent()
    {
        var remainingTime = model.Remaining;
        var percent = remainingTime.TotalSeconds * 100 / model.TotalDuration.TotalSeconds;
        TimerProgressBar.Value = percent;

        PulseCenter.Background = GetCurrentBrush();
    }
    void DisplayTime()
    {
        var remainingTime = model.Remaining;
        var percent = remainingTime.TotalSeconds * 100 / model.TotalDuration.TotalSeconds;
        TimerProgressBar.Value = percent;

        if (remainingTime >= TimeSpan.FromHours(1))
            RemainingTime.Text = $"{remainingTime:hh\\:mm}";

        else if (remainingTime >= TimeSpan.FromMinutes(1))
            RemainingTime.Text = $"{Math.Ceiling(remainingTime.TotalMinutes):00} min.";

        else
            RemainingTime.Text = $"{Math.Ceiling(remainingTime.TotalSeconds):00} sec.";

        PulseCenter.Background = GetCurrentBrush();
    }

    TimeSpan warningDuration = TimeSpan.FromSeconds(1);
    void Start()
    {
        UpdateBindingSource();

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

            // setup timer duration
            var totalDuration = convertDuration(delay);

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

            initialWidth = Width;
            initialHeight = Height;
            initialLeft = Left;
            initialTop = Top;

            model.Start(new TimerConfig(totalDuration, warningDuration, validReminders));
        }
    }

    void StartBtn_Click(object sender, RoutedEventArgs e) => Start();
    void PauseBtn_Click(object sender, RoutedEventArgs e)
    {
        if (model.CanPause())
            model.Pause();
        else if (model.CanResume())
            model.Resume();
    }
    void ResetBtn_Click(object sender, RoutedEventArgs e) => model.Stop();

    WindowState previous;
    void Window_StateChanged(object sender, EventArgs e)
    {
        var isMinimizing = WindowState == WindowState.Minimized;
        var isRestoring = previous == WindowState.Minimized && WindowState == WindowState.Normal;

        if (isMinimizing && model.CanHide())
            model.Hide();

        else if (isRestoring && model.CanPeek())
            model.Peek();

        previous = WindowState;
    }

    void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        UpdateBindingSource();
    }

    static void UpdateBindingSource()
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