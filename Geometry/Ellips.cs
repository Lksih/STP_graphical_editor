using System.Numerics;

namespace Geometry
{
    public class Ellips : IFigure
    {
        public Point Center { get; set;} //Эллипс задаётся своим центром
        public ReadOnlySpan<Point> Vertex => []; 
        double Rx, Ry, Angle; //А также радиусами по X и Y

        public Ellips(Point c, double rx, double ry) //Соответственно, одна точка и два числа
        {
            Center = c;
            Rx = rx;
            Ry = ry;
            Angle = 0;
        }
        public void Scale(double dx, double dy)
        {
            if (dx == 0 || dy == 0)
            throw new IncorrectScaleParameter();
            Rx *= dx;
            Ry *= dy;
        }
        public void RadialScale(double dr)
        {
            if (dr == 0)
            throw new IncorrectScaleParameter();
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
            Center += d;
        }
        public void UpdateVertex(ReadOnlySpan<Point> NewVertex)
        {
            if (!NewVertex.IsEmpty)
            throw new IncorrectVertexSpan("Эллипс не имеет вершин");
        }
        public IEnumerable<IDrawFigure> Draw() => throw new NotImplementedException();
        public bool IsIn(Point p, double eps)
        {
            if (eps < 0)
            throw new IncorrectInaccuracyParameter();
            Point dst = p - Center;
            double x = dst.X * Math.Cos(-Angle) - dst.Y * Math.Sin(-Angle),
                y = dst.X * Math.Sin(-Angle) + dst.Y * Math.Cos(-Angle);
            dst = new Point(x, y);
            double angle = Math.Atan2(dst.X, dst.Y), r = Rx*Ry / Math.Sqrt(Math.Pow(Ry * Math.Cos(angle), 2) + Math.Pow(Rx * Math.Sin(angle), 2)), 
            distance = Math.Sqrt(Math.Pow(dst.X, 2) + Math.Pow(dst.Y, 2));
            return distance <= r + eps;
        }
    }
}