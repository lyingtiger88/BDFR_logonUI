using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class ThemeSettingsWindow : Window
{
    private sealed record GaugeTarget(string Id, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private readonly ComboBox _preset = new();
    private readonly Dictionary<string, TextBox> _fields =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _swatches =
        new(StringComparer.Ordinal);

    private readonly ComboBox _gaugeTarget = new();
    private readonly Dictionary<GaugeVisualStyle, Border> _styleCards = new();
    private readonly TextBlock _styleStatus = new();

    private readonly Dictionary<WelcomeAnimationStyle, Border> _welcomeCards = new();
    private readonly TextBlock _welcomeStatus = new();
    private readonly CheckBox _welcomeEnabled = new();
    private readonly Slider _welcomeDuration = new();
    private readonly WelcomeAnimationOverlay _welcomePreview = new();

    private GaugeVisualStyle _selectedStyle;
    private GaugeStyleSettings _gaugeStyles;
    private WelcomeAnimationStyle _selectedWelcomeStyle;
    private WelcomeAnimationSettings _welcomeSettings;

    public ThemeSettings ResultSettings { get; private set; }
    public GaugeStyleSettings ResultGaugeStyles => _gaugeStyles.Clone();
    public WelcomeAnimationSettings ResultWelcomeAnimations => _welcomeSettings.Clone();

    public ThemeSettingsWindow(
        ThemeSettings current,
        GaugeStyleSettings gaugeStyles,
        WelcomeAnimationSettings welcomeSettings)
    {
        ResultSettings = current;
        _gaugeStyles = gaugeStyles.Clone();
        _welcomeSettings = welcomeSettings.Clone();
        _selectedWelcomeStyle = _welcomeSettings.Resolve();

        Title = "BDFR LogonUI — Theme, Gauge & Welcome";
        Width = 850;
        Height = 860;
        MinWidth = 720;
        MinHeight = 700;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(20, 27, 38));
        Foreground = Brushes.White;

        var root = new Grid { Margin = new Thickness(20) };
        root.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(
            new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star)
            });
        root.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "سفارشی‌سازی تم، گیج و انیمیشن خوش‌آمدگویی",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Thickness(0, 0, 0, 14)
        };

        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var panel = new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft
        };

        scroll.Content = panel;

        panel.Children.Add(Section("مدل گیج"));
        panel.Children.Add(new TextBlock
        {
            Text = "یکی از ۱۷ مدل را انتخاب کنید؛ می‌توانید آن را فقط روی CPU/RAM/Battery یا روی همه گیج‌ها اعمال کنید.",
            Foreground = new SolidColorBrush(Color.FromRgb(179, 193, 208)),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var targetRow = new Grid
        {
            Margin = new Thickness(0, 0, 0, 10)
        };
        targetRow.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
        targetRow.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto });
        targetRow.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto });

        _gaugeTarget.ItemsSource = new[]
        {
            new GaugeTarget("all", "همه گیج‌ها"),
            new GaugeTarget("cpu", "CPU"),
            new GaugeTarget("ram", "RAM"),
            new GaugeTarget("battery", "Battery")
        };
        _gaugeTarget.SelectedIndex = 0;
        _gaugeTarget.MinWidth = 180;
        _gaugeTarget.Margin = new Thickness(0, 0, 8, 0);
        _gaugeTarget.SelectionChanged += (_, _) =>
            LoadSelectedStyleForTarget();

        var applyTarget = Button("اعمال به انتخاب", "#394655");
        applyTarget.Click += (_, _) => ApplyStyleToTarget();

        var applyAll = Button("اعمال به همه", "#2F8CF0");
        applyAll.Click += (_, _) => ApplyStyleToAll();

        Grid.SetColumn(_gaugeTarget, 0);
        Grid.SetColumn(applyTarget, 1);
        Grid.SetColumn(applyAll, 2);

        targetRow.Children.Add(_gaugeTarget);
        targetRow.Children.Add(applyTarget);
        targetRow.Children.Add(applyAll);
        panel.Children.Add(targetRow);

        var styleGrid = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Thickness(0, 0, 0, 8)
        };

        foreach (var definition in GaugeStyleCatalog.All)
            styleGrid.Children.Add(CreateStyleCard(definition));

        panel.Children.Add(styleGrid);

        _styleStatus.Foreground =
            new SolidColorBrush(Color.FromRgb(129, 190, 239));
        _styleStatus.FontSize = 11;
        _styleStatus.Margin = new Thickness(0, 0, 0, 12);
        _styleStatus.FlowDirection = FlowDirection.RightToLeft;
        panel.Children.Add(_styleStatus);


        panel.Children.Add(Section("انیمیشن خوش‌آمدگویی پس از ورود"));
        panel.Children.Add(new TextBlock
        {
            Text = "یکی از ۹ انیمیشن را انتخاب کنید. همین Preset بعداً پس از تأیید موفق رمز/PIN توسط مسیر ورود واقعی اجرا می‌شود.",
            Foreground = new SolidColorBrush(Color.FromRgb(179, 193, 208)),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });

        _welcomeEnabled.Content = "نمایش انیمیشن خوش‌آمدگویی پس از ورود موفق";
        _welcomeEnabled.IsChecked = _welcomeSettings.Enabled;
        _welcomeEnabled.Foreground = Brushes.Gainsboro;
        _welcomeEnabled.FlowDirection = FlowDirection.RightToLeft;
        _welcomeEnabled.Margin = new Thickness(0, 0, 0, 10);
        panel.Children.Add(_welcomeEnabled);

        var welcomeGrid = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Thickness(0, 0, 0, 8)
        };

        foreach (var definition in WelcomeAnimationCatalog.All)
            welcomeGrid.Children.Add(CreateWelcomeCard(definition));

        panel.Children.Add(welcomeGrid);

        var welcomeControls = new Grid
        {
            Margin = new Thickness(0, 4, 0, 10)
        };
        welcomeControls.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });
        welcomeControls.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = GridLength.Auto
        });

        var durationPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft
        };

        durationPanel.Children.Add(new TextBlock
        {
            Text = "مدت:",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Gainsboro,
            Margin = new Thickness(8, 0, 0, 0)
        });

        _welcomeDuration.Minimum = 1.5;
        _welcomeDuration.Maximum = 8;
        _welcomeDuration.Value = Math.Clamp(
            _welcomeSettings.DurationSeconds,
            1.5,
            8);
        _welcomeDuration.Width = 190;
        _welcomeDuration.TickFrequency = .5;
        _welcomeDuration.IsSnapToTickEnabled = true;
        _welcomeDuration.Margin = new Thickness(8, 0, 8, 0);
        durationPanel.Children.Add(_welcomeDuration);

        var durationValue = new TextBlock
        {
            Width = 52,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(145, 190, 230)),
            FlowDirection = FlowDirection.LeftToRight
        };

        void UpdateDurationLabel() =>
            durationValue.Text = $"{_welcomeDuration.Value:0.0}s";

        _welcomeDuration.ValueChanged += (_, _) => UpdateDurationLabel();
        UpdateDurationLabel();
        durationPanel.Children.Add(durationValue);

        var previewWelcome = Button("پیش‌نمایش انتخاب‌شده", "#2F8CF0");
        previewWelcome.Click += async (_, _) =>
        {
            await _welcomePreview.PlayAsync(
                _selectedWelcomeStyle,
                _welcomeDuration.Value,
                "به BDFR خوش آمدید",
                "پیش‌نمایش انیمیشن انتخاب‌شده");
        };

        Grid.SetColumn(durationPanel, 0);
        Grid.SetColumn(previewWelcome, 1);
        welcomeControls.Children.Add(durationPanel);
        welcomeControls.Children.Add(previewWelcome);
        panel.Children.Add(welcomeControls);

        _welcomeStatus.Foreground =
            new SolidColorBrush(Color.FromRgb(129, 190, 239));
        _welcomeStatus.FontSize = 11;
        _welcomeStatus.Margin = new Thickness(0, 0, 0, 12);
        _welcomeStatus.FlowDirection = FlowDirection.RightToLeft;
        panel.Children.Add(_welcomeStatus);

        panel.Children.Add(Section("پریست رنگ"));
        _preset.ItemsSource = new[]
        {
            "Fluent",
            "Emerald",
            "Sunset",
            "Cyber",
            "Monochrome",
            "Light Gauge"
        };
        _preset.SelectedItem = current.Preset;
        _preset.Margin = new Thickness(0, 0, 0, 14);
        _preset.SelectionChanged += (_, _) =>
        {
            if (_preset.SelectedItem is string name)
                LoadIntoFields(ThemeSettingsService.Preset(name));
        };
        panel.Children.Add(_preset);

        panel.Children.Add(Section("رنگ‌های رابط"));
        AddColorField(
            panel,
            "CardSurface",
            "رنگ شیشه / کارت",
            current.CardSurface);
        AddColorField(
            panel,
            "CardBorder",
            "حاشیه کارت",
            current.CardBorder);
        AddColorField(
            panel,
            "Accent",
            "Accent",
            current.Accent);
        AddColorField(
            panel,
            "ToolbarSurface",
            "نوار ابزار",
            current.ToolbarSurface);

        panel.Children.Add(Section("رنگ‌های گیج"));
        AddColorField(
            panel,
            "GaugeTrack",
            "مسیر خالی",
            current.GaugeTrack);
        AddColorField(
            panel,
            "GaugeNormal",
            "Normal",
            current.GaugeNormal);
        AddColorField(
            panel,
            "GaugeWarning",
            "Warning",
            current.GaugeWarning);
        AddColorField(
            panel,
            "GaugeCritical",
            "Critical",
            current.GaugeCritical);
        AddColorField(
            panel,
            "GaugeMarker",
            "Marker / Needle",
            current.GaugeMarker);
        AddColorField(
            panel,
            "GaugeValue",
            "عدد / Needle Highlight",
            current.GaugeValue);
        AddColorField(
            panel,
            "GaugeScale",
            "Ticks / Scale",
            current.GaugeScale);
        AddColorField(
            panel,
            "GaugeLabel",
            "Label",
            current.GaugeLabel);

        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 14, 0, 0)
        };

        var apply = Button("اعمال و ذخیره", "#2F8CF0");
        apply.Click += (_, _) =>
        {
            ResultSettings = ReadSettings();
            _welcomeSettings.Style = _selectedWelcomeStyle.ToString();
            _welcomeSettings.Enabled = _welcomeEnabled.IsChecked == true;
            _welcomeSettings.DurationSeconds = _welcomeDuration.Value;
            DialogResult = true;
        };

        var cancel = Button("لغو", "#394655");
        cancel.Click += (_, _) => DialogResult = false;

        var reset = Button("Fluent پیش‌فرض", "#394655");
        reset.Click += (_, _) =>
        {
            _preset.SelectedItem = "Fluent";
            LoadIntoFields(ThemeSettingsService.Preset("Fluent"));
            _gaugeStyles = new GaugeStyleSettings();
            _welcomeSettings = new WelcomeAnimationSettings();
            _selectedWelcomeStyle = _welcomeSettings.Resolve();
            _welcomeEnabled.IsChecked = _welcomeSettings.Enabled;
            _welcomeDuration.Value = _welcomeSettings.DurationSeconds;
            LoadSelectedStyleForTarget();
            SelectWelcomeStyle(_selectedWelcomeStyle);
        };

        buttons.Children.Add(apply);
        buttons.Children.Add(cancel);
        buttons.Children.Add(reset);

        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);

        _welcomePreview.SetValue(Grid.RowSpanProperty, 3);
        Panel.SetZIndex(_welcomePreview, 1000);
        root.Children.Add(_welcomePreview);

        Content = root;
        LoadSelectedStyleForTarget();
        SelectWelcomeStyle(_selectedWelcomeStyle);
    }


    private Border CreateWelcomeCard(WelcomeAnimationDefinition definition)
    {
        var preview = new Grid
        {
            Width = 176,
            Height = 82,
            Background = WelcomePreviewBrush(definition.Style),
            ClipToBounds = true
        };

        preview.Children.Add(new Ellipse
        {
            Width = definition.Style == WelcomeAnimationStyle.MinimalCircle ? 58 : 28,
            Height = definition.Style == WelcomeAnimationStyle.MinimalCircle ? 58 : 28,
            Stroke = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
            StrokeThickness = 1.4,
            Fill = definition.Style == WelcomeAnimationStyle.ParticleBloom
                ? new SolidColorBrush(Color.FromArgb(80, 255, 197, 213))
                : Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = .8
        });

        preview.Children.Add(new TextBlock
        {
            Text = ((int)definition.Style).ToString("00"),
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(7)
        });

        var name = new TextBlock
        {
            Text = definition.PersianName,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Thickness(3, 5, 3, 0)
        };

        var english = new TextBlock
        {
            Text = definition.DisplayName,
            FontSize = 8.5,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 193, 210)),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(3, 2, 3, 0)
        };

        var stack = new StackPanel();
        stack.Children.Add(preview);
        stack.Children.Add(name);
        stack.Children.Add(english);

        var border = new Border
        {
            Width = 196,
            Height = 132,
            Padding = new Thickness(7),
            Margin = new Thickness(5),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(
                Color.FromArgb(70, 255, 255, 255)),
            Background = new SolidColorBrush(
                Color.FromArgb(95, 30, 40, 52)),
            Child = stack,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = definition.Description
        };

        border.MouseLeftButtonUp += (_, _) =>
            SelectWelcomeStyle(definition.Style);

        _welcomeCards[definition.Style] = border;
        return border;
    }

    private void SelectWelcomeStyle(WelcomeAnimationStyle style)
    {
        _selectedWelcomeStyle = style;

        foreach (var pair in _welcomeCards)
        {
            pair.Value.BorderBrush = pair.Key == style
                ? ThemeSettingsService.Brush("#2F8CF0", "#2F8CF0")
                : new SolidColorBrush(
                    Color.FromArgb(70, 255, 255, 255));

            pair.Value.BorderThickness = pair.Key == style
                ? new Thickness(3)
                : new Thickness(1);
        }

        var definition = WelcomeAnimationCatalog.Definition(style);
        _welcomeStatus.Text =
            $"انتخاب فعلی: {definition.PersianName} — {definition.DisplayName}";
    }

    private static Brush WelcomePreviewBrush(WelcomeAnimationStyle style)
        => style switch
        {
            WelcomeAnimationStyle.PersianSunrise =>
                Gradient("#10274A", "#D77B4E"),

            WelcomeAnimationStyle.ElegantFade =>
                Gradient("#061122", "#173E63"),

            WelcomeAnimationStyle.ParticleBloom =>
                Gradient("#2B1426", "#0B0F19"),

            WelcomeAnimationStyle.AuroraFlow =>
                Gradient("#08223B", "#0E6C70"),

            WelcomeAnimationStyle.GlassPanels =>
                Gradient("#0C223B", "#1A4B76"),

            WelcomeAnimationStyle.TypographyWave =>
                Gradient("#090A0F", "#4F351D"),

            WelcomeAnimationStyle.NatureSeasons =>
                Gradient("#D180A4", "#47795D"),

            WelcomeAnimationStyle.MinimalCircle =>
                Gradient("#050D1B", "#102D50"),

            WelcomeAnimationStyle.CityToDesktop =>
                Gradient("#102B4C", "#4E2A3B"),

            _ => Gradient("#0B1727", "#243A54")
        };

    private static Brush Gradient(string top, string bottom)
    {
        var converter = new BrushConverter();
        var first = (Color)ColorConverter.ConvertFromString(top);
        var second = (Color)ColorConverter.ConvertFromString(bottom);

        return new LinearGradientBrush(
            new GradientStopCollection
            {
                new(first, 0),
                new(second, 1)
            },
            new Point(0, 0),
            new Point(1, 1));
    }

    private Border CreateStyleCard(GaugeStyleDefinition definition)
    {
        var gauge = new BDFRGauge
        {
            Width = 118,
            Height = 92,
            Value = 62,
            Label = "CPU",
            VisualStyle = definition.Style,
            IsHitTestVisible = false,
            TrackBrush = ThemeSettingsService.Brush(
                ResultSettings.GaugeTrack,
                "#50E6ECF4"),
            NormalBrush = ThemeSettingsService.Brush(
                ResultSettings.GaugeNormal,
                "#5BDDA5"),
            WarningBrush = ThemeSettingsService.Brush(
                ResultSettings.GaugeWarning,
                "#FFA54C"),
            CriticalBrush = ThemeSettingsService.Brush(
                ResultSettings.GaugeCritical,
                "#FF5B4C"),
            MarkerBrush = ThemeSettingsService.Brush(
                ResultSettings.GaugeMarker,
                "#11233E"),
            ValueBrush = ThemeSettingsService.Brush(
                ResultSettings.GaugeValue,
                "#FFFFFF"),
            ScaleBrush = ThemeSettingsService.Brush(
                ResultSettings.GaugeScale,
                "#D3DBE5"),
            LabelBrush = ThemeSettingsService.Brush(
                ResultSettings.GaugeLabel,
                "#E2E7ED")
        };

        var label = new TextBlock
        {
            Text = definition.DisplayName,
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(
                Color.FromRgb(207, 218, 229)),
            Margin = new Thickness(2, 3, 2, 0)
        };

        var stack = new StackPanel();
        stack.Children.Add(gauge);
        stack.Children.Add(label);

        var border = new Border
        {
            Width = 148,
            Height = 128,
            Padding = new Thickness(8),
            Margin = new Thickness(5),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(
                Color.FromArgb(70, 255, 255, 255)),
            Background = new SolidColorBrush(
                Color.FromArgb(95, 30, 40, 52)),
            Child = stack,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = definition.Group
        };

        border.MouseLeftButtonUp += (_, _) =>
            SelectStyle(definition.Style);

        _styleCards[definition.Style] = border;
        return border;
    }

    private void SelectStyle(GaugeVisualStyle style)
    {
        _selectedStyle = style;

        foreach (var pair in _styleCards)
        {
            pair.Value.BorderBrush = pair.Key == style
                ? ThemeSettingsService.Brush("#2F8CF0", "#2F8CF0")
                : new SolidColorBrush(
                    Color.FromArgb(70, 255, 255, 255));

            pair.Value.BorderThickness = pair.Key == style
                ? new Thickness(3)
                : new Thickness(1);
        }

        _styleStatus.Text =
            $"انتخاب فعلی: {GaugeStyleCatalog.DisplayName(style)}";
    }

    private void LoadSelectedStyleForTarget()
    {
        var target = SelectedTargetId();

        var style = target == "all"
            ? GaugeStyleCatalog.Parse(_gaugeStyles.DefaultStyle)
            : _gaugeStyles.Resolve(target);

        SelectStyle(style);
    }

    private string SelectedTargetId()
        => _gaugeTarget.SelectedItem is GaugeTarget target
            ? target.Id
            : "all";

    private void ApplyStyleToTarget()
    {
        var target = SelectedTargetId();

        if (target == "all")
        {
            ApplyStyleToAll();
            return;
        }

        _gaugeStyles.Overrides[target] = _selectedStyle.ToString();
        _styleStatus.Text =
            $"{GaugeStyleCatalog.DisplayName(_selectedStyle)} برای {target.ToUpperInvariant()} ذخیره شد";
    }

    private void ApplyStyleToAll()
    {
        _gaugeStyles.DefaultStyle = _selectedStyle.ToString();
        _gaugeStyles.Overrides.Clear();

        _styleStatus.Text =
            $"{GaugeStyleCatalog.DisplayName(_selectedStyle)} برای همه گیج‌ها تنظیم شد";
    }

    private void AddColorField(
        Panel panel,
        string key,
        string title,
        string value)
    {
        panel.Children.Add(Label(title));

        var row = new Grid
        {
            Margin = new Thickness(0, 0, 0, 10)
        };

        row.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
        row.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(52) });

        var box = new TextBox
        {
            Text = value,
            Height = 32,
            Padding = new Thickness(7, 4, 7, 4),
            Margin = new Thickness(0, 0, 8, 0),
            FlowDirection = FlowDirection.LeftToRight,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var swatch = new Button
        {
            Width = 42,
            Height = 32,
            BorderBrush = new SolidColorBrush(
                Color.FromArgb(100, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Background = ThemeSettingsService.Brush(
                value,
                "#FFFFFF"),
            ToolTip = "باز کردن Color Wheel"
        };

        swatch.Click += (_, _) =>
        {
            var picker = new ColorPickerWindow(box.Text)
            {
                Owner = this
            };

            if (picker.ShowDialog() != true)
                return;

            box.Text = picker.SelectedHex;
            UpdateSwatch(key);
            _preset.SelectedItem = null;
        };

        box.TextChanged += (_, _) =>
        {
            UpdateSwatch(key);

            if (IsLoaded)
                _preset.SelectedItem = null;
        };

        _fields[key] = box;
        _swatches[key] = swatch;

        Grid.SetColumn(box, 0);
        Grid.SetColumn(swatch, 1);

        row.Children.Add(box);
        row.Children.Add(swatch);
        panel.Children.Add(row);
    }

    private void UpdateSwatch(string key)
    {
        if (!_fields.TryGetValue(key, out var box)
            || !_swatches.TryGetValue(key, out var swatch))
        {
            return;
        }

        swatch.Background = ThemeSettingsService.Brush(
            box.Text,
            "#FFFFFF");
    }

    private void LoadIntoFields(ThemeSettings s)
    {
        Set("CardSurface", s.CardSurface);
        Set("CardBorder", s.CardBorder);
        Set("Accent", s.Accent);
        Set("ToolbarSurface", s.ToolbarSurface);
        Set("GaugeTrack", s.GaugeTrack);
        Set("GaugeNormal", s.GaugeNormal);
        Set("GaugeWarning", s.GaugeWarning);
        Set("GaugeCritical", s.GaugeCritical);
        Set("GaugeMarker", s.GaugeMarker);
        Set("GaugeValue", s.GaugeValue);
        Set("GaugeScale", s.GaugeScale);
        Set("GaugeLabel", s.GaugeLabel);
    }

    private void Set(string key, string value)
    {
        if (!_fields.TryGetValue(key, out var box))
            return;

        box.Text = value;
        UpdateSwatch(key);
    }

    private ThemeSettings ReadSettings()
        => new(
            _preset.SelectedItem as string ?? "Custom",
            Value("CardSurface"),
            Value("CardBorder"),
            Value("Accent"),
            Value("ToolbarSurface"),
            Value("GaugeTrack"),
            Value("GaugeNormal"),
            Value("GaugeWarning"),
            Value("GaugeCritical"),
            Value("GaugeMarker"),
            Value("GaugeValue"),
            Value("GaugeScale"),
            Value("GaugeLabel"));

    private string Value(string key)
        => _fields[key].Text.Trim();

    private static TextBlock Label(string text)
        => new()
        {
            Text = text,
            Foreground = Brushes.Gainsboro,
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 4),
            FlowDirection = FlowDirection.RightToLeft
        };

    private static TextBlock Section(string text)
        => new()
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 7),
            FlowDirection = FlowDirection.RightToLeft
        };

    private static Button Button(
        string text,
        string background)
        => new()
        {
            Content = text,
            Foreground = Brushes.White,
            Background = ThemeSettingsService.Brush(
                background,
                "#394655"),
            BorderBrush = new SolidColorBrush(
                Color.FromArgb(70, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 8, 14, 8),
            Margin = new Thickness(5, 0, 5, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
}
