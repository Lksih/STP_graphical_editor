
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Geometry;
using Geometry.Graphic;
using STP_group_1.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace STP_group_1.Views.Controls;

public sealed class GeometryCanvas : Control
{
    public static readonly StyledProperty<IEnumerable<Geometry.Point>> PressedPointsProperty =
        AvaloniaProperty.Register<GeometryCanvas, IEnumerable<Geometry.Point>>(nameof(PressedPoints));

    public IEnumerable<Geometry.Point> PressedPoints
    {
        get => GetValue(PressedPointsProperty);
        set => SetValue(PressedPointsProperty, value);
    }

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
        // Никогда не возвращаем Infinity/NaN: внутри Canvas availableSize может быть бесконечным,
        // а это приводит к InvalidOperationException ("Invalid size returned for Measure").
        var w = Width;
        var h = Height;

        var hasExplicitW = !double.IsNaN(w) && !double.IsInfinity(w);
        var hasExplicitH = !double.IsNaN(h) && !double.IsInfinity(h);

        if (hasExplicitW || hasExplicitH)
        {
            var mw = hasExplicitW ? w : 0;
            var mh = hasExplicitH ? h : 0;
            return new Size(Math.Max(0, mw), Math.Max(0, mh));
        }

        var aw = availableSize.Width;
        var ah = availableSize.Height;

        if (double.IsNaN(aw) || double.IsInfinity(aw))
            aw = 0;
        if (double.IsNaN(ah) || double.IsInfinity(ah))
            ah = 0;

        return new Size(Math.Max(0, aw), Math.Max(0, ah));
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
            e.KeyModifiers,
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
        var (dx, dy) = GetDxDyFromPointer(pt);

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

            var vm = DataContext as ICanvasInteractionHandler;
            if (vm is null)
                return;

            var pt = e.GetPosition(this);
            var (dx, dy) = GetDxDyFromPointer(pt);

            vm.HandleCanvasPointerReleased(dx, dy);
        }
    }

    private (double dx, double dy) GetDxDyFromPointer(Avalonia.Point pt)
    {
        var delta = pt - _dragStartPointer;

        var dx = delta.X / Math.Max(ZoomFactor, 0.0001);
        var dy = delta.Y / Math.Max(ZoomFactor, 0.0001);

        return (dx, dy);
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

        var points = PressedPoints;
        if (points is null)
            return;

        foreach (var point in points)
        {
            context.DrawEllipse(
                Brushes.Red,
                null,
                new Avalonia.Point(point.X * ZoomFactor, point.Y * ZoomFactor),
                3,
                3
            );
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

        var isFilled = false;
        var fillColor = color;
        if (FigureGraphicPropertiesMap is not null && FigureGraphicPropertiesMap.TryGetValue(figure, out var props2))
        {
            isFilled = props2.IsFilled;
            fillColor = props2.FillColor;
        }

        var fillBrush = isFilled
            ? new SolidColorBrush(new Color(
                (byte)Math.Clamp((int)(fillColor.A * 0.25), 0, 255),
                fillColor.R,
                fillColor.G,
                fillColor.B))
            : null;

        if (figure is Curve && verts.Length == 3)
        {
            var p0 = verts[0];
            var p1 = verts[1];
            var p2 = verts[2];

            var len01 = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2));
            var len12 = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            var approxLen = len01 + len12;
            var segments = (int)Math.Clamp(approxLen * ZoomFactor / 6.0, 16, 128);

            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(new Avalonia.Point(p0.X * ZoomFactor, p0.Y * ZoomFactor), false);

                for (int i = 1; i <= segments; i++)
                {
                    var t = (double)i / segments;
                    var oneMinusT = 1.0 - t;
                    var currX =
                        oneMinusT * oneMinusT * p0.X +
                        2 * oneMinusT * t * p1.X +
                        t * t * p2.X;
                    var currY =
                        oneMinusT * oneMinusT * p0.Y +
                        2 * oneMinusT * t * p1.Y +
                        t * t * p2.Y;

                    g.LineTo(new Avalonia.Point(currX * ZoomFactor, currY * ZoomFactor));
                }

                g.EndFigure(false);
            }

            ctx.DrawGeometry(null, pen, geo);
            return;
        }

        if (figure is CurvedPolygon)
        {
            var points = new List<Geometry.Point>();

            void SampleSegment(Geometry.Point a, Geometry.Point b, Geometry.Point c)
            {
                var len01 = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
                var len12 = Math.Sqrt(Math.Pow(c.X - b.X, 2) + Math.Pow(c.Y - b.Y, 2));
                var approxLen = len01 + len12;

                var segs = (int)Math.Clamp(approxLen * ZoomFactor / 6.0, 8, 96);

                for (int j = 0; j <= segs; j++)
                {
                    if (points.Count > 0 && j == 0)
                        continue;

                    var t = (double)j / segs;
                    var oneMinusT = 1.0 - t;
                    var currX =
                        oneMinusT * oneMinusT * a.X +
                        2 * oneMinusT * t * b.X +
                        t * t * c.X;
                    var currY =
                        oneMinusT * oneMinusT * a.Y +
                        2 * oneMinusT * t * b.Y +
                        t * t * c.Y;

                    points.Add(new Geometry.Point(currX, currY));
                }
            }

            for (int i = 0; i < verts.Length - 2; i += 3)
                SampleSegment(verts[i], verts[i + 1], verts[i + 2]);

            if (verts.Length >= 3)
                SampleSegment(verts[verts.Length - 2], verts[verts.Length - 1], verts[0]);

            if (points.Count >= 2)
            {
                var geo = new StreamGeometry();
                using (var g = geo.Open())
                {
                    g.BeginFigure(new Avalonia.Point(points[0].X * ZoomFactor, points[0].Y * ZoomFactor), isFilled);
                    for (int i = 1; i < points.Count; i++)
                        g.LineTo(new Avalonia.Point(points[i].X * ZoomFactor, points[i].Y * ZoomFactor));
                    g.EndFigure(true);
                }

                ctx.DrawGeometry(fillBrush, pen, geo);
                return;
            }
        }

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
                g.BeginFigure(new Avalonia.Point(verts[0].X * ZoomFactor, verts[0].Y * ZoomFactor), isFilled);
                for (var i = 1; i < verts.Length; i++)
                {
                    g.LineTo(new Avalonia.Point(verts[i].X * ZoomFactor, verts[i].Y * ZoomFactor));
                }
                g.EndFigure(true);
            }

            ctx.DrawGeometry(fillBrush, pen, geo);
        }
        else
        {
            double angle = ((Ellipse)figure).ReadableAngle;

            var centerPt = new Avalonia.Point(center.X * ZoomFactor, center.Y * ZoomFactor);

            var matrix = Matrix.CreateRotation(angle) *
                 Matrix.CreateTranslation(centerPt.X, centerPt.Y);

            using (ctx.PushTransform(matrix))
            {
                if (fillBrush is not null)
                    ctx.DrawEllipse(fillBrush, pen, new Avalonia.Point(0, 0), verts[0].X, verts[0].Y);
                else
                    ctx.DrawEllipse(null, pen, new Avalonia.Point(0, 0), verts[0].X, verts[0].Y);
            }
        }
    }
}
