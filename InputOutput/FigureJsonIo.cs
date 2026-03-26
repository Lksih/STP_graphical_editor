using Geometry;
using Newtonsoft.Json;

namespace InputOutput;

public static class FigureJsonIo
{
    private const string LineType = "Line";
    private const string CurveType = "Curve";
    private const string PolygonType = "Polygon";
    private const string CurvedPolygonType = "CurvedPolygon";
    private const string EllipsType = "Ellipse";

    private static readonly Type EllipsTypeInfo = typeof(Ellipse);

    public static void SaveFigures(IEnumerable<IFigure> figures, string filePath)
    {
        var dtos = figures.Select(ToDto).ToList();
        string json = JsonConvert.SerializeObject(dtos, Formatting.Indented);

        File.WriteAllText(filePath, json);
    }

    public static IReadOnlyList<IFigure> LoadFigures(string filePath)
    {
        string json = File.ReadAllText(filePath);
        List<FigureDto>? dtos = JsonConvert.DeserializeObject<List<FigureDto>>(json);

        if (dtos is null)
        {
            return [];
        }

        return dtos.Select(ToFigure).ToList();
    }

    private static FigureDto ToDto(IFigure figure)
    {
        return figure switch
        {
            Line line => new FigureDto
            {
                Type = LineType,
                Points = line.Vertex.ToArray().Select(PointDto.FromGeometryPoint).ToList()
            },
            Curve curve => new FigureDto
            {
                Type = CurveType,
                Points = curve.Vertex.ToArray().Select(PointDto.FromGeometryPoint).ToList()
            },
            Polygon polygon => new FigureDto
            {
                Type = PolygonType,
                Points = polygon.Vertex.ToArray().Select(PointDto.FromGeometryPoint).ToList()
            },
            CurvedPolygon curvedPolygon => new FigureDto
            {
                Type = CurvedPolygonType,
                Points = curvedPolygon.Vertex.ToArray().Select(PointDto.FromGeometryPoint).ToList()
            },
            Ellipse ellipse => new FigureDto
            {
                Type = EllipsType,
                Center = PointDto.FromGeometryPoint(ellipse.Center),
                Rx = ReadEllipsField(ellipse, "Rx"),
                Ry = ReadEllipsField(ellipse, "Ry"),
                Angle = ReadEllipsField(ellipse, "Angle")
            },
            _ => throw new NotSupportedException($"Figure type '{figure.GetType().Name}' is not supported.")
        };
    }

    private static IFigure ToFigure(FigureDto dto)
    {
        return dto.Type switch
        {
            LineType => new Line(RequirePoints(dto, 2)[0], RequirePoints(dto, 2)[1]),
            CurveType => new Curve(RequirePoints(dto, 3)),
            PolygonType => new Polygon(RequirePointsAtLeast(dto, 3)),
            CurvedPolygonType => new CurvedPolygon(RequireCurvedPolygonPoints(dto)),
            EllipsType => CreateEllips(dto),
            _ => throw new NotSupportedException($"Figure DTO type '{dto.Type}' is not supported.")
        };
    }

    private static Point[] RequirePoints(FigureDto dto, int count)
    {
        var points = dto.Points?.Select(p => p.ToGeometryPoint()).ToArray();
        if (points is null || points.Length != count)
        {
            throw new InvalidDataException($"Figure '{dto.Type}' must contain exactly {count} point(s).");
        }

        return points;
    }

    private static Point[] RequirePointsAtLeast(FigureDto dto, int minCount)
    {
        var points = dto.Points?.Select(p => p.ToGeometryPoint()).ToArray();
        if (points is null || points.Length < minCount)
        {
            throw new InvalidDataException($"Figure '{dto.Type}' must contain at least {minCount} point(s).");
        }

        return points;
    }

    private static Point[] RequireCurvedPolygonPoints(FigureDto dto)
    {
        var points = dto.Points?.Select(p => p.ToGeometryPoint()).ToArray();
        if (points is null || points.Length < 4 || points.Length % 2 != 0)
        {
            throw new InvalidDataException("Figure 'CurvedPolygon' must contain at least 4 points and the points count must be divisible by 2.");
        }

        return points;
    }

    private static IFigure CreateEllips(FigureDto dto)
    {
        if (dto.Center is null || dto.Rx is null || dto.Ry is null)
        {
            throw new InvalidDataException("Figure 'Ellipse' must contain center, rx and ry.");
        }

        var ellipse = new Ellipse(dto.Center.ToGeometryPoint(), dto.Rx.Value, dto.Ry.Value);
        if (dto.Angle is not null && dto.Angle.Value != 0)
        {
            ellipse.Rotate(dto.Angle.Value);
        }

        return ellipse;
    }

    private static double ReadEllipsField(Ellipse ellipse, string fieldName)
    {
        var field = EllipsTypeInfo.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field is null || field.FieldType != typeof(double))
        {
            throw new MissingFieldException(nameof(Ellipse), fieldName);
        }

        return (double)field.GetValue(ellipse)!;
    }
}
