using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace BDFR.LogonUI.Demo;

public sealed class LayoutPersistenceService
{
    private readonly string _path;

    public LayoutPersistenceService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BDFR", "LogonUI");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "layout.demo.json");
    }

    public void Save(IEnumerable<EditableWidgetHost> widgets)
    {
        var model = widgets.Select(w => new LayoutItem(
            w.WidgetId,
            Safe(Canvas.GetLeft(w)),
            Safe(Canvas.GetTop(w)),
            w.ActualWidth > 0 ? w.ActualWidth : w.Width,
            w.ActualHeight > 0 ? w.ActualHeight : w.Height)).ToArray();

        File.WriteAllText(_path, JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true }));
    }

    public bool TryLoad(IEnumerable<EditableWidgetHost> widgets)
    {
        if (!File.Exists(_path))
            return false;

        try
        {
            var items = JsonSerializer.Deserialize<LayoutItem[]>(File.ReadAllText(_path));
            if (items is null)
                return false;

            var byId = widgets.ToDictionary(w => w.WidgetId, StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (!byId.TryGetValue(item.Id, out var widget))
                    continue;

                Canvas.SetLeft(widget, Math.Max(0, item.X));
                Canvas.SetTop(widget, Math.Max(0, item.Y));
                widget.Width = Math.Max(widget.MinWidth, item.Width);
                widget.Height = Math.Max(widget.MinHeight, item.Height);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void DeleteSavedLayout()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private static double Safe(double value) => double.IsNaN(value) ? 0 : value;

    private sealed record LayoutItem(string Id, double X, double Y, double Width, double Height);
}
