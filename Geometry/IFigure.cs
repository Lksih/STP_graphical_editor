using System.Drawing;
using System.Dynamic;
using System.Numerics;

namespace Geometry
{
    public class Point{
        public double X {get; set;} 
        public double Y {get; set;}

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
        public void Addition(Point a)
        {
            X += a.X;
            Y += a.Y;
        }
        public void Substraction(Point a)
        {
            X -= a.X;
            Y -= a.Y;
        }
        public void Multiply(double c)
        {
            X = X * c;
            Y = Y * c;
        }
        public static Point operator +(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y+b.Y);
        }
        public static Point operator -(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y-b.Y);
        }
        public static Point operator *(Point a, double c)
        {
            return new Point(a.X * c, a.Y*c);
        }
        };

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
