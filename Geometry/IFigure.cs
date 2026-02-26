using System.Drawing;
using System.Dynamic;
using System.Numerics;

namespace Geometry
{
    public class Point{public double X {get; set;} public double Y {get; set;}};

    public interface IFigureGraphicProperties
    {
        Color Color { get; }
        double Thickness { get; }
    }

    public interface IDrawFigure
    {

    }

    public interface IFigure
    {
        Point Center { get; }
        List<Point> Vertex { get; }
        void Scale(double dx, double dy); // можно отрицательные для отражения
        void RadialScale(double dr);
        void Rotate(double angle);
        void Move(double dx, double dy);
        void UpdateVertex(ReadOnlySpan<Point> Vertex);
        IEnumerable<IDrawFigure> Draw();
        bool IsIn(Point p, double eps);
    }
}
