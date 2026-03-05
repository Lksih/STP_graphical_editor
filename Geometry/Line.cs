using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    public class Line : IFigure
    {
        public Point Center { get => (VertArray[0] + VertArray[1]) * 0.5;}
        public ReadOnlySpan<Point> Vertex { get => VertArray; }
        private Point[] VertArray;

        public Line(Point a, Point b)
        {
            VertArray = new Point[2];
            VertArray.Append(a);
            VertArray.Append(b);

        }
        public void Scale(double dx, double dy)
        {
            for (int i = 0; i < VertArray.Count(); i++)
            {
                Point dist = VertArray[i] - Center;
                dist.X *= dx;
                dist.Y *= dy;
                VertArray[i] = Center + dist;
            }
        }
        public void RadialScale(double dr)
        {
            for (int i = 0; i < VertArray.Count(); i++)
            {
                VertArray[i] = Center + (VertArray[i] - Center) * dr;
            }
        }
        public void Rotate(double angle)
        {
            for (int i = 0; i < VertArray.Count(); i++)
            {
                Point vert = VertArray[i];
                double tx = vert.X - Center.X, ty = vert.Y - Center.Y, currAngle = Math.Atan2(ty, tx), distance = Math.Sqrt(Math.Pow(tx, 2) + Math.Pow(ty, 2));
                Point d = new Point(Math.Cos(angle + currAngle), Math.Sin(angle + currAngle));
                d.Multiply(distance);
                VertArray[i] = Center + d;
            }
        }
        public void Move(double dx, double dy)
        {
            Point d = new Point(dx, dy);
            for (int i = 0; i < VertArray.Count(); i++)
                VertArray[i].Addition(d);
            Center.Addition(d);
        }
        public void UpdateVertex(ReadOnlySpan<Point> NewVertex)
        {
            VertArray = NewVertex.ToArray();
        }
        public IEnumerable<IDrawFigure> Draw() => throw new NullReferenceException();
        public bool IsIn(Point p, double eps)
        {
            if (Vertex[0].X == Vertex[1].X)
            {
                return Math.Abs(p.X - Vertex[0].X) <= eps && Math.Min(Vertex[0].Y, Vertex[1].Y) - eps <= p.Y && p.Y <= Math.Max(Vertex[0].Y, Vertex[1].Y) - eps;
            }
            else if (Vertex[0].Y == Vertex[1].Y)
            {
                return Math.Abs(p.Y - Vertex[0].Y) <= eps && Math.Min(Vertex[0].X, Vertex[1].X) - eps <= p.X && p.X <= Math.Max(Vertex[0].X, Vertex[1].X) - eps;
            }
            else
            {
                double t1 = (p.X - Vertex[0].X) / (Vertex[1].X - Vertex[0].X), t2 = (p.Y - Vertex[0].Y) / (Vertex[1].Y - Vertex[0].Y), lenght = Math.Sqrt(Math.Pow(Vertex[0].X - Vertex[1].X, 2) + Math.Pow(Vertex[0].Y - Vertex[1].Y, 2));
                return Math.Abs(t1 - t2) * lenght <= 2 * eps && (Math.Max(t2, t1) - 1) * lenght <= eps && Math.Min(t2, t1) * lenght >= -eps;
            }
        }
    }
}