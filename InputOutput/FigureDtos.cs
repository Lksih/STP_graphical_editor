using Avalonia.Media;
using Geometry;
using Geometry.Graphic;

namespace InputOutput;

internal sealed class FigureDto
{
    public required string Type { get; init; }
    public List<PointDto>? Points { get; init; }
    public PointDto? Center { get; init; }
    public double? Rx { get; init; }
    public double? Ry { get; init; }
    public double? Angle { get; init; }

    // ── Style ──
    public ColorDto? StrokeColor { get; init; }
    public double? StrokeThickness { get; init; }
    public ColorDto? FillColor { get; init; }
}

internal sealed class PointDto
{
    public required double X { get; init; }
    public required double Y { get; init; }

    public static PointDto FromGeometryPoint(Point point) =>
        new() { X = point.X, Y = point.Y };

    public Point ToGeometryPoint() => new(X, Y);
}

internal sealed class ColorDto
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte A { get; init; } = 255;
}