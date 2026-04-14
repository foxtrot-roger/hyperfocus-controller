using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        var last = Stopwatch.GetTimestamp();
        timer.Tick += (s, e) =>
        {
            var now = Stopwatch.GetTimestamp();
            var delta = Stopwatch.GetElapsedTime(last, now);
            last = now;

            Update(delta);
        };

        StartTimerScreen.Visibility = Visibility.Visible;
        WarningScreen.Visibility = Visibility.Collapsed;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;
    }

    void Update(TimeSpan delta)
    {
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

            remainingTime = convertDuration(delay);
            startDuration = remainingTime;

            Mode = DisplayMode.Percent;

            // Setup warnings
            var values = WarningInput.Text.Split(',')
                .Select(s => double.TryParse(s.Trim(), out var val) ? val : -1)
                .Distinct()
                .Where(val => val > 0);

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

    async void ShowWarningPopup()
    {
        StartTimerScreen.Visibility = Visibility.Collapsed;
        WarningScreen.Visibility = Visibility.Visible;
        TimeIsUpScreen.Visibility = Visibility.Collapsed;
        SizeToContent = SizeToContent.Height;

        // 1. Prepare Window for Popup
        WindowState = WindowState.Normal;
        Topmost = true;

        // 2. Set Popup Size
        Width = 300;
        Height = 200;

        // 3. Position at Top-Right
        var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
        var windowHandle = windowInteropHelper.Handle;

        // Find the screen associated with that handle
        var screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
        Left = screen.WorkingArea.Left + screen.WorkingArea.Width - Width - 20;
        Top = screen.WorkingArea.Top + 20;

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
}