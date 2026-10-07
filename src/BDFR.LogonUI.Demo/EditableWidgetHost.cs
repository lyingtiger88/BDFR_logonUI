using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class EditableWidgetHost : ContentControl
{
    private Thumb? _resizeThumb;
    private Point _dragOrigin;
    private double _originLeft;
    private double _originTop;
    private bool _dragging;

    static EditableWidgetHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(EditableWidgetHost),
            new FrameworkPropertyMetadata(typeof(EditableWidgetHost)));
    }

    public string WidgetId { get; set; } = Guid.NewGuid().ToString("N");

    public static readonly DependencyProperty IsEditModeProperty =
        DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(EditableWidgetHost),
            new PropertyMetadata(false));

    public bool IsEditMode
    {
        get => (bool)GetValue(IsEditModeProperty);
        set => SetValue(IsEditModeProperty, value);
    }

    public static readonly DependencyProperty IsLayoutLockedProperty =
        DependencyProperty.Register(nameof(IsLayoutLocked), typeof(bool), typeof(EditableWidgetHost),
            new PropertyMetadata(false));

    public bool IsLayoutLocked
    {
        get => (bool)GetValue(IsLayoutLockedProperty);
        set => SetValue(IsLayoutLockedProperty, value);
    }

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(EditableWidgetHost),
            new PropertyMetadata(false));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public event EventHandler? LayoutChanged;

    public override void OnApplyTemplate()
    {
        if (_resizeThumb is not null)
            _resizeThumb.DragDelta -= ResizeThumbOnDragDelta;

        base.OnApplyTemplate();

        _resizeThumb = GetTemplateChild("PART_ResizeThumb") as Thumb;
        if (_resizeThumb is not null)
            _resizeThumb.DragDelta += ResizeThumbOnDragDelta;
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);

        if (!IsEditMode || IsLayoutLocked || FindAncestor<Thumb>(e.OriginalSource as DependencyObject) is not null)
            return;

        if (Parent is not Canvas canvas)
            return;

        IsSelected = true;
        Panel.SetZIndex(this, 100);

        _dragging = true;
        _dragOrigin = e.GetPosition(canvas);
        _originLeft = SafeCanvasValue(Canvas.GetLeft(this));
        _originTop = SafeCanvasValue(Canvas.GetTop(this));
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);

        if (!_dragging || e.LeftButton != MouseButtonState.Pressed || Parent is not Canvas canvas)
            return;

        var current = e.GetPosition(canvas);
        var left = _originLeft + current.X - _dragOrigin.X;
        var top = _originTop + current.Y - _dragOrigin.Y;

        left = Math.Clamp(left, 0, Math.Max(0, canvas.ActualWidth - ActualWidth));
        top = Math.Clamp(top, 0, Math.Max(0, canvas.ActualHeight - ActualHeight));

        Canvas.SetLeft(this, Snap(left));
        Canvas.SetTop(this, Snap(top));
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);

        if (!_dragging)
            return;

        _dragging = false;
        ReleaseMouseCapture();
        Panel.SetZIndex(this, 1);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void ResizeThumbOnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!IsEditMode || IsLayoutLocked)
            return;

        var maxWidth = Parent is Canvas canvas ? Math.Max(MinWidth, canvas.ActualWidth - SafeCanvasValue(Canvas.GetLeft(this))) : double.PositiveInfinity;
        var maxHeight = Parent is Canvas canvas2 ? Math.Max(MinHeight, canvas2.ActualHeight - SafeCanvasValue(Canvas.GetTop(this))) : double.PositiveInfinity;

        Width = Snap(Math.Clamp(ActualWidth + e.HorizontalChange, Math.Max(140, MinWidth), maxWidth));
        Height = Snap(Math.Clamp(ActualHeight + e.VerticalChange, Math.Max(90, MinHeight), maxHeight));
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private static double Snap(double value)
    {
        const double grid = 8.0;
        return Math.Round(value / grid) * grid;
    }

    private static double SafeCanvasValue(double value) => double.IsNaN(value) ? 0 : value;

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T typed)
                return typed;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
