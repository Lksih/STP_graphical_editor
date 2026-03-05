using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Reactive;

namespace STP_group_1.Views.Controls;

public partial class CanvasViewport : UserControl
{
    private ScrollViewer? _scroll;
    private bool _isPanning;
    private bool _spaceDown;
    private Point _panStartPointer;
    private Vector _panStartOffset;

    public CanvasViewport()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttached;
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _scroll = this.FindControl<ScrollViewer>("PART_Scroll");
        if (_scroll is null)
            return;

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);

        // Zoom (Ctrl + wheel)
        _scroll.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);

        // Pan (Space + drag)
        _scroll.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        _scroll.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        _scroll.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        // simple discrete steps: up => +10%, down => -10%
        var delta = e.Delta.Y;
        if (delta > 0)
            vm.ZoomInCommand.Execute(Unit.Default);
        else if (delta < 0)
            vm.ZoomOutCommand.Execute(Unit.Default);

        e.Handled = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_scroll is null)
            return;

        // Pan: Space + left button
        if (!e.KeyModifiers.HasFlag(KeyModifiers.None) && !e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            !e.KeyModifiers.HasFlag(KeyModifiers.Alt) && !e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // ignore modified presses except Space (handled by KeyModifiers? space isn't a modifier in Avalonia)
        }

        if (!_spaceDown)
            return;

        var point = e.GetCurrentPoint(_scroll);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        _isPanning = true;
        _panStartPointer = point.Position;
        _panStartOffset = _scroll.Offset;

        _scroll.Cursor = new Cursor(StandardCursorType.Hand);
        e.Pointer.Capture(_scroll);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_scroll is null || !_isPanning)
            return;

        var p = e.GetPosition(_scroll);
        var delta = p - _panStartPointer;

        _scroll.Offset = _panStartOffset - delta;
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_scroll is null || !_isPanning)
            return;

        _isPanning = false;
        _scroll.Cursor = Cursor.Default;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
            _spaceDown = true;
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
            _spaceDown = false;
    }
}