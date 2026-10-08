using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BDFR.LogonUI.Demo;

public sealed class WelcomeAnimationGalleryWindow : Window
{
    private readonly Dictionary<WelcomeAnimationStyle, Border> _cards = new();
    private readonly WelcomeAnimationOverlay _preview = new();
    private readonly CheckBox _enabled = new();
    private readonly Slider _duration = new();
    private readonly TextBlock _status = new();

    private WelcomeAnimationStyle _selected;
    private readonly WelcomeAnimationSettings _settings;

    public WelcomeAnimationSettings ResultSettings => _settings.Clone();

    public WelcomeAnimationGalleryWindow(WelcomeAnimationSettings current)
    {
        _settings = current.Clone();
        _selected = _settings.Resolve();

        Title = "BDFR LogonUI — گالری خوش‌آمدگویی";
        Width = 980;
        Height = 760;
        MinWidth = 780;
        MinHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(19, 26, 37));
        Foreground = Brushes.White;

        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft
        };

        header.Children.Add(new TextBlock
        {
            Text = "گالری انیمیشن خوش‌آمدگویی",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold
        });

        header.Children.Add(new TextBlock
        {
            Text = "۹ مدل قابل انتخاب؛ روی هر کارت کلیک کن و همان‌جا پیش‌نمایش بگیر.",
            Foreground = new SolidColorBrush(Color.FromRgb(177, 193, 210)),
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 12)
        });

        _enabled.Content = "نمایش بعد از ورود موفق";
        _enabled.IsChecked = _settings.Enabled;
        _enabled.Foreground = Brushes.Gainsboro;
        _enabled.Margin = new Thickness(0, 0, 0, 8);
        header.Children.Add(_enabled);

        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var wrap = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.LeftToRight
        };

        foreach (var definition in WelcomeAnimationCatalog.All)
            wrap.Children.Add(CreateCard(definition));

        scroll.Content = wrap;
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var footer = new Grid
        {
            Margin = new Thickness(0, 14, 0, 0)
        };

        footer.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });
        footer.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = GridLength.Auto
        });

        var left = new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft
        };

        _status.Foreground = new SolidColorBrush(Color.FromRgb(129, 190, 239));
        _status.FontSize = 11;
        _status.Margin = new Thickness(0, 0, 0, 8);
        left.Children.Add(_status);

        var durationRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.RightToLeft
        };

        durationRow.Children.Add(new TextBlock
        {
            Text = "مدت:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        });

        _duration.Minimum = 1.5;
        _duration.Maximum = 8;
        _duration.Value = Math.Clamp(_settings.DurationSeconds, 1.5, 8);
        _duration.TickFrequency = .5;
        _duration.IsSnapToTickEnabled = true;
        _duration.Width = 220;
        durationRow.Children.Add(_duration);

        var durationText = new TextBlock
        {
            Width = 55,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = FlowDirection.LeftToRight,
            Foreground = new SolidColorBrush(Color.FromRgb(145, 190, 230))
        };

        void UpdateDuration() => durationText.Text = $"{_duration.Value:0.0}s";
        _duration.ValueChanged += (_, _) => UpdateDuration();
        UpdateDuration();
        durationRow.Children.Add(durationText);

        left.Children.Add(durationRow);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var preview = MakeButton("پیش‌نمایش", "#394655");
        preview.Click += async (_, _) =>
        {
            await _preview.PlayAsync(
                _selected,
                _duration.Value,
                "به BDFR خوش آمدید",
                WelcomeAnimationCatalog.Definition(_selected).PersianName);
        };

        var save = MakeButton("انتخاب و ذخیره", "#2F8CF0");
        save.Click += (_, _) =>
        {
            _settings.Style = _selected.ToString();
            _settings.Enabled = _enabled.IsChecked == true;
            _settings.DurationSeconds = _duration.Value;
            DialogResult = true;
        };

        var cancel = MakeButton("لغو", "#394655");
        cancel.Click += (_, _) => DialogResult = false;

        buttons.Children.Add(preview);
        buttons.Children.Add(save);
        buttons.Children.Add(cancel);

        Grid.SetColumn(left, 0);
        Grid.SetColumn(buttons, 1);
        footer.Children.Add(left);
        footer.Children.Add(buttons);

        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        _preview.SetValue(Grid.RowSpanProperty, 3);
        Panel.SetZIndex(_preview, 1000);
        root.Children.Add(_preview);

        Content = root;
        Select(_selected);
    }

    private Border CreateCard(WelcomeAnimationDefinition definition)
    {
        var previewSurface = new Grid
        {
            Width = 250,
            Height = 126,
            Background = PreviewBrush(definition.Style),
            ClipToBounds = true
        };

        previewSurface.Children.Add(new Ellipse
        {
            Width = definition.Style == WelcomeAnimationStyle.MinimalCircle ? 76 : 34,
            Height = definition.Style == WelcomeAnimationStyle.MinimalCircle ? 76 : 34,
            Stroke = new SolidColorBrush(Color.FromArgb(165, 255, 255, 255)),
            StrokeThickness = 1.5,
            Fill = definition.Style == WelcomeAnimationStyle.ParticleBloom
                ? new SolidColorBrush(Color.FromArgb(70, 255, 190, 210))
                : Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        previewSurface.Children.Add(new TextBlock
        {
            Text = "به BDFR خوش آمدید",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 58, 0, 0),
            FlowDirection = FlowDirection.RightToLeft
        });

        var stack = new StackPanel();
        stack.Children.Add(previewSurface);
        stack.Children.Add(new TextBlock
        {
            Text = definition.PersianName,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 2),
            FlowDirection = FlowDirection.RightToLeft
        });
        stack.Children.Add(new TextBlock
        {
            Text = definition.DisplayName,
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(177, 193, 210)),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var card = new Border
        {
            Width = 276,
            Height = 186,
            Margin = new Thickness(7),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.FromArgb(105, 29, 39, 52)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Child = stack,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = definition.Description
        };

        card.MouseLeftButtonUp += async (_, _) =>
        {
            Select(definition.Style);
            await _preview.PlayAsync(
                definition.Style,
                _duration.Value,
                "به BDFR خوش آمدید",
                definition.PersianName);
        };

        _cards[definition.Style] = card;
        return card;
    }

    private void Select(WelcomeAnimationStyle style)
    {
        _selected = style;

        foreach (var pair in _cards)
        {
            pair.Value.BorderBrush = pair.Key == style
                ? ThemeSettingsService.Brush("#2F8CF0", "#2F8CF0")
                : new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));

            pair.Value.BorderThickness = pair.Key == style
                ? new Thickness(3)
                : new Thickness(1);
        }

        var definition = WelcomeAnimationCatalog.Definition(style);
        _status.Text = $"انتخاب فعلی: {definition.PersianName} — {definition.DisplayName}";
    }

    private static Brush PreviewBrush(WelcomeAnimationStyle style) =>
        style switch
        {
            WelcomeAnimationStyle.PersianSunrise => Gradient("#10274A", "#D77B4E"),
            WelcomeAnimationStyle.ElegantFade => Gradient("#061122", "#173E63"),
            WelcomeAnimationStyle.ParticleBloom => Gradient("#2B1426", "#0B0F19"),
            WelcomeAnimationStyle.AuroraFlow => Gradient("#08223B", "#0E6C70"),
            WelcomeAnimationStyle.GlassPanels => Gradient("#0C223B", "#1A4B76"),
            WelcomeAnimationStyle.TypographyWave => Gradient("#090A0F", "#4F351D"),
            WelcomeAnimationStyle.NatureSeasons => Gradient("#D180A4", "#47795D"),
            WelcomeAnimationStyle.MinimalCircle => Gradient("#050D1B", "#102D50"),
            WelcomeAnimationStyle.CityToDesktop => Gradient("#102B4C", "#4E2A3B"),
            _ => Gradient("#0B1727", "#243A54")
        };

    private static Brush Gradient(string first, string second)
    {
        var a = (Color)ColorConverter.ConvertFromString(first);
        var b = (Color)ColorConverter.ConvertFromString(second);

        return new LinearGradientBrush(
            new GradientStopCollection
            {
                new(a, 0),
                new(b, 1)
            },
            new Point(0, 0),
            new Point(1, 1));
    }

    private static Button MakeButton(string text, string background) =>
        new()
        {
            Content = text,
            Foreground = Brushes.White,
            Background = ThemeSettingsService.Brush(background, "#394655"),
            BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 8, 14, 8),
            Margin = new Thickness(5, 0, 5, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
}
