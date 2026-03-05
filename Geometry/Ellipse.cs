using System.Numerics;

namespace Geometry
{
    public class Ellipse : IFigure
    {
        public Point Center { get; set;}
        public ReadOnlySpan<Point> Vertex { get => []; }
        double Rx, Ry, Angle;

        public Ellipse(Point c, double rx, double ry)
        {
            Center = c;
            Rx = rx;
            Ry = ry;
        }
        public void Scale(double dx, double dy)
        {
            Rx *= dx;
            Ry *= dy;
        }
        public void RadialScale(double dr)
        {
            Rx *= dr;
            Ry *= dr;
        }
        public void Rotate(double angle)
        {
            Angle += angle;
        }
        public void Move(double dx, double dy)
        {
            Point d = new Point(dx, dy);
            Center.Addition(d);
        }
        public void UpdateVertex(ReadOnlySpan<Point> NewVertex) => throw new NullReferenceException();
        public IEnumerable<IDrawFigure> Draw() => throw new NullReferenceException();
        public bool IsIn(Point p, double eps)
        {
            Point dst = p - Center;
            double angle = Math.Atan2(dst.X, dst.Y) - Angle, r = Rx*Ry / Math.Sqrt(Math.Pow(Ry * Math.Cos(angle), 2) + Math.Pow(Rx * Math.Sin(angle), 2)), 
            distance = Math.Sqrt(Math.Pow(dst.X, 2) + Math.Pow(dst.Y, 2));
            return distance <= r + eps;
        }
    }
}