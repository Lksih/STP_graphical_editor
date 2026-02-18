using System.Drawing;

namespace Geometry
{
    public record Point(double X, double Y);

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
        ReadOnlySpan<Point> Vertex { get; }
        void Scale(double dx, double dy); // можно отрицательные для отражения
        void RadialScale(double dr);
        void Rotate(double angle);
        void Move(double dx, double dy);
        void UpdateVertex(ReadOnlySpan<Point> Vertex);
        IEnumerable<IDrawFigure> Draw();
        bool IsIn(Point p, double eps);
    }
}
