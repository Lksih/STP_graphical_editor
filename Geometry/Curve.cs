using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    public class Curve : IFigure
    {
        public Point Center { get; private set;}
        public ReadOnlySpan<Point> Vertex { get => CollectionsMarshal.AsSpan(VertList); }
        private List<Point> VertList;

        public Curve(ReadOnlySpan<Point> Verts)
        {
            if (Verts.ToArray().Count() != 3)
            throw new IncorrectVertexSpan("Кривая должна задаваться 3-мя вершинами");
            Center = new Point(0, 0);
            VertList = [.. Verts];
            foreach (var vert in VertList)
                Center.Addition(vert);
            Center.Multiply(1 / VertList.Count());
        }
        public void Scale(double dx, double dy)
        {
            if (dx == 0 || dy == 0)
            throw new IncorrectScaleParameter();
            for (int i = 0; i < VertList.Count(); i++)
            {
                Point dist = VertList[i] - Center;
                dist.X *= dx;
                dist.Y *= dy;
                VertList[i] = Center + dist;
            }
        }
        public void RadialScale(double dr)
        {
            if (dr == 0)
            throw new IncorrectScaleParameter();
            for (int i = 0; i < VertList.Count(); i++)
            {
                VertList[i] = Center + (VertList[i] - Center) * dr;
            }
        }
        public void Rotate(double angle)
        {
            for (int i = 0; i < VertList.Count(); i++)
            {
                Point vert = VertList[i];
                double tx = vert.X - Center.X, 
                ty = vert.Y - Center.Y, 
                currAngle = Math.Atan2(ty, tx), 
                distance = Math.Sqrt(Math.Pow(tx, 2) + Math.Pow(ty, 2));
                Point d = new Point(Math.Cos(angle + currAngle), Math.Sin(angle + currAngle));
                d.Multiply(distance);
                VertList[i] = Center + d;
            }
        }
        public void Move(double dx, double dy)
        {
            Point d = new Point(dx, dy);
            for (int i = 0; i < VertList.Count(); i++)
                VertList[i].Addition(d);
            Center.Addition(d);
        }
        public void UpdateVertex(ReadOnlySpan<Point> NewVertex)
        {
            if (NewVertex.ToArray().Count() != 3)
            throw new IncorrectVertexSpan("Кривая должна задаваться 3-мя вершинами");
            VertList = [.. NewVertex];
            foreach (var vert in VertList)
                Center.Addition(vert);
            Center.Multiply(1 / VertList.Count());
        }
        public IEnumerable<IDrawFigure> Draw() => throw new NullReferenceException();
        public bool IsIn(Point p, double eps) // через аппроксимацию отрезками
        {
            if (eps < 0)
                throw new IncorrectInaccuracyParameter();

            
            Point p0 = VertList[0];
            Point p1 = VertList[1];
            Point p2 = VertList[2];

            
            foreach (var v in VertList)
            {
                double dx0 = p.X - v.X;
                double dy0 = p.Y - v.Y;
                if (Math.Sqrt(dx0 * dx0 + dy0 * dy0) <= eps)
                    return true;
            }

            
            double len01 = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2));
            double len12 = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            double approxLen = len01 + len12;

            int segments = (int)(approxLen / (eps > 0 ? eps : 1e-6));
            if (segments < 8) segments = 8;
            if (segments > 256) segments = 256;

            
            Point prev = p0;
            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                double oneMinusT = 1.0 - t;

                Point curr = new Point(
                    oneMinusT * oneMinusT * p0.X + 2 * oneMinusT * t * p1.X + t * t * p2.X,
                    oneMinusT * oneMinusT * p0.Y + 2 * oneMinusT * t * p1.Y + t * t * p2.Y
                );

                if (IsPointNearSegment(p, prev, curr, eps))
                    return true;

                prev = curr;
            }

            return false;
        }

        private static bool IsPointNearSegment(Point p, Point a, Point b, double eps)
        {
            

            
            if (a.X == b.X)
            {
                return Math.Abs(p.X - a.X) <= eps &&
                       Math.Min(a.Y, b.Y) - eps <= p.Y &&
                       p.Y <= Math.Max(a.Y, b.Y) + eps;
            }
            
            else if (a.Y == b.Y)
            {
                return Math.Abs(p.Y - a.Y) <= eps &&
                       Math.Min(a.X, b.X) - eps <= p.X &&
                       p.X <= Math.Max(a.X, b.X) + eps;
            }
            else
            {
                double t1 = (p.X - a.X) / (b.X - a.X);
                double t2 = (p.Y - a.Y) / (b.Y - a.Y);
                double lenght = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

                return Math.Abs(t1 - t2) * lenght <= 2 * eps &&
                       (Math.Max(t2, t1) - 1) * lenght <= eps &&
                       Math.Min(t2, t1) * lenght >= -eps;
            }
        }
    }
}
