using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed record WidgetManagerItem(
    string Id,
    string Title,
    string Description,
    EditableWidgetHost Host);

public sealed class WidgetManagerWindow : Window
{
    private readonly IReadOnlyList<WidgetManagerItem> _items;

    public bool Changed { get; private set; }

    public WidgetManagerWindow(IReadOnlyList<WidgetManagerItem> items)
    {
        _items = items;

        Title = "BDFR LogonUI — مدیریت ویجت‌ها";
        Width = 760;
        Height = 640;
        MinWidth = 620;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(19, 26, 37));
        Foreground = Brushes.White;

        var root = new Grid { Margin = new Thickness(20) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft
        };

        header.Children.Add(new TextBlock
        {
            Text = "مدیریت ویجت‌ها",
            FontSize = 25,
            FontWeight = FontWeights.SemiBold
        });

        header.Children.Add(new TextBlock
        {
            Text = "ویجت‌ها را از صفحه حذف یا دوباره اضافه کن. حذف در این بخش دائمی نیست و هر زمان قابل بازگشت است.",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(177, 193, 210)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 14)
        });

        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var stack = new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft
        };

        foreach (var item in _items)
            stack.Children.Add(CreateRow(item));

        scroll.Content = stack;
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

        var showAll = MakeButton("نمایش همه", "#394655");
        showAll.HorizontalAlignment = HorizontalAlignment.Right;
        showAll.Click += (_, _) =>
        {
            foreach (var item in _items)
                item.Host.IsWidgetEnabled = true;

            Changed = true;
            RebuildRows(stack);
        };

        var done = MakeButton("تمام", "#2F8CF0");
        done.Click += (_, _) => DialogResult = true;

        Grid.SetColumn(showAll, 0);
        Grid.SetColumn(done, 1);
        footer.Children.Add(showAll);
        footer.Children.Add(done);

        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        Content = root;
    }

    private Border CreateRow(WidgetManagerItem item)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });
        grid.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = GridLength.Auto
        });

        var info = new StackPanel();

        info.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            FlowDirection = FlowDirection.RightToLeft
        });

        info.Children.Add(new TextBlock
        {
            Text = item.Description,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(168, 187, 205)),
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            FlowDirection = FlowDirection.RightToLeft
        });

        var toggle = MakeButton(
            item.Host.IsWidgetEnabled ? "حذف از صفحه" : "افزودن به صفحه",
            item.Host.IsWidgetEnabled ? "#66404A" : "#2F8CF0");

        toggle.MinWidth = 120;
        toggle.Click += (_, _) =>
        {
            item.Host.IsWidgetEnabled = !item.Host.IsWidgetEnabled;
            Changed = true;

            toggle.Content = item.Host.IsWidgetEnabled
                ? "حذف از صفحه"
                : "افزودن به صفحه";

            toggle.Background = ThemeSettingsService.Brush(
                item.Host.IsWidgetEnabled ? "#66404A" : "#2F8CF0",
                "#394655");
        };

        Grid.SetColumn(info, 0);
        Grid.SetColumn(toggle, 1);
        grid.Children.Add(info);
        grid.Children.Add(toggle);

        return new Border
        {
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 8),
            CornerRadius = new CornerRadius(13),
            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(85, 28, 39, 52)),
            Child = grid
        };
    }

    private void RebuildRows(Panel panel)
    {
        panel.Children.Clear();

        foreach (var item in _items)
            panel.Children.Add(CreateRow(item));
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
            Margin = new Thickness(6, 0, 0, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
}
