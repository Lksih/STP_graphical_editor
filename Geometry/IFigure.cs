using System.Drawing;
using System.Dynamic;
using System.Numerics;

namespace Geometry
{
    public class Point{
        public double X {get; set;} 
        public double Y {get; set;}

        public Point(double x, double y) //При реализации других классов нужно 
        {                                //создавать экземпляры этого класса
            X = x;                       //Он определяется полями X и Y
            Y = y;                       //То есть двумя координатами
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
        public static double operator *(Point a, Point b)
        {
            return a.X * b.X + a.Y * b.Y;
        }
        };

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
    public class IncorrectScaleParameter : Exception
        {
            public IncorrectScaleParameter() : base("Параметр масштабирования не должен быть равен 0") { }
        }
    public class IncorrectInaccuracyParameter : Exception
        {
            public IncorrectInaccuracyParameter() : base("Параметр погрешности должен быть неотрицательным") { }
        }
    public class IncorrectVertexSpan : Exception
        {
            public IncorrectVertexSpan(string message) : base(message) { }
        }
}
