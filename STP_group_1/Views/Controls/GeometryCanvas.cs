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

    public void Refresh()
    {
        InvalidateVisual();
    }

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

    public static readonly StyledProperty<IReadOnlyDictionary<IFigure, IFigureGraphicProperties>?> FigureGraphicPropertiesMapProperty =
        AvaloniaProperty.Register<GeometryCanvas, IReadOnlyDictionary<IFigure, IFigureGraphicProperties>?>(nameof(FigureGraphicPropertiesMap));

    public IReadOnlyDictionary<IFigure, IFigureGraphicProperties>? FigureGraphicPropertiesMap
    {
        get => GetValue(FigureGraphicPropertiesMapProperty);
        set => SetValue(FigureGraphicPropertiesMapProperty, value);
    }

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
    AvaloniaProperty.Register<GeometryCanvas, IBrush?>(nameof(Background));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    private bool _isDragging;
    private Avalonia.Point _dragStartPointer;

    static GeometryCanvas()
    {
        AffectsRender<GeometryCanvas>(FiguresProperty, ZoomFactorProperty, SelectedFigureProperty, FigureGraphicPropertiesMapProperty, BackgroundProperty);
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

        var vm = DataContext as ICanvasInteractionHandler;
        var figures = Figures;
        if (vm is null || figures is null)
            return;

        var modelPoint = new Geometry.Point(pt.X / ZoomFactor, pt.Y / ZoomFactor);

        const double baseEpsPx = 6.0;
        var eps = baseEpsPx / Math.Max(ZoomFactor, 0.0001);
        var handled = vm.HandleCanvasPointerPressed(
            modelPoint,
            point.Properties.IsLeftButtonPressed,
            point.Properties.IsRightButtonPressed,
            eps,
            figures,
            out var shouldStartDragging);

        if (!handled)
            return;

        if (shouldStartDragging)
        {
            _isDragging = true;
            _dragStartPointer = pt;
            e.Pointer.Capture(this);
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDragging)
            return;

        var vm = DataContext as ICanvasInteractionHandler;
        if (vm is null)
            return;

        var pt = e.GetPosition(this);
        var delta = pt - _dragStartPointer;

        var dx = delta.X / Math.Max(ZoomFactor, 0.0001);
        var dy = delta.Y / Math.Max(ZoomFactor, 0.0001);

        vm.HandleCanvasDragDelta(dx, dy);
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
        if (Background != null)
        {
            context.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }

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

        if (FigureGraphicPropertiesMap is not null && FigureGraphicPropertiesMap.TryGetValue(figure, out var props))
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
            ctx.DrawEllipse(null, pen, centerPt, verts[0].X, verts[0].Y);
        }
    }
}
