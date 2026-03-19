using Geometry;

namespace InputOutput;

internal sealed class FigureDto
{
    public required string Type { get; init; }
    public List<PointDto>? Points { get; init; }
    public PointDto? Center { get; init; }
    public double? Rx { get; init; }
    public double? Ry { get; init; }
    public double? Angle { get; init; }
}

internal sealed class PointDto
{
    public required double X { get; init; }
    public required double Y { get; init; }

    public static PointDto FromGeometryPoint(Point point)
    {
        return new PointDto
        {
            X = point.X,
            Y = point.Y
        };
    }

    public Point ToGeometryPoint()
    {
        return new Point(X, Y);
    }
}
