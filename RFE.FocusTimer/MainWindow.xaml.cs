using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace RFE.FocusTimer;

public partial class MainWindow : Window
{
    DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };

    double initialWidth;
    double initialHeight;
    double initialLeft;
    double initialTop;

    enum DisplayMode { Time, Percent }
    TimeSpan startDuration;
    TimeSpan remainingTime;

    DisplayMode Mode = DisplayMode.Percent;

    List<TimeSpan> warnings = new();
    TimeSpan warningDuration = TimeSpan.FromSeconds(1);


    public MainWindow()
    {
        InitializeComponent();
        lastUpdateTime = Stopwatch.GetTimestamp();
        timer.Tick += (s, e) => Update();

        StartTimerScreen.Visibility = Visibility.Visible;
        WarningScreen.Visibility = Visibility.Collapsed;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;
    }

    long lastUpdateTime;
    void Update()
    {
        var now = Stopwatch.GetTimestamp();
        var delta = Stopwatch.GetElapsedTime(lastUpdateTime, now);
        lastUpdateTime = now;

        remainingTime -= delta;

        if (warnings.Any(x => x >= remainingTime))
        {
            warnings.RemoveAll(x => x >= remainingTime);
            ShowWarningPopup();
        }

        UpdateDislay();

        if (remainingTime <= TimeSpan.Zero)
        {
            timer.Stop();
            TriggerTimeUp();
        }
    }

    void StartBtn_Click(object sender, RoutedEventArgs e)
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

            lastUpdateTime = Stopwatch.GetTimestamp();
            remainingTime = convertDuration(delay);
            startDuration = remainingTime;
            Mode = DisplayMode.Percent;

            initialWidth = Width;
            initialHeight = Height;
            initialLeft = Left;
            initialTop = Top;

            // UI Toggle
            StartTimerScreen.Visibility = Visibility.Collapsed;
            WarningScreen.Visibility = Visibility.Visible;
            TimeIsUpScreen.Visibility = Visibility.Collapsed;

            timer.Start();
            WindowState = WindowState.Minimized;
            UpdateDislay();
        }
    }
    void PauseBtn_Click(object sender, RoutedEventArgs e)
    {
        timer.IsEnabled = !timer.IsEnabled;
        PauseBtn.Content = timer.IsEnabled ? "PAUSE" : "RESUME";
        lastUpdateTime = Stopwatch.GetTimestamp();
    }
    void ResetBtn_Click(object sender, RoutedEventArgs e)
    {
        timer.Stop();

        // 1. Restore Window State
        WindowStyle = WindowStyle.SingleBorderWindow;
        WindowState = WindowState.Normal;
        Topmost = false;

        // 2. Force back to initial position and size
        Width = initialWidth;
        Height = initialHeight;
        Left = initialLeft;
        Top = initialTop;

        // 3. Reset UI
        StartTimerScreen.Visibility = Visibility.Visible;
        WarningScreen.Visibility = Visibility.Collapsed;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;
        SizeToContent = SizeToContent.Height;
    }

    void UpdateDislay()
    {
        char[] heartbeatGlyphs = { '▪', '▫' };
        var currentGlyph = heartbeatGlyphs[DateTime.Now.Second % heartbeatGlyphs.Length];

        var percent = remainingTime.TotalSeconds * 100 / startDuration.TotalSeconds;
        TimerProgressBar.Value = percent;
        if (Mode == DisplayMode.Time)
        {
            if (remainingTime >= TimeSpan.FromHours(1))
                RemainingTime.Text = $"{currentGlyph}{remainingTime:hh\\:mm}";

            else if (remainingTime >= TimeSpan.FromMinutes(1))
                RemainingTime.Text = $"{currentGlyph}{Math.Ceiling(remainingTime.TotalMinutes):00} min.";

            else
                RemainingTime.Text = $"{currentGlyph}{Math.Ceiling(remainingTime.TotalSeconds):00} sec.";
        }
        else
        {
            if (percent >= 95)
                RemainingTime.Text = $"{currentGlyph}⚡";
            else if (percent >= 5)
                RemainingTime.Text = $"{currentGlyph}🚴";
            else
                RemainingTime.Text = $"{currentGlyph}🚩";
        }
    }

    const double warningWidth = 300;
    const double warningHeight = 200;
    const double warningMargin = 20;
    async void ShowWarningPopup()
    {
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

        Mode = DisplayMode.Time;
        await Task.Delay(warningDuration);

        var isCountingDown = remainingTime > TimeSpan.Zero
            && WarningScreen.Visibility == Visibility.Visible
            && timer.IsEnabled;
        if (isCountingDown)
        {
            Mode = DisplayMode.Percent;
            WindowState = WindowState.Minimized;
        }
    }
    void TriggerTimeUp()
    {
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