using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Reactive;
using System.Reactive.Linq;
using System.ComponentModel;
using System;

namespace STP_group_1.Views.Controls;

public partial class CanvasViewport : UserControl
{
    private ScrollViewer? _scroll;

    private Border? _miniMapContainer;
    private GeometryCanvas? _miniMapCanvas;
    private Border? _miniMapViewport;
    private INotifyPropertyChanged? _vmNotify;

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

        _scroll = PART_Scroll;
        _miniMapContainer = PART_MiniMapContainer;
        _miniMapCanvas = PART_MiniMapCanvas;
        _miniMapViewport = PART_MiniMapViewport;

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);

        PART_Scroll.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);

        PART_Scroll.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        PART_Scroll.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        PART_Scroll.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);

        PART_Scroll.GetObservable(ScrollViewer.OffsetProperty)
            .Subscribe(Observer.Create<Vector>(_ => UpdateMiniMap()));
        PART_Scroll.GetObservable(ScrollViewer.ViewportProperty)
            .Subscribe(Observer.Create<Size>(_ => UpdateMiniMap()));

        if (_miniMapContainer is not null)
        {
            _miniMapContainer.GetObservable(BoundsProperty)
                .Subscribe(Observer.Create<Rect>(_ => UpdateMiniMap()));
        }

        if (_vmNotify is not null)
            _vmNotify.PropertyChanged -= OnVmPropertyChanged;

        _vmNotify = DataContext as INotifyPropertyChanged;
        if (_vmNotify is not null)
            _vmNotify.PropertyChanged += OnVmPropertyChanged;

        UpdateMiniMap();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.ZoomPercent) or
            nameof(ViewModels.MainWindowViewModel.ZoomPercentText) or
            nameof(ViewModels.MainWindowViewModel.CanvasWidth) or
            nameof(ViewModels.MainWindowViewModel.CanvasHeight) or
            nameof(ViewModels.MainWindowViewModel.CanvasBackground))
        {
            UpdateMiniMap();
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_scroll is null)
            return;

        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var oldZoom = vm.ZoomFactor;
        var pointerInViewport = e.GetPosition(_scroll);
        var oldOffset = _scroll.Offset;

        // Keep model point under cursor stable across zoom changes.
        var modelPointX = (oldOffset.X + pointerInViewport.X) / Math.Max(oldZoom, 0.000001);
        var modelPointY = (oldOffset.Y + pointerInViewport.Y) / Math.Max(oldZoom, 0.000001);

        var delta = e.Delta.Y;
        if (Math.Abs(delta) < 0.0001)
            return;

        // Exponential zoom by wheel delta: smooth for both wheel and touchpad.
        const double wheelStep = 1.10;
        var factor = Math.Pow(wheelStep, delta);
        var targetZoomPercent = vm.ZoomPercent * factor;
        vm.ZoomPercent = Math.Clamp(targetZoomPercent, 10.0, 800.0);

        var newZoom = vm.ZoomFactor;
        if (Math.Abs(newZoom - oldZoom) < 0.000001)
        {
            e.Handled = true;
            return;
        }

        var newOffset = new Vector(
            modelPointX * newZoom - pointerInViewport.X,
            modelPointY * newZoom - pointerInViewport.Y);
        _scroll.Offset = newOffset;
        UpdateMiniMap();


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

     private void UpdateMiniMap()
    {
        if (_scroll is null || _miniMapContainer is null || _miniMapCanvas is null || _miniMapViewport is null)
            return;

        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        var containerSize = _miniMapContainer.Bounds.Size;
        if (containerSize.Width <= 0 || containerSize.Height <= 0)
            return;

        const double pad = 8.0;
        var availW = Math.Max(0, containerSize.Width - pad * 2);
        var availH = Math.Max(0, containerSize.Height - pad * 2);

        var canvasW = Math.Max(1.0, vm.CanvasWidth);
        var canvasH = Math.Max(1.0, vm.CanvasHeight);

        var miniScale = Math.Min(availW / canvasW, availH / canvasH);
        miniScale = Math.Max(0.0001, miniScale);

        _miniMapCanvas.ZoomFactor = miniScale;
        _miniMapCanvas.Width = canvasW * miniScale;
        _miniMapCanvas.Height = canvasH * miniScale;

        var left = pad + (availW - _miniMapCanvas.Width) / 2.0;
        var top = pad + (availH - _miniMapCanvas.Height) / 2.0;
        Canvas.SetLeft(_miniMapCanvas, left);
        Canvas.SetTop(_miniMapCanvas, top);

        var zoom = Math.Max(vm.ZoomFactor, 0.0001);
        var view = _scroll.Viewport;
        var off = _scroll.Offset;

        var viewModelX = off.X / zoom;
        var viewModelY = off.Y / zoom;
        var viewModelW = view.Width / zoom;
        var viewModelH = view.Height / zoom;

        var rectLeft = left + viewModelX * miniScale;
        var rectTop = top + viewModelY * miniScale;
        var rectW = viewModelW * miniScale;
        var rectH = viewModelH * miniScale;

        // Clamp to minimap content area.
        rectW = Math.Clamp(rectW, 0, _miniMapCanvas.Width);
        rectH = Math.Clamp(rectH, 0, _miniMapCanvas.Height);
        rectLeft = Math.Clamp(rectLeft, left, left + _miniMapCanvas.Width - rectW);
        rectTop = Math.Clamp(rectTop, top, top + _miniMapCanvas.Height - rectH);

        Canvas.SetLeft(_miniMapViewport, rectLeft);
        Canvas.SetTop(_miniMapViewport, rectTop);
        _miniMapViewport.Width = rectW;
        _miniMapViewport.Height = rectH;
    }
}