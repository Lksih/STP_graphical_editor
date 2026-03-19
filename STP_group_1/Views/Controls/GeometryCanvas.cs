using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Geometry;
using STP_group_1.ViewModels;
using UI.Models;

namespace STP_group_1.Views.Controls;

public sealed class GeometryCanvas : Control
{
    public static readonly StyledProperty<IEnumerable<IFigure>?> FiguresProperty =
        AvaloniaProperty.Register<GeometryCanvas, IEnumerable<IFigure>?>(nameof(Figures));

    public IEnumerable<IFigure>? Figures
    {
        get => GetValue(FiguresProperty);
        set => SetValue(FiguresProperty, value);
    }

    public static readonly StyledProperty<double> ZoomFactorProperty =
        AvaloniaProperty.Register<GeometryCanvas, double>(nameof(ZoomFactor), 1.0);

    public double ZoomFactor
    {
        get => GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    public static readonly StyledProperty<IFigure?> SelectedFigureProperty =
        AvaloniaProperty.Register<GeometryCanvas, IFigure?>(nameof(SelectedFigure));

    public IFigure? SelectedFigure
    {
        get => GetValue(SelectedFigureProperty);
        set => SetValue(SelectedFigureProperty, value);
    }

    private bool _isDragging;
    private Avalonia.Point _dragStartPointer;

    private Geometry.Point? _lineStart;
    private readonly List<Geometry.Point> _polygonPoints = new();
    private Geometry.Point? _ellipseCenter;

    static GeometryCanvas()
    {
        AffectsRender<GeometryCanvas>(FiguresProperty, ZoomFactorProperty, SelectedFigureProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Control по умолчанию меряется в 0x0; нам нужно занимать доступное место.
        return availableSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FiguresProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldCol)
                oldCol.CollectionChanged -= OnFiguresCollectionChanged;
            if (change.NewValue is INotifyCollectionChanged newCol)
                newCol.CollectionChanged += OnFiguresCollectionChanged;

            InvalidateVisual();
        }
    }

    private void OnFiguresCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pt = e.GetPosition(this);
        var point = e.GetCurrentPoint(this);

        var vm = DataContext as MainWindowViewModel;
        var figures = Figures;
        if (vm is null || figures is null)
            return;

        var modelPoint = new Geometry.Point(pt.X / ZoomFactor, pt.Y / ZoomFactor);

        // ---- Creation tools ----
        if (vm.SelectedTool == ToolKind.Line || vm.SelectedTool == ToolKind.Polygon || vm.SelectedTool == ToolKind.Ellipse)
        {
            if (vm.SelectedTool == ToolKind.Line && point.Properties.IsLeftButtonPressed)
            {
                if (_lineStart is null)
                {
                    _lineStart = modelPoint;
                }
                else
                {
                    var fig = new Line(_lineStart, modelPoint);
                    vm.CurrentLayerFigures.Add(fig);
                    vm.CurrentLayerFiguresGraphicProperties[fig] = new FigureGraphicProperties(vm.ForegroundColor, 2.0);
                    vm.SelectedFigure = fig;
                    _lineStart = null;
                    vm.IsDirty = true;
                    InvalidateVisual();
                }

                e.Handled = true;
                return;
            }

            if (vm.SelectedTool == ToolKind.Polygon)
            {
                if (point.Properties.IsLeftButtonPressed)
                {
                    _polygonPoints.Add(modelPoint);
                    e.Handled = true;
                    return;
                }

                if (point.Properties.IsRightButtonPressed && _polygonPoints.Count >= 3)
                {
                    var verts = _polygonPoints.ToArray();
                    var fig = new Polygon(verts);
                    vm.CurrentLayerFigures.Add(fig);
                    vm.CurrentLayerFiguresGraphicProperties[fig] = new FigureGraphicProperties(vm.ForegroundColor, 2.0);
                    vm.SelectedFigure = fig;
                    _polygonPoints.Clear();
                    vm.IsDirty = true;
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
            }

            if (vm.SelectedTool == ToolKind.Ellipse && point.Properties.IsLeftButtonPressed)
            {
                if (_ellipseCenter is null)
                {
                    _ellipseCenter = modelPoint;
                }
                else
                {
                    var c = _ellipseCenter;
                    var rx = Math.Abs(modelPoint.X - c.X);
                    var ry = Math.Abs(modelPoint.Y - c.Y);
                    if (rx < 1) rx = 1;
                    if (ry < 1) ry = 1;

                    var fig = new Ellipse(c, rx, ry);
                    vm.CurrentLayerFigures.Add(fig);
                    vm.CurrentLayerFiguresGraphicProperties[fig] = new FigureGraphicProperties(vm.ForegroundColor, 2.0);
                    vm.SelectedFigure = fig;
                    _ellipseCenter = null;
                    vm.IsDirty = true;
                    InvalidateVisual();
                }

                e.Handled = true;
                return;
            }
        }

        // ---- Selection / move / erase ----
        if (!point.Properties.IsLeftButtonPressed)
            return;

        const double baseEpsPx = 6.0;
        var eps = baseEpsPx / Math.Max(ZoomFactor, 0.0001);

        // hit-test сверху вниз (последняя фигура "выше")
        var hit = figures.Reverse().FirstOrDefault(f => f.IsIn(modelPoint, eps));

        if (hit is null)
        {
            vm.SelectedFigure = null;
            InvalidateVisual();
            return;
        }

        if (vm.SelectedTool == ToolKind.Eraser)
        {
            // Удаление объекта
            var list = vm.CurrentLayerFigures;
            if (list.Contains(hit))
            {
                list.Remove(hit);
                if (ReferenceEquals(vm.SelectedFigure, hit))
                    vm.SelectedFigure = null;
                vm.IsDirty = true;
                InvalidateVisual();
            }

            e.Handled = true;
            return;
        }

        vm.SelectedFigure = hit;

        if (vm.SelectedTool == ToolKind.Move)
        {
            _isDragging = true;
            _dragStartPointer = pt;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
        else
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDragging)
            return;

        var vm = DataContext as MainWindowViewModel;
        var fig = vm?.SelectedFigure;
        if (vm is null || fig is null)
            return;

        var pt = e.GetPosition(this);
        var delta = pt - _dragStartPointer;

        var dx = delta.X / Math.Max(ZoomFactor, 0.0001);
        var dy = delta.Y / Math.Max(ZoomFactor, 0.0001);

        fig.Move(dx, dy);
        _dragStartPointer = pt;

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var figures = Figures;
        if (figures is null)
            return;

        foreach (var figure in figures)
        {
            DrawFigure(context, figure);
        }
    }

    private void DrawFigure(DrawingContext ctx, IFigure figure)
    {
        var verts = figure.Vertex.ToArray();
        var center = figure.Center;

        var color = Colors.CornflowerBlue;
        var thickness = 1.5;

        if (figure is IFigureGraphicProperties props)
        {
            var c = props.Color;
            color = new Color(c.A, c.R, c.G, c.B);
            thickness = props.Thickness;
        }

        if (ReferenceEquals(figure, SelectedFigure))
        {
            // лёгкая подсветка выбранной
            thickness = Math.Max(thickness, 2.5);
            color = Colors.DeepSkyBlue;
        }

        var pen = new Pen(new SolidColorBrush(color), thickness);

        if (verts.Length == 2)
        {
            var p1 = new Avalonia.Point(verts[0].X * ZoomFactor, verts[0].Y * ZoomFactor);
            var p2 = new Avalonia.Point(verts[1].X * ZoomFactor, verts[1].Y * ZoomFactor);
            ctx.DrawLine(pen, p1, p2);
        }
        else if (verts.Length > 2)
        {
            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(new Avalonia.Point(verts[0].X * ZoomFactor, verts[0].Y * ZoomFactor), true);
                for (var i = 1; i < verts.Length; i++)
                {
                    g.LineTo(new Avalonia.Point(verts[i].X * ZoomFactor, verts[i].Y * ZoomFactor));
                }
                g.EndFigure(true);
            }

            ctx.DrawGeometry(null, pen, geo);
        }
        else
        {
            var centerPt = new Avalonia.Point(center.X * ZoomFactor, center.Y * ZoomFactor);
            const double radius = 12;
            ctx.DrawEllipse(null, pen, centerPt, radius, radius);
        }
    }
}

