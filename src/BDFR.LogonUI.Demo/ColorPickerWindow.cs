using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class ColorPickerWindow : Window
{
    private readonly ColorWheelControl _wheel = new();
    private readonly Border _preview = new();
    private readonly TextBox _hex = new();
    private readonly TextBox _red = new();
    private readonly TextBox _green = new();
    private readonly TextBox _blue = new();
    private readonly TextBox _alpha = new();
    private bool _updatingFields;
    private byte _alphaValue = 255;

    public string SelectedHex { get; private set; }

    public ColorPickerWindow(string initialHex)
    {
        var initialColor = ParseColor(initialHex, Colors.White);
        _alphaValue = initialColor.A;
        SelectedHex = ToHex(initialColor);

        Title = "BDFR Color Picker";
        Width = 540;
        Height = 620;
        MinWidth = 500;
        MinHeight = 570;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(18, 23, 31));
        Foreground = Brushes.White;
        ResizeMode = ResizeMode.CanResize;

        var root = new Grid { Margin = new Thickness(20) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new TextBlock
        {
            Text = "انتخاب رنگ",
            FontSize = 21,
            FontWeight = FontWeights.SemiBold,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(heading, 0);
        root.Children.Add(heading);

        _wheel.HorizontalAlignment = HorizontalAlignment.Center;
        _wheel.VerticalAlignment = VerticalAlignment.Center;
        _wheel.Margin = new Thickness(0, 4, 0, 10);
        _wheel.SetColor(initialColor);
        _wheel.SelectedColorChanged += (_, _) => SyncFromWheel();
        Grid.SetRow(_wheel, 1);
        root.Children.Add(_wheel);

        var fields = new Grid { Margin = new Thickness(4, 4, 4, 12) };
        fields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
        fields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        fields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        fields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        fields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        fields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });

        _preview.Width = 58;
        _preview.Height = 36;
        _preview.CornerRadius = new CornerRadius(8);
        _preview.BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
        _preview.BorderThickness = new Thickness(1);
        _preview.Margin = new Thickness(0, 0, 10, 0);

        Grid.SetColumn(_preview, 0);
        fields.Children.Add(_preview);

        ConfigureBox(_hex, 0);
        _hex.Margin = new Thickness(0, 0, 10, 0);
        Grid.SetColumn(_hex, 1);
        fields.Children.Add(_hex);

        ConfigureBox(_red, 3);
        ConfigureBox(_green, 3);
        ConfigureBox(_blue, 3);
        ConfigureBox(_alpha, 3);

        Grid.SetColumn(_red, 2);
        Grid.SetColumn(_green, 3);
        Grid.SetColumn(_blue, 4);
        Grid.SetColumn(_alpha, 5);
        fields.Children.Add(_red);
        fields.Children.Add(_green);
        fields.Children.Add(_blue);
        fields.Children.Add(_alpha);

        AddTinyLabel(fields, "R", 2);
        AddTinyLabel(fields, "G", 3);
        AddTinyLabel(fields, "B", 4);
        AddTinyLabel(fields, "A", 5);

        Grid.SetRow(fields, 2);
        root.Children.Add(fields);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var ok = MakeButton("تأیید", "#2F8CF0");
        ok.Click += (_, _) =>
        {
            CommitFields();
            SelectedHex = ToHex(CurrentColor());
            DialogResult = true;
        };

        var cancel = MakeButton("لغو", "#38424F");
        cancel.Click += (_, _) => DialogResult = false;

        actions.Children.Add(ok);
        actions.Children.Add(cancel);
        Grid.SetRow(actions, 3);
        root.Children.Add(actions);

        Content = root;
        SyncFromWheel();
    }

    private void ConfigureBox(TextBox box, int maxLength)
    {
        box.Height = 32;
        box.Padding = new Thickness(6, 4, 6, 4);
        box.Margin = new Thickness(3, 0, 3, 0);
        box.FlowDirection = FlowDirection.LeftToRight;
        box.VerticalContentAlignment = VerticalAlignment.Center;

        if (maxLength > 0)
            box.MaxLength = maxLength;

        box.LostKeyboardFocus += (_, _) => CommitFields();
        box.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;

            CommitFields();
            e.Handled = true;
        };
    }

    private void CommitFields()
    {
        if (_updatingFields)
            return;

        if (TryParseColor(_hex.Text, out var hexColor))
        {
            _alphaValue = hexColor.A;
            _wheel.SetColor(hexColor);
            return;
        }

        if (byte.TryParse(_red.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r)
            && byte.TryParse(_green.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var g)
            && byte.TryParse(_blue.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        {
            if (byte.TryParse(_alpha.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var a))
                _alphaValue = a;

            _wheel.SetColor(Color.FromRgb(r, g, b));
        }
    }

    private void SyncFromWheel()
    {
        _updatingFields = true;

        try
        {
            var rgb = _wheel.SelectedColor;
            var color = Color.FromArgb(_alphaValue, rgb.R, rgb.G, rgb.B);
            SelectedHex = ToHex(color);
            _hex.Text = SelectedHex;
            _red.Text = color.R.ToString(CultureInfo.InvariantCulture);
            _green.Text = color.G.ToString(CultureInfo.InvariantCulture);
            _blue.Text = color.B.ToString(CultureInfo.InvariantCulture);
            _alpha.Text = color.A.ToString(CultureInfo.InvariantCulture);
            _preview.Background = new SolidColorBrush(color);
        }
        finally
        {
            _updatingFields = false;
        }
    }

    private Color CurrentColor()
    {
        var rgb = _wheel.SelectedColor;
        return Color.FromArgb(_alphaValue, rgb.R, rgb.G, rgb.B);
    }

    private static void AddTinyLabel(Grid grid, string text, int column)
    {
        var label = new TextBlock
        {
            Text = text,
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(150, 163, 178)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -14, 0, 0)
        };

        Grid.SetColumn(label, column);
        grid.Children.Add(label);
    }

    public static bool TryParseColor(string value, out Color color)
    {
        try
        {
            color = (Color)ColorConverter.ConvertFromString(value.Trim());
            return true;
        }
        catch
        {
            color = Colors.White;
            return false;
        }
    }

    public static Color ParseColor(string value, Color fallback) =>
        TryParseColor(value, out var color) ? color : fallback;

    public static string ToHex(Color color) =>
        color.A == 255
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Button MakeButton(string text, string background) => new()
    {
        Content = text,
        Foreground = Brushes.White,
        Background = ThemeSettingsService.Brush(background, "#38424F"),
        BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
        Padding = new Thickness(16, 8, 16, 8),
        Margin = new Thickness(5, 0, 0, 0),
        Cursor = Cursors.Hand
    };
}
