using Avalonia;
using Geometry;
using System;
using System.Collections.Generic;
using Avalonia.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Models
{

    public class GraphicLine : Line, IFigureGraphicProperties
    {
        public GraphicLine(Geometry.Point a, Geometry.Point b, Color color, double thickness) : base(a, b)
        {
            Color = color;
            Thickness = thickness;
        }

        public Color Color { get; }

        public double Thickness { get; }
    }

    public class GraphicEllipse : Ellipse, IFigureGraphicProperties
    {
        public GraphicEllipse(Geometry.Point c, double rx, double ry, Color color, double thickness) : base(c, rx, ry)
        {
            Color = color;
            Thickness = thickness;
        }

        public Color Color { get; }

        public double Thickness { get; }
    }

    public class GraphicPolygon : Polygon, IFigureGraphicProperties
    {
        public GraphicPolygon(ReadOnlySpan<Geometry.Point> Verts, Color color, double thickness) : base(Verts)
        {
            Color = color;
            Thickness = thickness;
        }

        public Color Color { get; }

        public double Thickness { get; }
    }
}
