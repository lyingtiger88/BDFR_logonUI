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

    public static readonly DependencyProperty DesignWidthProperty =
        DependencyProperty.Register(nameof(DesignWidth), typeof(double), typeof(EditableWidgetHost),
            new PropertyMetadata(320d));

    public double DesignWidth
    {
        get => (double)GetValue(DesignWidthProperty);
        set => SetValue(DesignWidthProperty, value);
    }

    public static readonly DependencyProperty DesignHeightProperty =
        DependencyProperty.Register(nameof(DesignHeight), typeof(double), typeof(EditableWidgetHost),
            new PropertyMetadata(200d));

    public double DesignHeight
    {
        get => (double)GetValue(DesignHeightProperty);
        set => SetValue(DesignHeightProperty, value);
    }

    public static readonly DependencyProperty PreserveAspectRatioProperty =
        DependencyProperty.Register(nameof(PreserveAspectRatio), typeof(bool), typeof(EditableWidgetHost),
            new PropertyMetadata(true));

    public bool PreserveAspectRatio
    {
        get => (bool)GetValue(PreserveAspectRatioProperty);
        set => SetValue(PreserveAspectRatioProperty, value);
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

        var canvas = Parent as Canvas;
        var maxWidth = canvas is null
            ? double.PositiveInfinity
            : Math.Max(MinWidth, canvas.ActualWidth - SafeCanvasValue(Canvas.GetLeft(this)));

        var maxHeight = canvas is null
            ? double.PositiveInfinity
            : Math.Max(MinHeight, canvas.ActualHeight - SafeCanvasValue(Canvas.GetTop(this)));

        var minWidth = Math.Max(90, MinWidth);
        var minHeight = Math.Max(55, MinHeight);

        var proposedWidth = ActualWidth + e.HorizontalChange;
        var proposedHeight = ActualHeight + e.VerticalChange;

        var keepRatio = PreserveAspectRatio && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        if (keepRatio)
        {
            var ratio = DesignWidth > 0 && DesignHeight > 0
                ? DesignWidth / DesignHeight
                : Math.Max(.1, ActualWidth / Math.Max(1, ActualHeight));

            if (Math.Abs(e.HorizontalChange) >= Math.Abs(e.VerticalChange))
                proposedHeight = proposedWidth / ratio;
            else
                proposedWidth = proposedHeight * ratio;

            if (proposedWidth < minWidth)
            {
                proposedWidth = minWidth;
                proposedHeight = proposedWidth / ratio;
            }

            if (proposedHeight < minHeight)
            {
                proposedHeight = minHeight;
                proposedWidth = proposedHeight * ratio;
            }

            if (proposedWidth > maxWidth)
            {
                proposedWidth = maxWidth;
                proposedHeight = proposedWidth / ratio;
            }

            if (proposedHeight > maxHeight)
            {
                proposedHeight = maxHeight;
                proposedWidth = proposedHeight * ratio;
            }

            proposedWidth = Snap(proposedWidth);
            proposedHeight = proposedWidth / ratio;
        }
        else
        {
            proposedWidth = Snap(Math.Clamp(proposedWidth, minWidth, maxWidth));
            proposedHeight = Snap(Math.Clamp(proposedHeight, minHeight, maxHeight));
        }

        Width = Math.Max(minWidth, proposedWidth);
        Height = Math.Max(minHeight, proposedHeight);
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
