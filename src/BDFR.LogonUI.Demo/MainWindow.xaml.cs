using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BDFR.LogonUI.Demo;

public partial class MainWindow : Window
{
    private readonly LayoutPersistenceService _layout = new();
    private readonly DispatcherTimer _clockTimer;
    private bool _editMode = true;
    private bool _fullScreen;
    private WindowStyle _previousStyle;
    private WindowState _previousState;
    private ResizeMode _previousResizeMode;

    public MainWindow()
    {
        InitializeComponent();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        Loaded += (_, _) =>
        {
            UpdateClock();
            _layout.TryLoad(Widgets());
            SetEditMode(true);
        };

        Closed += (_, _) => _clockTimer.Stop();
    }

    private IEnumerable<EditableWidgetHost> Widgets() =>
        LayoutCanvas.Children.OfType<EditableWidgetHost>();

    private void UpdateClock()
    {
        var now = DateTime.Now;
        ClockText.Text = now.ToString("HH:mm", CultureInfo.InvariantCulture);
        GregorianDateText.Text = now.ToString("MMMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));

        var pc = new PersianCalendar();
        var year = pc.GetYear(now);
        var month = pc.GetMonth(now);
        var day = pc.GetDayOfMonth(now);

        var months = new[]
        {
            "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
        };

        var days = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Saturday] = "شنبه",
            [DayOfWeek.Sunday] = "یکشنبه",
            [DayOfWeek.Monday] = "دوشنبه",
            [DayOfWeek.Tuesday] = "سه‌شنبه",
            [DayOfWeek.Wednesday] = "چهارشنبه",
            [DayOfWeek.Thursday] = "پنجشنبه",
            [DayOfWeek.Friday] = "جمعه"
        };

        PersianDateText.Text = $"{days[now.DayOfWeek]}، {day} {months[month]} {year}";
        PersianMonthText.Text = $"{months[month]} {year}";
    }

    private void SetEditMode(bool enabled)
    {
        _editMode = enabled;
        foreach (var widget in Widgets())
        {
            widget.IsEditMode = enabled;
            widget.IsSelected = false;
        }

        EditStateText.Text = enabled ? "حالت ویرایش فعال" : "چیدمان قفل است";
        EditStateText.Foreground = enabled
            ? System.Windows.Media.Brushes.LightSkyBlue
            : System.Windows.Media.Brushes.LightGreen;
    }

    private void EditMode_Click(object sender, RoutedEventArgs e) => SetEditMode(!_editMode);

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
        Place(SystemHost, 455, 330, 640, 245);
        Place(QuickHost, 30, 600, 270, 88);
        Place(UnlockHost, 570, 610, 420, 82);

        SetEditMode(true);
        EditStateText.Text = "چیدمان پیش‌فرض بازیابی شد";
    }

    private static void Place(EditableWidgetHost widget, double left, double top, double width, double height)
    {
        Canvas.SetLeft(widget, left);
        Canvas.SetTop(widget, top);
        widget.Width = width;
        widget.Height = height;
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

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

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

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
