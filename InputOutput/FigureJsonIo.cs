using Geometry;
using Geometry.Graphic;
using Newtonsoft.Json;
using AvColor = Avalonia.Media.Color;

namespace InputOutput;

public static class FigureJsonIo
{
    private const string LineType = "Line";
    private const string CurveType = "Curve";
    private const string PolygonType = "Polygon";
    private const string CurvedPolygonType = "CurvedPolygon";
    private const string EllipseType = "Ellipse";

    private static readonly Type EllipseTypeInfo = typeof(Ellipse);

    // ═══════════════════════════════════════════════════════════
    //  SAVE
    // ═══════════════════════════════════════════════════════════

    public static async Task SaveFiguresAsync(
        IEnumerable<IFigure> figures,
        Dictionary<IFigure, IFigureGraphicProperties> styles,
        string filePath)
    {
        var dtos = figures.Select(f => ToDto(f, styles)).ToList();
        string json = JsonConvert.SerializeObject(dtos, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
    }

    // ═══════════════════════════════════════════════════════════
    //  LOAD
    // ═══════════════════════════════════════════════════════════

    public static async Task<(IReadOnlyList<IFigure> Figures,
        Dictionary<IFigure, IFigureGraphicProperties> Styles)>
        LoadFiguresAsync(string filePath)
    {
        string json = await File.ReadAllTextAsync(filePath);
        var dtos = JsonConvert.DeserializeObject<List<FigureDto>>(json);

        if (dtos is null)
            return (Array.Empty<IFigure>(),
                new Dictionary<IFigure, IFigureGraphicProperties>());

        var figures = new List<IFigure>(dtos.Count);
        var styles = new Dictionary<IFigure, IFigureGraphicProperties>(dtos.Count);

        foreach (var dto in dtos)
        {
            var figure = ToFigure(dto);
            figures.Add(figure);
            styles[figure] = ExtractStyle(dto);
        }

        return (figures, styles);
    }

    // ═══════════════════════════════════════════════════════════
    //  DTO ↔ Figure
    // ═══════════════════════════════════════════════════════════

    private static FigureDto ToDto(
        IFigure figure,
        Dictionary<IFigure, IFigureGraphicProperties> styles)
    {
        styles.TryGetValue(figure, out var style);

        var colorDto = style != null
            ? new ColorDto
            {
                R = style.Color.R,
                G = style.Color.G,
                B = style.Color.B,
                A = style.Color.A
            }
            : new ColorDto { R = 0, G = 0, B = 0, A = 255 };

        double thickness = style?.Thickness ?? 1.0;

        return figure switch
        {
            Line => new FigureDto
            {
                Type = LineType,
                Points = VerticesToDtos(figure),
                StrokeColor = colorDto,
                StrokeThickness = thickness
            },
            Curve => new FigureDto
            {
                Type = CurveType,
                Points = VerticesToDtos(figure),
                StrokeColor = colorDto,
                StrokeThickness = thickness
            },
            Polygon => new FigureDto
            {
                Type = PolygonType,
                Points = VerticesToDtos(figure),
                StrokeColor = colorDto,
                StrokeThickness = thickness
            },
            CurvedPolygon => new FigureDto
            {
                Type = CurvedPolygonType,
                Points = VerticesToDtos(figure),
                StrokeColor = colorDto,
                StrokeThickness = thickness
            },
            Ellipse ellipse => new FigureDto
            {
                Type = EllipseType,
                Center = PointDto.FromGeometryPoint(ellipse.Center),
                Rx = ReadEllipseField(ellipse, "Rx"),
                Ry = ReadEllipseField(ellipse, "Ry"),
                Angle = ReadEllipseField(ellipse, "Angle"),
                StrokeColor = colorDto,
                StrokeThickness = thickness
            },
            _ => throw new NotSupportedException(
                $"Figure type '{figure.GetType().Name}' is not supported.")
        };
    }

    private static List<PointDto> VerticesToDtos(IFigure figure) =>
        figure.Vertex.ToArray()
            .Select(PointDto.FromGeometryPoint).ToList();

    private static IFigure ToFigure(FigureDto dto) =>
        dto.Type switch
        {
            LineType => new Line(
                RequirePoints(dto, 2)[0],
                RequirePoints(dto, 2)[1]),
            CurveType => new Curve(RequirePoints(dto, 3)),
            PolygonType => new Polygon(RequirePointsAtLeast(dto, 3)),
            CurvedPolygonType => new CurvedPolygon(
                RequireCurvedPolygonPoints(dto)),
            EllipseType => CreateEllipse(dto),
            _ => throw new NotSupportedException(
                $"Figure DTO type '{dto.Type}' is not supported.")
        };

    // ═══════════════════════════════════════════════════════════
    //  Style helpers
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Извлечение стиля из DTO. Работает и со старыми JSON
    /// (без полей цвета) — вернёт чёрный, толщина 1.
    /// </summary>
    private static IFigureGraphicProperties ExtractStyle(FigureDto dto)
    {
        AvColor color;
        if (dto.StrokeColor is { } c)
            color = AvColor.FromArgb(c.A, c.R, c.G, c.B);
        else
            color = AvColor.FromArgb(255, 0, 0, 0);

        double thickness = dto.StrokeThickness ?? 1.0;

        return new FigureGraphicProperties(color, thickness);
    }

    // ═══════════════════════════════════════════════════════════
    //  Validation helpers
    // ═══════════════════════════════════════════════════════════

    private static Geometry.Point[] RequirePoints(FigureDto dto, int count)
    {
        var points = dto.Points?
            .Select(p => p.ToGeometryPoint()).ToArray();
        if (points is null || points.Length != count)
            throw new InvalidDataException(
                $"Figure '{dto.Type}' must have exactly {count} point(s).");
        return points;
    }

    private static Geometry.Point[] RequirePointsAtLeast(
        FigureDto dto, int minCount)
    {
        var points = dto.Points?
            .Select(p => p.ToGeometryPoint()).ToArray();
        if (points is null || points.Length < minCount)
            throw new InvalidDataException(
                $"Figure '{dto.Type}' must have ≥ {minCount} point(s).");
        return points;
    }

    private static Geometry.Point[] RequireCurvedPolygonPoints(FigureDto dto)
    {
        var points = dto.Points?
            .Select(p => p.ToGeometryPoint()).ToArray();
        if (points is null || points.Length < 4 || points.Length % 2 != 0)
            throw new InvalidDataException(
                "CurvedPolygon must have ≥ 4 points, count divisible by 2.");
        return points;
    }

    private static IFigure CreateEllipse(FigureDto dto)
    {
        if (dto.Center is null || dto.Rx is null || dto.Ry is null)
            throw new InvalidDataException(
                "Ellipse must have center, rx, ry.");

        var ellipse = new Ellipse(
            dto.Center.ToGeometryPoint(), dto.Rx.Value, dto.Ry.Value);

        if (dto.Angle is not null and not 0)
            ellipse.Rotate(dto.Angle.Value);

        return ellipse;
    }

    private static double ReadEllipseField(Ellipse ellipse, string fieldName)
    {
        var field = EllipseTypeInfo.GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (field is null || field.FieldType != typeof(double))
            throw new MissingFieldException(nameof(Ellipse), fieldName);
        return (double)field.GetValue(ellipse)!;
    }
}