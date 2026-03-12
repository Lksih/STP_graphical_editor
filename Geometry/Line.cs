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
            VertArray = [a, b];
        }
        public void Scale(double dx, double dy)
        {
            if (dx == 0 || dy == 0)
            throw new IncorrectScaleParameter();
            for (int i = 0; i < VertArray.Length; i++)
            {
                Point dist = VertArray[i] - Center;
                dist.X *= dx;
                dist.Y *= dy;
                VertArray[i] = Center + dist;
            }
        }
        public void RadialScale(double dr)
        {
            if (dr == 0)
            throw new IncorrectScaleParameter();
            for (int i = 0; i < VertArray.Length; i++)
            {
                VertArray[i] = Center + (VertArray[i] - Center) * dr;
            }
        }
        public void Rotate(double angle)
        {
            for (int i = 0; i < VertArray.Length; i++)
            {
                double x = VertArray[i].X * Math.Cos(angle) - VertArray[i].Y * Math.Sin(angle),
                y = VertArray[i].X * Math.Sin(angle) + VertArray[i].Y * Math.Cos(angle);
                VertArray[i] = new Point(x, y);
            }
        }
        public void Move(double dx, double dy)
        {
            Point d = new Point(dx, dy);
            for (int i = 0; i < VertArray.Length; i++)
                VertArray[i]+=d;
        }
        public void UpdateVertex(ReadOnlySpan<Point> NewVertex)
        {
            if (NewVertex.Length != 2)
            throw new IncorrectVertexSpan("Линия должна задаваться 2-мя точками");
            VertArray = NewVertex.ToArray();
        }
        public IEnumerable<IDrawFigure> Draw() => throw new NotImplementedException();
        public bool IsIn(Point p, double eps)
        {
            if (eps < 0)
            throw new IncorrectInaccuracyParameter();
            if (Vertex[0].X == Vertex[1].X)
            {
                return Math.Abs(p.X - Vertex[0].X) <= eps && 
                Math.Min(Vertex[0].Y, Vertex[1].Y) - eps <= p.Y && 
                p.Y <= Math.Max(Vertex[0].Y, Vertex[1].Y) - eps;
            }
            else if (Vertex[0].Y == Vertex[1].Y)
            {
                return Math.Abs(p.Y - Vertex[0].Y) <= eps && 
                Math.Min(Vertex[0].X, Vertex[1].X) - eps <= p.X && 
                p.X <= Math.Max(Vertex[0].X, Vertex[1].X) - eps;
            }
            else
            {
                double t1 = (p.X - Vertex[0].X) / (Vertex[1].X - Vertex[0].X), 
                t2 = (p.Y - Vertex[0].Y) / (Vertex[1].Y - Vertex[0].Y), 
                lenght = Math.Sqrt(Math.Pow(Vertex[0].X - Vertex[1].X, 2) + Math.Pow(Vertex[0].Y - Vertex[1].Y, 2));
                return Math.Abs(t1 - t2) * lenght <= 2 * eps && 
                (Math.Max(t2, t1) - 1) * lenght <= eps && 
                Math.Min(t2, t1) * lenght >= -eps;
            }
        }

    }
}