using Geometry;
using Geometry.Graphic;
using Svg;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using AvColor = Avalonia.Media.Color;
using Point = Geometry.Point;
using SdColor = System.Drawing.Color;

namespace InputOutput
{
    /// <summary>
    /// Реестр сериализаторов для ЧИСТОЙ геометрии.
    /// Стили теперь обрабатываются отдельно.
    /// </summary>
    public static class FigureSerializers
    {
        private static readonly Dictionary<Type, IFigureSerializer> _serializers = new();
        private static readonly Dictionary<string, IFigureSerializer> _tagMap = new();

        static FigureSerializers()
        {
            Register(new LineSerializer());
            Register(new PolygonSerializer());
            Register(new EllipseSerializer());
            Register(new CurveSerializer());
            Register(new CurvedPolygonSerializer());
        }

        public static void Register(IFigureSerializer serializer)
        {
            _serializers[serializer.FigureType] = serializer;
            _tagMap[serializer.SvgTagName] = serializer;
        }

        public static IFigureSerializer? GetSerializer(IFigure figure) =>
            _serializers.GetValueOrDefault(figure.GetType());

        public static IFigureSerializer? GetSerializer(string tagName) =>
            _tagMap.GetValueOrDefault(tagName);
    }

    public static class SVGConverter
    {
        /// <summary>
        /// СОХРАНЕНИЕ: Принимает геометрию и словарь стилей.
        /// </summary>
        public static void Save(
            IEnumerable<IFigure> figures,
            Dictionary<IFigure, IFigureGraphicProperties> styles,
            string filePath,
            int width,
            int height)
        {
            string ext = Path.GetExtension(filePath).TrimStart('.').ToLower();

            if (ext == "svg")
            {
                CanvasToSVG(figures, styles, filePath);
            }
            else
            {
                string tempSvg = Path.ChangeExtension(filePath, ".temp.svg");
                try
                {
                    CanvasToSVG(figures, styles, tempSvg);
                    SVGToBitmapFormat(tempSvg, filePath, width, height);
                }
                finally
                {
                    if (File.Exists(tempSvg)) File.Delete(tempSvg);
                }
            }
        }

        /// <summary>
        /// ЗАГРУЗКА: Возвращает и геометрию, и словарь стилей.
        /// </summary>
        public static (List<IFigure> Figures, Dictionary<IFigure, IFigureGraphicProperties> Styles) Load(string filePath)
        {
            var figures = new List<IFigure>();
            var styles = new Dictionary<IFigure, IFigureGraphicProperties>();

            var doc = new XmlDocument();
            doc.Load(filePath);

            var nodes = doc.SelectNodes("//*[local-name()='svg']/*");
            if (nodes == null) return (figures, styles);

            foreach (XmlNode node in nodes)
            {
                var serializer = FigureSerializers.GetSerializer(node.Name);
                if (serializer != null)
                {
                    try
                    {
                        // 1. Восстанавливаем геометрию
                        var figure = serializer.FromXml(node);
                        figures.Add(figure);

                        // 2. Восстанавливаем стиль
                        var style = StyleHelper.ReadStyleFromXml(node);
                        styles[figure] = style;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading {node.Name}: {ex.Message}");
                    }
                }
            }
            return (figures, styles);
        }

        private static void CanvasToSVG(
            IEnumerable<IFigure> figures,
            Dictionary<IFigure, IFigureGraphicProperties> styles,
            string filePath)
        {
            var doc = new XmlDocument();
            var svgEl = doc.CreateElement("svg");
            svgEl.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
            svgEl.SetAttribute("version", "1.1");
            doc.AppendChild(svgEl);

            foreach (var fig in figures)
            {
                var serializer = FigureSerializers.GetSerializer(fig);
                if (serializer != null)
                {
                    var xmlEl = serializer.ToXml(doc, fig);

                    // Достаем стиль из словаря, если его нет - берем дефолтный
                    if (!styles.TryGetValue(fig, out var style))
                    {
                        style = new FigureGraphicProperties(AvColor.FromRgb(0, 0, 0), 1.0); // Default style
                    }

                    StyleHelper.WriteStyleToXml(xmlEl, style);
                    svgEl.AppendChild(xmlEl);
                }
            }
            doc.Save(filePath);
        }

        private static void SVGToBitmapFormat(string svgPath, string outputPath, int width, int height)
        {
            var svgDoc = SvgDocument.Open(svgPath);
            using var bitmap = new System.Drawing.Bitmap(width, height);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.Clear(SdColor.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                svgDoc.Draw(g);
            }

            string fmt = Path.GetExtension(outputPath).TrimStart('.').ToLower();
            var encoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.MimeType == $"image/{fmt}")
                          ?? throw new NotSupportedException($"Format {fmt} not supported");

            using var pars = new EncoderParameters(1);
            pars.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
            bitmap.Save(outputPath, encoder, pars);
        }
    }

    public interface IFigureSerializer
    {
        string SvgTagName { get; }
        Type FigureType { get; }
        XmlElement ToXml(XmlDocument doc, IFigure figure);
        IFigure FromXml(XmlNode node);
    }

    internal static class StyleHelper
    {
        public static string ColorToString(AvColor c) =>
            $"rgba({c.R},{c.G},{c.B},{c.A / 255.0:0.##})".Replace(',', '.');

        public static AvColor ParseColor(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "none") return AvColor.FromRgb(0, 0, 0);
            try
            {
                if (s.StartsWith("rgba"))
                {
                    var parts = s.Replace("rgba(", "").Replace(")", "").Split(',');
                    byte r = byte.Parse(parts[0].Trim());
                    byte g = byte.Parse(parts[1].Trim());
                    byte b = byte.Parse(parts[2].Trim());
                    double aDouble = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
                    return AvColor.FromArgb((byte)(aDouble * 255), r, g, b);
                }
                var sdColor = System.Drawing.ColorTranslator.FromHtml(s);
                return AvColor.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B);
            }
            catch { return AvColor.FromRgb(0, 0, 0); }
        }

        public static void WriteStyleToXml(XmlElement el, IFigureGraphicProperties style)
        {
            el.SetAttribute("stroke", ColorToString(style.Color));
            el.SetAttribute("stroke-width", style.Thickness.ToString(CultureInfo.InvariantCulture));
            el.SetAttribute("fill", "none");
        }

        public static IFigureGraphicProperties ReadStyleFromXml(XmlNode node)
        {
            var strokeAttr = node.Attributes?["stroke"]?.Value;
            var widthAttr = node.Attributes?["stroke-width"]?.Value;

            var color = strokeAttr != null ? ParseColor(strokeAttr) : AvColor.FromRgb(0, 0, 0);
            var thickness = widthAttr != null ? double.Parse(widthAttr, CultureInfo.InvariantCulture) : 1.0;

            return new FigureGraphicProperties(color, thickness);
        }
    }

    public class LineSerializer : IFigureSerializer
    {
        public string SvgTagName => "line";
        public Type FigureType => typeof(Line);

        public XmlElement ToXml(XmlDocument doc, IFigure figure)
        {
            var el = doc.CreateElement("line");
            var v = figure.Vertex;
            el.SetAttribute("x1", v[0].X.ToString(CultureInfo.InvariantCulture));
            el.SetAttribute("y1", v[0].Y.ToString(CultureInfo.InvariantCulture));
            el.SetAttribute("x2", v[1].X.ToString(CultureInfo.InvariantCulture));
            el.SetAttribute("y2", v[1].Y.ToString(CultureInfo.InvariantCulture));
            return el;
        }

        public IFigure FromXml(XmlNode node)
        {
            double x1 = double.Parse(node.Attributes!["x1"]!.Value, CultureInfo.InvariantCulture);
            double y1 = double.Parse(node.Attributes!["y1"]!.Value, CultureInfo.InvariantCulture);
            double x2 = double.Parse(node.Attributes!["x2"]!.Value, CultureInfo.InvariantCulture);
            double y2 = double.Parse(node.Attributes!["y2"]!.Value, CultureInfo.InvariantCulture);
            return new Line(new Point(x1, y1), new Point(x2, y2));
        }
    }

    public class PolygonSerializer : IFigureSerializer
    {
        public string SvgTagName => "polygon";
        public Type FigureType => typeof(Polygon);

        public XmlElement ToXml(XmlDocument doc, IFigure figure)
        {
            var el = doc.CreateElement("polygon");
            var sb = new StringBuilder();
            foreach (var p in figure.Vertex)
                sb.Append($"{p.X.ToString(CultureInfo.InvariantCulture)},{p.Y.ToString(CultureInfo.InvariantCulture)} ");
            el.SetAttribute("points", sb.ToString().Trim());
            return el;
        }

        public IFigure FromXml(XmlNode node)
        {
            var split = node.Attributes!["points"]!.Value.Trim().Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            var points = new Point[split.Length / 2];
            for (int i = 0; i < split.Length; i += 2)
                points[i / 2] = new Point(
                    double.Parse(split[i], CultureInfo.InvariantCulture),
                    double.Parse(split[i + 1], CultureInfo.InvariantCulture));
            return new Polygon(points);
        }
    }

    public class EllipseSerializer : IFigureSerializer
    {
        public string SvgTagName => "ellipse";
        public Type FigureType => typeof(Ellipse);

        public XmlElement ToXml(XmlDocument doc, IFigure figure)
        {
            var el = doc.CreateElement("ellipse");
            var f = (Ellipse)figure;
            var type = typeof(Ellipse);
            double rx = (double)type.GetField("Rx", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f)!;
            double ry = (double)type.GetField("Ry", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f)!;
            double angle = (double)type.GetField("Angle", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f)!;

            el.SetAttribute("cx", f.Center.X.ToString(CultureInfo.InvariantCulture));
            el.SetAttribute("cy", f.Center.Y.ToString(CultureInfo.InvariantCulture));
            el.SetAttribute("rx", rx.ToString(CultureInfo.InvariantCulture));
            el.SetAttribute("ry", ry.ToString(CultureInfo.InvariantCulture));

            if (Math.Abs(angle) > 0.001)
            {
                double deg = angle * 180.0 / Math.PI;
                el.SetAttribute("transform",
                    $"rotate({deg.ToString(CultureInfo.InvariantCulture)} {f.Center.X.ToString(CultureInfo.InvariantCulture)} {f.Center.Y.ToString(CultureInfo.InvariantCulture)})");
            }
            return el;
        }

        public IFigure FromXml(XmlNode node)
        {
            double cx = double.Parse(node.Attributes!["cx"]!.Value, CultureInfo.InvariantCulture);
            double cy = double.Parse(node.Attributes!["cy"]!.Value, CultureInfo.InvariantCulture);
            double rx = double.Parse(node.Attributes!["rx"]!.Value, CultureInfo.InvariantCulture);
            double ry = double.Parse(node.Attributes!["ry"]!.Value, CultureInfo.InvariantCulture);

            var ellipse = new Ellipse(new Point(cx, cy), rx, ry);

            var transform = node.Attributes["transform"]?.Value;
            if (!string.IsNullOrEmpty(transform) && transform.StartsWith("rotate"))
            {
                var parts = transform.Replace("rotate(", "").Replace(")", "").Split(' ');
                double deg = double.Parse(parts[0], CultureInfo.InvariantCulture);
                ellipse.Rotate(deg * Math.PI / 180.0);
            }
            return ellipse;
        }
    }
    public class CurveSerializer : IFigureSerializer
    {
        public string SvgTagName => "path";
        public Type FigureType => typeof(Curve);

        public XmlElement ToXml(XmlDocument doc, IFigure figure)
        {
            var el = doc.CreateElement("path");
            var v = figure.Vertex;
            string d = $"M {v[0].X.ToString(CultureInfo.InvariantCulture)} {v[0].Y.ToString(CultureInfo.InvariantCulture)} " +
                       $"Q {v[1].X.ToString(CultureInfo.InvariantCulture)} {v[1].Y.ToString(CultureInfo.InvariantCulture)} " +
                       $"{v[2].X.ToString(CultureInfo.InvariantCulture)} {v[2].Y.ToString(CultureInfo.InvariantCulture)}";
            el.SetAttribute("d", d);
            el.SetAttribute("data-type", "curve");
            return el;
        }

        public IFigure FromXml(XmlNode node) => throw new NotImplementedException("Load curve logic");
    }

    public class CurvedPolygonSerializer : IFigureSerializer
    {
        public string SvgTagName => "path-poly";
        public Type FigureType => typeof(CurvedPolygon);

        public XmlElement ToXml(XmlDocument doc, IFigure figure)
        {
            var el = doc.CreateElement("path");
            el.SetAttribute("data-type", "CurvedPolygon");
            var v = figure.Vertex;
            var sb = new StringBuilder();
            for (int i = 0; i < v.Length; i += 3)
            {
                sb.Append($"M {v[i].X.ToString(CultureInfo.InvariantCulture)} {v[i].Y.ToString(CultureInfo.InvariantCulture)} ");
                sb.Append($"Q {v[i + 1].X.ToString(CultureInfo.InvariantCulture)} {v[i + 1].Y.ToString(CultureInfo.InvariantCulture)} ");
                sb.Append($"{v[i + 2].X.ToString(CultureInfo.InvariantCulture)} {v[i + 2].Y.ToString(CultureInfo.InvariantCulture)} ");
            }
            el.SetAttribute("d", sb.ToString().Trim());
            return el;
        }

        public IFigure FromXml(XmlNode node)
        {
            string d = node.Attributes!["d"]!.Value;
            var parts = d.Split('M', StringSplitOptions.RemoveEmptyEntries);
            var points = new List<Point>();
            foreach (var part in parts)
            {
                var coords = part.Replace("Q", "").Trim().Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (coords.Length >= 6)
                {
                    points.Add(new Point(double.Parse(coords[0], CultureInfo.InvariantCulture), double.Parse(coords[1], CultureInfo.InvariantCulture)));
                    points.Add(new Point(double.Parse(coords[2], CultureInfo.InvariantCulture), double.Parse(coords[3], CultureInfo.InvariantCulture)));
                    points.Add(new Point(double.Parse(coords[4], CultureInfo.InvariantCulture), double.Parse(coords[5], CultureInfo.InvariantCulture)));
                }
            }
            return new CurvedPolygon(points.ToArray());
        }
    }
}