using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class WallpaperGalleryWindow : Window
{
    private readonly SeasonalBackgroundService _backgrounds;
    private readonly WrapPanel _panel = new();
    private readonly TextBlock _summary = new();
    private readonly TextBlock _selection = new();
    private string? _selectedPath;

    public bool WallpaperChanged { get; private set; }

    public WallpaperGalleryWindow(SeasonalBackgroundService backgrounds)
    {
        _backgrounds = backgrounds;
        _selectedPath = backgrounds.ActivePath;

        Title = "BDFR LogonUI — Wallpaper Gallery";
        Width = 980;
        Height = 720;
        MinWidth = 760;
        MinHeight = 540;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(15, 21, 30));
        Foreground = Brushes.White;

        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new TextBlock
        {
            Text = "انتخاب پس‌زمینه",
            FontSize = 23,
            FontWeight = FontWeights.SemiBold,
            FlowDirection = FlowDirection.RightToLeft
        };
        Grid.SetRow(heading, 0);
        root.Children.Add(heading);

        var info = new StackPanel
        {
            Margin = new Thickness(0, 8, 0, 14),
            FlowDirection = FlowDirection.RightToLeft
        };

        info.Children.Add(new TextBlock
        {
            Text = "این صفحه فقط تصاویر موجود در wallpaper برنامه و Pictures\\LockScreen را نمایش می‌دهد.",
            Foreground = new SolidColorBrush(Color.FromRgb(185, 200, 216)),
            TextWrapping = TextWrapping.Wrap
        });

        info.Children.Add(new TextBlock
        {
            Text = $"پوشه سفارشی: {_backgrounds.UserWallpaperFolder}",
            Foreground = new SolidColorBrush(Color.FromRgb(125, 150, 174)),
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        _summary.Foreground = new SolidColorBrush(Color.FromRgb(132, 177, 216));
        _summary.Margin = new Thickness(0, 5, 0, 0);
        info.Children.Add(_summary);

        Grid.SetRow(info, 1);
        root.Children.Add(info);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        _panel.Orientation = Orientation.Horizontal;
        scroll.Content = _panel;

        Grid.SetRow(scroll, 2);
        root.Children.Add(scroll);

        var footer = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _selection.Text = "یک تصویر را انتخاب کنید";
        _selection.Foreground = new SolidColorBrush(Color.FromRgb(180, 196, 212));
        _selection.VerticalAlignment = VerticalAlignment.Center;
        _selection.FlowDirection = FlowDirection.RightToLeft;
        footer.Children.Add(_selection);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        var refresh = MakeButton("بازخوانی", "#384657");
        refresh.Click += (_, _) => Reload();

        var close = MakeButton("بستن", "#384657");
        close.Click += (_, _) => Close();

        actions.Children.Add(refresh);
        actions.Children.Add(close);
        Grid.SetColumn(actions, 1);
        footer.Children.Add(actions);

        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        Content = root;
        Reload();
    }

    private void Reload()
    {
        _panel.Children.Clear();

        var items = _backgrounds.GetAvailableWallpapers();
        _summary.Text = $"{items.Count} تصویر قابل انتخاب";

        if (items.Count == 0)
        {
            _panel.Children.Add(new Border
            {
                Width = 520,
                Padding = new Thickness(18),
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(80, 60, 72, 88)),
                Child = new TextBlock
                {
                    Text = "تصویری پیدا نشد. فایل‌ها را خارج از Lock Screen در wallpaper برنامه یا Pictures\\LockScreen قرار دهید.",
                    TextWrapping = TextWrapping.Wrap,
                    FlowDirection = FlowDirection.RightToLeft,
                    Foreground = new SolidColorBrush(Color.FromRgb(195, 207, 220))
                }
            });

            return;
        }

        foreach (var item in items)
        {
            var thumb = _backgrounds.LoadThumbnail(item.Path, 330);
            if (thumb is null)
                continue;

            var image = new Image
            {
                Source = thumb,
                Width = 250,
                Height = 142,
                Stretch = Stretch.UniformToFill
            };

            var overlay = new Border
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(Color.FromArgb(180, 10, 15, 22)),
                Padding = new Thickness(9, 6, 9, 6),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = item.DisplayName,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            FontWeight = FontWeights.SemiBold
                        },
                        new TextBlock
                        {
                            Text = item.SourceLabel,
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(160, 180, 199))
                        }
                    }
                }
            };

            var cardGrid = new Grid();
            cardGrid.Children.Add(image);
            cardGrid.Children.Add(overlay);

            var card = new Border
            {
                Width = 252,
                Height = 144,
                Margin = new Thickness(7),
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
                BorderThickness = new Thickness(
                    string.Equals(_selectedPath, item.Path, StringComparison.OrdinalIgnoreCase)
                        ? 3
                        : 1),
                BorderBrush = string.Equals(_selectedPath, item.Path, StringComparison.OrdinalIgnoreCase)
                    ? ThemeSettingsService.Brush("#2F8CF0", "#2F8CF0")
                    : new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                Child = cardGrid,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = item.Path
            };

            var captured = item;
            card.MouseLeftButtonUp += (_, _) => Select(captured);
            _panel.Children.Add(card);
        }
    }

    private void Select(WallpaperSourceItem item)
    {
        try
        {
            _backgrounds.SelectWallpaper(item.Path);
            _selectedPath = item.Path;
            WallpaperChanged = true;
            _selection.Text = $"انتخاب شد: {item.DisplayName}";
            Reload();
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

    private static Button MakeButton(string text, string background) => new()
    {
        Content = text,
        Foreground = Brushes.White,
        Background = ThemeSettingsService.Brush(background, "#384657"),
        BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(14, 8, 14, 8),
        Margin = new Thickness(5, 0, 0, 0),
        Cursor = System.Windows.Input.Cursors.Hand
    };
}
