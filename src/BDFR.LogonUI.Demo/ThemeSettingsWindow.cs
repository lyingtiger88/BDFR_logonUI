using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class ThemeSettingsWindow : Window
{
    private readonly ComboBox _preset = new();
    private readonly Dictionary<string, TextBox> _fields = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _swatches = new(StringComparer.Ordinal);

    public ThemeSettings ResultSettings { get; private set; }

    public ThemeSettingsWindow(ThemeSettings current)
    {
        ResultSettings = current;

        Title = "BDFR LogonUI — Theme & Gauge";
        Width = 610;
        Height = 760;
        MinWidth = 540;
        MinHeight = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(20, 27, 38));
        Foreground = Brushes.White;

        var root = new Grid { Margin = new Thickness(20) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "سفارشی‌سازی تم و رنگ گیج",
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

        var panel = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
        scroll.Content = panel;

        panel.Children.Add(Label("پریست"));
        _preset.ItemsSource = new[] { "Fluent", "Emerald", "Sunset", "Cyber", "Monochrome", "Light Gauge" };
        _preset.SelectedItem = current.Preset;
        _preset.Margin = new Thickness(0, 0, 0, 14);
        _preset.SelectionChanged += (_, _) =>
        {
            if (_preset.SelectedItem is string name)
                LoadIntoFields(ThemeSettingsService.Preset(name));
        };
        panel.Children.Add(_preset);

        panel.Children.Add(Section("رنگ‌های رابط"));
        AddColorField(panel, "CardSurface", "رنگ شیشه / کارت", current.CardSurface);
        AddColorField(panel, "CardBorder", "حاشیه کارت", current.CardBorder);
        AddColorField(panel, "Accent", "Accent", current.Accent);
        AddColorField(panel, "ToolbarSurface", "نوار ابزار", current.ToolbarSurface);

        panel.Children.Add(Section("رنگ‌های گیج"));
        AddColorField(panel, "GaugeTrack", "مسیر خالی", current.GaugeTrack);
        AddColorField(panel, "GaugeNormal", "Normal", current.GaugeNormal);
        AddColorField(panel, "GaugeWarning", "Warning", current.GaugeWarning);
        AddColorField(panel, "GaugeCritical", "Critical", current.GaugeCritical);
        AddColorField(panel, "GaugeMarker", "Marker", current.GaugeMarker);
        AddColorField(panel, "GaugeValue", "عدد وسط", current.GaugeValue);
        AddColorField(panel, "GaugeScale", "اعداد Scale", current.GaugeScale);
        AddColorField(panel, "GaugeLabel", "Label", current.GaugeLabel);

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
            DialogResult = true;
        };

        var cancel = Button("لغو", "#394655");
        cancel.Click += (_, _) => DialogResult = false;

        var reset = Button("Fluent پیش‌فرض", "#394655");
        reset.Click += (_, _) =>
        {
            _preset.SelectedItem = "Fluent";
            LoadIntoFields(ThemeSettingsService.Preset("Fluent"));
        };

        buttons.Children.Add(apply);
        buttons.Children.Add(cancel);
        buttons.Children.Add(reset);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);

        Content = root;
    }

    private void AddColorField(Panel panel, string key, string title, string value)
    {
        panel.Children.Add(Label(title));

        var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });

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
            BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Background = ThemeSettingsService.Brush(value, "#FFFFFF"),
            ToolTip = "باز کردن Color Wheel"
        };

        swatch.Click += (_, _) =>
        {
            var picker = new ColorPickerWindow(box.Text) { Owner = this };
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
        if (!_fields.TryGetValue(key, out var box) || !_swatches.TryGetValue(key, out var swatch))
            return;

        swatch.Background = ThemeSettingsService.Brush(box.Text, "#FFFFFF");
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

    private ThemeSettings ReadSettings() => new(
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

    private string Value(string key) => _fields[key].Text.Trim();

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        Foreground = Brushes.Gainsboro,
        FontSize = 12,
        Margin = new Thickness(0, 4, 0, 4),
        FlowDirection = FlowDirection.RightToLeft
    };

    private static TextBlock Section(string text) => new()
    {
        Text = text,
        Foreground = Brushes.White,
        FontSize = 17,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 12, 0, 7),
        FlowDirection = FlowDirection.RightToLeft
    };

    private static Button Button(string text, string background) => new()
    {
        Content = text,
        Foreground = Brushes.White,
        Background = ThemeSettingsService.Brush(background, "#394655"),
        BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
        Padding = new Thickness(14, 8, 14, 8),
        Margin = new Thickness(5, 0, 5, 0),
        Cursor = System.Windows.Input.Cursors.Hand
    };
}
