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
using BDFR.LogonUI.Contracts;

namespace BDFR.LogonUI.Demo;

public partial class MainWindow : Window
{
    private readonly LayoutPersistenceService _layout = new();
    private readonly SystemTelemetryService _telemetry = new();
    private readonly SeasonalBackgroundService _backgrounds = new();
    private readonly AnahitaCalendarService _calendar = new();
    private readonly CalendarBrokerClient _broker = new();
    private readonly ThemeSettingsService _themes = new();
    private readonly GaugeStyleSettingsService _gaugeStyleService = new();
    private readonly WelcomeAnimationSettingsService _welcomeAnimationService = new();
    private readonly OrganizationMessageService _organizationMessages = new();
    private ThemeSettings _theme = ThemeSettingsService.Preset("Fluent");
    private GaugeStyleSettings _gaugeStyles = new();
    private WelcomeAnimationSettings _welcomeAnimations = new();
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _telemetryTimer;
    private readonly DispatcherTimer _brokerTimer;
    private readonly DispatcherTimer _seasonCarouselTimer;
    private readonly DispatcherTimer _organizationMessageTimer;

    private bool _brokerRefreshInFlight;

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

        _brokerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        _brokerTimer.Tick += async (_, _) => await RefreshBrokerCalendarAsync();

        _seasonCarouselTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _seasonCarouselTimer.Tick += (_, _) =>
        {
            if (_backgrounds.AdvanceSeasonCarousel(DateTime.Now))
                RefreshBackground();
        };

        _organizationMessageTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _organizationMessageTimer.Tick += (_, _) => RefreshOrganizationMessageIfChanged();

        LayoutCanvas.SizeChanged += (_, _) => EnsureWidgetsInsideCanvas();
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            _clockTimer.Stop();
            _telemetryTimer.Stop();
            _brokerTimer.Stop();
            _seasonCarouselTimer.Stop();
            _organizationMessageTimer.Stop();
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;

        _theme = _themes.Load();
        _gaugeStyles = _gaugeStyleService.Load();
        _welcomeAnimations = _welcomeAnimationService.Load();
        ApplyTheme(_theme);
        ApplyGaugeStyles(_gaugeStyles);

        UpdateClock();
        BuildCalendar(now);
        RefreshBackground(now);
        RefreshOrganizationMessage(force: true);

        if (_backgrounds.SeasonCarouselMode)
            _seasonCarouselTimer.Start();

        _telemetry.Read();
        UpdateTelemetry();

        var hasSavedLayout = _layout.TryLoad(Widgets());
        EnsureWidgetsInsideCanvas();
        SetEditMode(!hasSavedLayout);
        RefreshOrganizationMessage(force: true);

        _clockTimer.Start();
        _telemetryTimer.Start();
        _organizationMessageTimer.Start();

        AgendaBrokerStateText.Text = "Broker…";
        if (await _broker.EnsureBrokerAsync())
        {
            AgendaBrokerStateText.Text = "Broker ✓";
            await RefreshBrokerCalendarAsync();
            _brokerTimer.Start();
        }
        else
        {
            AgendaBrokerStateText.Text = "Broker offline";
            ShowBrokerOfflineState();
        }
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

    private async Task RefreshBrokerCalendarAsync()
    {
        if (_brokerRefreshInFlight)
            return;

        _brokerRefreshInFlight = true;

        try
        {
            var snapshot = await _broker.ReadCalendarAsync(isLocked: true);
            if (snapshot is null)
            {
                AgendaBrokerStateText.Text = "Anahita: بدون Snapshot";
                ShowBrokerOfflineState();
                return;
            }

            AgendaBrokerStateText.Text = "Anahita ✓";
            RenderAgenda(snapshot);
            RenderCalendarNotifications(snapshot);
        }
        catch
        {
            AgendaBrokerStateText.Text = "Broker offline";
            ShowBrokerOfflineState();
        }
        finally
        {
            _brokerRefreshInFlight = false;
        }
    }

    private void RenderAgenda(CalendarLockSnapshot snapshot)
    {
        AgendaItemsPanel.Children.Clear();

        var items = snapshot.Agenda
            .OrderBy(x => x.AllDay ? 0 : 1)
            .ThenBy(x => x.StartsAt)
            .Take(5)
            .ToArray();

        if (items.Length == 0)
        {
            AgendaItemsPanel.Children.Add(new TextBlock
            {
                Text = "برای امروز برنامه‌ای منتشر نشده است",
                Foreground = new SolidColorBrush(Color.FromRgb(175, 195, 216)),
                Margin = new Thickness(0, 5, 0, 0)
            });
            return;
        }

        foreach (var item in items)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var timeText = item.AllDay
                ? "همه‌روز"
                : item.StartsAt?.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture) ?? "—";

            var time = new TextBlock
            {
                Text = timeText,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 184, 207)),
                FontSize = 11,
                FlowDirection = FlowDirection.LeftToRight
            };

            var title = new TextBlock
            {
                Text = item.Title,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 13,
                FlowDirection = FlowDirection.RightToLeft
            };

            Grid.SetColumn(time, 0);
            Grid.SetColumn(title, 1);
            row.Children.Add(time);
            row.Children.Add(title);
            AgendaItemsPanel.Children.Add(row);
        }
    }

    private void RenderCalendarNotifications(CalendarLockSnapshot snapshot)
    {
        CalendarNotificationPanel.Children.Clear();

        var reminders = snapshot.Reminders
            .OrderBy(x => x.FireAtUtc)
            .Take(3)
            .ToArray();

        if (reminders.Length == 0)
        {
            CalendarNotificationPanel.Children.Add(new TextBlock
            {
                Text = "یادآوری فعالی برای نمایش وجود ندارد",
                Foreground = new SolidColorBrush(Color.FromRgb(199, 211, 224)),
                FontSize = 12
            });
            return;
        }

        foreach (var reminder in reminders)
        {
            CalendarNotificationPanel.Children.Add(new TextBlock
            {
                Text = $"{reminder.FireAtUtc.ToLocalTime():HH:mm}  {reminder.Title}",
                Foreground = new SolidColorBrush(Color.FromRgb(199, 211, 224)),
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 2),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }
    }

    private void ShowBrokerOfflineState()
    {
        AgendaItemsPanel.Children.Clear();
        AgendaItemsPanel.Children.Add(new TextBlock
        {
            Text = "Anahita هنوز Snapshot تازه‌ای به Broker نداده است",
            Foreground = new SolidColorBrush(Color.FromRgb(145, 171, 194)),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        });

        CalendarNotificationPanel.Children.Clear();
        CalendarNotificationPanel.Children.Add(new TextBlock
        {
            Text = "داده تقویم در دسترس نیست",
            Foreground = new SolidColorBrush(Color.FromRgb(199, 211, 224)),
            FontSize = 12
        });
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

        SeasonCarouselButton.Content = _backgrounds.SeasonCarouselMode
            ? "Season Carousel: روشن"
            : "Season Carousel: خاموش";

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

        RefreshOrganizationMessage(force: true);
        EnsureWidgetsInsideCanvas();
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
        Place(OrganizationHost, 820, 485, 370, 165);
        Place(QuickHost, 30, 600, 270, 88);
        Place(UnlockHost, 570, 610, 420, 82);

        SetEditMode(true);
        EditStateText.Text = "چیدمان پیش‌فرض بازیابی شد";
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ThemeSettingsWindow(
            _theme,
            _gaugeStyles,
            _welcomeAnimations)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
            return;

        _theme = dialog.ResultSettings;
        _gaugeStyles = dialog.ResultGaugeStyles;
        _welcomeAnimations = dialog.ResultWelcomeAnimations;

        _themes.Save(_theme);
        _gaugeStyleService.Save(_gaugeStyles);
        _welcomeAnimationService.Save(_welcomeAnimations);

        ApplyTheme(_theme);
        ApplyGaugeStyles(_gaugeStyles);

        var welcomeName = WelcomeAnimationCatalog
            .Definition(_welcomeAnimations.Resolve())
            .PersianName;

        EditStateText.Text =
            $"تنظیمات ذخیره شد • {_theme.Preset} • {welcomeName}";
    }

    private void ApplyTheme(ThemeSettings settings)
    {
        ThemeSettingsService.ApplyAppResources(settings);

        var gauges = new[] { CpuGauge, RamGauge, BatteryGauge };
        foreach (var gauge in gauges)
        {
            gauge.TrackBrush = ThemeSettingsService.Brush(settings.GaugeTrack, "#50E6ECF4");
            gauge.NormalBrush = ThemeSettingsService.Brush(settings.GaugeNormal, "#5BDDA5");
            gauge.WarningBrush = ThemeSettingsService.Brush(settings.GaugeWarning, "#FFA54C");
            gauge.CriticalBrush = ThemeSettingsService.Brush(settings.GaugeCritical, "#FF5B4C");
            gauge.MarkerBrush = ThemeSettingsService.Brush(settings.GaugeMarker, "#11233E");
            gauge.ValueBrush = ThemeSettingsService.Brush(settings.GaugeValue, "#FFFFFF");
            gauge.ScaleBrush = ThemeSettingsService.Brush(settings.GaugeScale, "#D3DBE5");
            gauge.LabelBrush = ThemeSettingsService.Brush(settings.GaugeLabel, "#E2E7ED");
        }
    }

    private void ApplyGaugeStyles(GaugeStyleSettings settings)
    {
        CpuGauge.VisualStyle = settings.Resolve("cpu");
        RamGauge.VisualStyle = settings.Resolve("ram");
        BatteryGauge.VisualStyle = settings.Resolve("battery");
    }

    private void WelcomeGallery_Click(object sender, RoutedEventArgs e)
    {
        var gallery = new WelcomeAnimationGalleryWindow(_welcomeAnimations)
        {
            Owner = this
        };

        if (gallery.ShowDialog() != true)
            return;

        _welcomeAnimations = gallery.ResultSettings;
        _welcomeAnimationService.Save(_welcomeAnimations);

        var name = WelcomeAnimationCatalog
            .Definition(_welcomeAnimations.Resolve())
            .PersianName;

        EditStateText.Text = $"انیمیشن خوش‌آمد ذخیره شد • {name}";
    }

    private async void WelcomePreview_Click(
        object sender,
        RoutedEventArgs e)
    {
        await WelcomeOverlay.PlayAsync(
            _welcomeAnimations.Resolve(),
            _welcomeAnimations.DurationSeconds,
            "به BDFR خوش آمدید",
            "پیش‌نمایش انیمیشن پس از ورود موفق");
    }

    private Task PlaySuccessfulLoginWelcomeAsync(string? displayName = null)
    {
        if (!_welcomeAnimations.Enabled)
            return Task.CompletedTask;

        var title = string.IsNullOrWhiteSpace(displayName)
            ? "به BDFR خوش آمدید"
            : $"{displayName}، خوش آمدید";

        return WelcomeOverlay.PlayAsync(
            _welcomeAnimations.Resolve(),
            _welcomeAnimations.DurationSeconds,
            title,
            "ورود با موفقیت انجام شد");
    }

    private void RefreshOrganizationMessageIfChanged()
    {
        if (_organizationMessages.HasChanged())
            RefreshOrganizationMessage(force: true);
    }

    private void RefreshOrganizationMessage(bool force = false)
    {
        if (!force && !_organizationMessages.HasChanged())
            return;

        try
        {
            var message = _organizationMessages.Load();
            var now = DateTimeOffset.Now;
            var active = message.IsActive(now);

            if (!active)
            {
                if (_editMode)
                {
                    OrganizationHost.Visibility = Visibility.Visible;
                    OrganizationNameText.Text = string.IsNullOrWhiteSpace(message.Organization)
                        ? "پیام سازمانی"
                        : message.Organization;
                    OrganizationTitleText.Text = "ویجت پیام سازمانی غیرفعال است";
                    OrganizationMessageText.Text =
                        "در حالت ویرایش نمایش داده می‌شود تا بتوانی جای آن را تنظیم کنی. برای نمایش واقعی، Enabled=true و بازه زمانی معتبر قرار بده.";
                    OrganizationFooterText.Text = message.SourcePath;
                    OrganizationPriorityText.Text = "DISABLED";
                    OrganizationPriorityBar.Background = Brushes.Gray;
                    OrganizationPriorityBadge.Background =
                        new SolidColorBrush(Color.FromArgb(55, 160, 170, 180));
                }
                else
                {
                    OrganizationHost.Visibility = Visibility.Collapsed;
                }

                return;
            }

            OrganizationHost.Visibility = Visibility.Visible;
            OrganizationNameText.Text = message.Organization;
            OrganizationTitleText.Text = message.Title;
            OrganizationMessageText.Text = string.IsNullOrWhiteSpace(message.Message)
                ? "پیامی برای نمایش ثبت نشده است."
                : message.Message;
            OrganizationFooterText.Text = message.Footer;

            var (label, barColor, badgeColor) = message.Priority switch
            {
                OrganizationMessagePriority.Critical =>
                    ("CRITICAL", "#FF5B4C", "#40FF5B4C"),

                OrganizationMessagePriority.Important =>
                    ("IMPORTANT", "#FFA54C", "#40FFA54C"),

                _ =>
                    ("NORMAL", "#4FA3FF", "#404FA3FF")
            };

            OrganizationPriorityText.Text = label;
            OrganizationPriorityBar.Background =
                (Brush)new BrushConverter().ConvertFromString(barColor)!;
            OrganizationPriorityBadge.Background =
                (Brush)new BrushConverter().ConvertFromString(badgeColor)!;

            OrganizationHost.ToolTip =
                $"منبع پیام: {message.SourcePath}";
        }
        catch (Exception ex)
        {
            OrganizationHost.Visibility = Visibility.Visible;
            OrganizationNameText.Text = "BDFR LogonUI";
            OrganizationTitleText.Text = "خطا در فایل پیام سازمانی";
            OrganizationMessageText.Text = ex.Message;
            OrganizationFooterText.Text = _organizationMessages.ActivePath;
            OrganizationPriorityText.Text = "ERROR";
            OrganizationPriorityBar.Background = Brushes.IndianRed;
            OrganizationPriorityBadge.Background =
                new SolidColorBrush(Color.FromArgb(64, 255, 91, 76));
        }
    }

    private void OrganizationMessage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = _organizationMessages.GetEditablePath();

            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });

            EditStateText.Text = "فایل پیام سازمانی باز شد";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "BDFR LogonUI — Organizational Message",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Background_Click(object sender, RoutedEventArgs e)
    {
        var gallery = new WallpaperGalleryWindow(_backgrounds)
        {
            Owner = this
        };

        gallery.ShowDialog();

        if (gallery.WallpaperChanged)
        {
            _seasonCarouselTimer.Stop();
            RefreshBackground();
        }
    }

    private void SeasonCarousel_Click(object sender, RoutedEventArgs e)
    {
        var enabled = !_backgrounds.SeasonCarouselMode;
        _backgrounds.SetSeasonCarouselMode(enabled);

        if (enabled)
            _seasonCarouselTimer.Start();
        else
            _seasonCarouselTimer.Stop();

        RefreshBackground();
    }

    private void EnsureWidgetsInsideCanvas()
    {
        if (LayoutCanvas.ActualWidth <= 0 || LayoutCanvas.ActualHeight <= 0)
            return;

        foreach (var widget in Widgets())
            EnsureWidgetInsideCanvas(widget);
    }

    private void EnsureWidgetInsideCanvas(EditableWidgetHost widget)
    {
        var width = widget.ActualWidth > 0
            ? widget.ActualWidth
            : widget.Width;

        var height = widget.ActualHeight > 0
            ? widget.ActualHeight
            : widget.Height;

        if (double.IsNaN(width) || width <= 0)
            width = widget.DesignWidth;

        if (double.IsNaN(height) || height <= 0)
            height = widget.DesignHeight;

        var maxWidth = Math.Max(widget.MinWidth, LayoutCanvas.ActualWidth - 8);
        var maxHeight = Math.Max(widget.MinHeight, LayoutCanvas.ActualHeight - 8);

        if (width > maxWidth)
        {
            widget.Width = maxWidth;
            width = maxWidth;
        }

        if (height > maxHeight)
        {
            widget.Height = maxHeight;
            height = maxHeight;
        }

        var left = Canvas.GetLeft(widget);
        var top = Canvas.GetTop(widget);

        if (double.IsNaN(left))
            left = 0;

        if (double.IsNaN(top))
            top = 0;

        left = Math.Clamp(
            left,
            0,
            Math.Max(0, LayoutCanvas.ActualWidth - width));

        top = Math.Clamp(
            top,
            0,
            Math.Max(0, LayoutCanvas.ActualHeight - height));

        Canvas.SetLeft(widget, left);
        Canvas.SetTop(widget, top);
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
