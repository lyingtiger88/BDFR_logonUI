using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BDFR.LogonUI.Demo;

public partial class MainWindow : Window
{
    private readonly LayoutPersistenceService _layout = new();
    private readonly SystemTelemetryService _telemetry = new();
    private readonly SeasonalBackgroundService _backgrounds = new();
    private readonly AnahitaCalendarService _calendar = new();
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _telemetryTimer;

    private bool _editMode = true;
    private bool _fullScreen;
    private DateTime _calendarDate;
    private WindowStyle _previousStyle;
    private WindowState _previousState;
    private ResizeMode _previousResizeMode;

    public MainWindow()
    {
        InitializeComponent();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();

        _telemetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _telemetryTimer.Tick += (_, _) => UpdateTelemetry();

        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            _clockTimer.Stop();
            _telemetryTimer.Stop();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;

        UpdateClock();
        BuildCalendar(now);
        RefreshBackground(now);

        _telemetry.Read();
        UpdateTelemetry();

        var hasSavedLayout = _layout.TryLoad(Widgets());
        SetEditMode(!hasSavedLayout);

        _clockTimer.Start();
        _telemetryTimer.Start();
    }

    private IEnumerable<EditableWidgetHost> Widgets() =>
        LayoutCanvas.Children.OfType<EditableWidgetHost>();

    private void UpdateClock()
    {
        var now = DateTime.Now;

        ClockText.Text = now.ToString("HH:mm", CultureInfo.InvariantCulture);
        PersianDateText.Text = _calendar.PersianFullDate(now);
        GregorianDateText.Text = _calendar.GregorianFullDate(now);
        HijriDateText.Text = _calendar.HijriFullDate(now);

        if (_calendarDate != now.Date)
        {
            BuildCalendar(now);
            RefreshBackground(now);
        }
    }

    private void BuildCalendar(DateTime now)
    {
        _calendarDate = now.Date;
        PersianMonthText.Text = _calendar.PersianMonthTitle(now);
        TodaySecondaryDatesText.Text =
            $"میلادی {_calendar.GregorianFullDate(now)}  •  قمری {_calendar.HijriFullDate(now)}";

        CalendarDaysGrid.Children.Clear();

        foreach (var day in _calendar.BuildMonth(now))
            CalendarDaysGrid.Children.Add(CreateDayCell(day));
    }

    private static Border CreateDayCell(AnahitaCalendarDay day)
    {
        var background = day.IsToday
            ? new SolidColorBrush(Color.FromArgb(205, 45, 127, 240))
            : Brushes.Transparent;

        if (background.CanFreeze)
            background.Freeze();

        var cell = new Border
        {
            Margin = new Thickness(1.5),
            Padding = new Thickness(2),
            CornerRadius = new CornerRadius(7),
            Background = background,
            Opacity = day.IsCurrentPersianMonth ? 1 : .34,
            ToolTip = day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };

        var grid = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight
        };

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var persianDay = new TextBlock
        {
            Text = AnahitaCalendarService.ToPersianDigits(day.PersianDay.ToString(CultureInfo.InvariantCulture)),
            FontSize = 14,
            FontWeight = day.IsToday ? FontWeights.SemiBold : FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft
        };

        var secondary = new Grid
        {
            Margin = new Thickness(2, 0, 2, 1),
            FlowDirection = FlowDirection.LeftToRight
        };

        secondary.ColumnDefinitions.Add(new ColumnDefinition());
        secondary.ColumnDefinitions.Add(new ColumnDefinition());

        var gregorian = new TextBlock
        {
            Text = day.GregorianDay.ToString(CultureInfo.InvariantCulture),
            FontSize = 7.5,
            Foreground = new SolidColorBrush(Color.FromRgb(165, 205, 238)),
            HorizontalAlignment = HorizontalAlignment.Left,
            FlowDirection = FlowDirection.LeftToRight
        };

        var hijri = new TextBlock
        {
            Text = AnahitaCalendarService.ToArabicIndicDigits(day.HijriDay.ToString(CultureInfo.InvariantCulture)),
            FontFamily = new FontFamily("Traditional Arabic"),
            FontSize = 8.5,
            Foreground = new SolidColorBrush(Color.FromRgb(199, 211, 224)),
            HorizontalAlignment = HorizontalAlignment.Right,
            FlowDirection = FlowDirection.RightToLeft
        };

        Grid.SetColumn(gregorian, 0);
        Grid.SetColumn(hijri, 1);
        secondary.Children.Add(gregorian);
        secondary.Children.Add(hijri);

        Grid.SetRow(persianDay, 0);
        Grid.SetRow(secondary, 1);
        grid.Children.Add(persianDay);
        grid.Children.Add(secondary);

        cell.Child = grid;
        return cell;
    }

    private void UpdateTelemetry()
    {
        try
        {
            var snapshot = _telemetry.Read();

            AnimateGauge(CpuGauge, snapshot.CpuPercent, true);
            AnimateGauge(RamGauge, snapshot.MemoryPercent, true);

            var hasBattery = snapshot.BatteryPercent.HasValue;
            AnimateGauge(BatteryGauge, snapshot.BatteryPercent ?? 0, hasBattery);
            BatteryGauge.Label = hasBattery
                ? snapshot.IsOnAcPower ? "باتری • AC" : "باتری"
                : "باتری • N/A";

            TelemetryStatusText.Text =
                $"LIVE • RAM آزاد {snapshot.AvailableMemoryMb / 1024d:0.0} GB";

            NetworkStatusText.Text = NetworkInterface.GetIsNetworkAvailable()
                ? "Network ✓"
                : "Offline";

            PowerStatusText.Text = hasBattery
                ? snapshot.IsOnAcPower ? "AC ✓" : "Battery"
                : "Desktop";
        }
        catch
        {
            TelemetryStatusText.Text = "Telemetry unavailable";
            CpuGauge.IsAvailable = false;
            RamGauge.IsAvailable = false;
            BatteryGauge.IsAvailable = false;
        }
    }

    private static void AnimateGauge(BDFRGauge gauge, double target, bool available)
    {
        gauge.IsAvailable = available;

        if (!available)
        {
            gauge.BeginAnimation(BDFRGauge.ValueProperty, null);
            gauge.Value = 0;
            return;
        }

        target = Math.Clamp(target, 0, 100);
        var from = Math.Clamp(gauge.Value, 0, 100);

        gauge.BeginAnimation(BDFRGauge.ValueProperty, null);
        gauge.Value = target;

        var animation = new DoubleAnimation
        {
            From = from,
            To = target,
            Duration = TimeSpan.FromMilliseconds(420),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };

        gauge.BeginAnimation(BDFRGauge.ValueProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private void RefreshBackground(DateTime? at = null)
    {
        var now = at ?? DateTime.Now;
        var image = _backgrounds.Resolve(now);

        BackgroundImage.Source = image;
        BackgroundImage.Visibility = image is null ? Visibility.Collapsed : Visibility.Visible;

        ElenaButton.Content = _backgrounds.ElenaMode
            ? "Elena: روشن"
            : "Elena: خاموش";

        var activeName = string.IsNullOrWhiteSpace(_backgrounds.ActivePath)
            ? "بدون فایل تصویری"
            : Path.GetFileName(_backgrounds.ActivePath);

        BackgroundModeText.Text = $"{_backgrounds.CurrentModeLabel(now)} • {activeName}";
    }

    private void SetEditMode(bool enabled)
    {
        _editMode = enabled;

        foreach (var widget in Widgets())
        {
            widget.IsEditMode = enabled;
            widget.IsLayoutLocked = !enabled;
            widget.IsSelected = false;
        }

        EditGridOverlay.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        EditStateText.Text = enabled
            ? "حالت ویرایش فعال"
            : "چیدمان قفل است";

        EditStateText.Foreground = enabled
            ? Brushes.LightSkyBlue
            : Brushes.LightGreen;
    }

    private void EditMode_Click(object sender, RoutedEventArgs e) =>
        SetEditMode(!_editMode);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _layout.Save(Widgets());
        EditStateText.Text = "چیدمان ذخیره شد";
    }

    private void FinishEdit_Click(object sender, RoutedEventArgs e)
    {
        _layout.Save(Widgets());
        SetEditMode(false);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _layout.DeleteSavedLayout();

        Place(CalendarHost, 20, 18, 380, 465);
        Place(ClockHost, 500, 25, 500, 220);
        Place(NotificationsHost, 1080, 18, 390, 440);
        Place(SystemHost, 455, 330, 640, 58);
        Place(CpuGaugeHost, 465, 405, 185, 165);
        Place(RamGaugeHost, 680, 405, 185, 165);
        Place(BatteryGaugeHost, 895, 405, 185, 165);
        Place(QuickHost, 30, 600, 270, 88);
        Place(UnlockHost, 570, 610, 420, 82);

        SetEditMode(true);
        EditStateText.Text = "چیدمان پیش‌فرض بازیابی شد";
    }

    private void Background_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "انتخاب پس‌زمینه BDFR LogonUI",
            Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp|JPEG|*.jpg;*.jpeg|PNG|*.png|Bitmap|*.bmp",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            _backgrounds.ImportCustom(dialog.FileName);
            RefreshBackground();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "BDFR LogonUI",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Elena_Click(object sender, RoutedEventArgs e)
    {
        _backgrounds.SetElenaMode(!_backgrounds.ElenaMode);
        RefreshBackground();
    }

    private void OpenBackgroundFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_backgrounds.BackgroundFolder);

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\\\"{_backgrounds.BackgroundFolder}\\\"",
            UseShellExecute = true
        });
    }

    private static void Place(
        EditableWidgetHost widget,
        double left,
        double top,
        double width,
        double height)
    {
        Canvas.SetLeft(widget, left);
        Canvas.SetTop(widget, top);
        widget.Width = width;
        widget.Height = height;
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e) =>
        ToggleFullscreen();

    private void ToggleFullscreen()
    {
        if (!_fullScreen)
        {
            _previousStyle = WindowStyle;
            _previousState = WindowState;
            _previousResizeMode = ResizeMode;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _fullScreen = true;
        }
        else
        {
            WindowStyle = _previousStyle;
            ResizeMode = _previousResizeMode;
            WindowState = _previousState;
            _fullScreen = false;
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) =>
        Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _fullScreen)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }
}
