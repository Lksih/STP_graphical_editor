using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Reactive;
using System.Reactive.Linq;

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
        if (PART_Scroll is null)
            return;

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);

        PART_Scroll.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);

        PART_Scroll.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        PART_Scroll.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        PART_Scroll.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
    }

    private async void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var delta = e.Delta.Y;
        if (delta > 0)
            await vm.ZoomInCommand.Execute(Unit.Default);
        else if (delta < 0)
            await vm.ZoomOutCommand.Execute(Unit.Default);

        e.Handled = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_scroll is null)
            return;

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