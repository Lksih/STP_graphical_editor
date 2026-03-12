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
        public bool IsIn(Point p, double eps)
        {
            throw new NullReferenceException();
            /*if (eps < 0)
            throw new IncorrectInaccuracyParameter();
            for (int i = 0; i < VertList.Count - 1; i++)
                {
                    if (VertList[i].X == VertList[i + 1].X)
                    {
                    if (Math.Abs(p.X - VertList[i].X) <= eps && 
                    Math.Min(VertList[i].Y, VertList[i + 1].Y) - eps <= p.Y && 
                    p.Y <= Math.Max(VertList[i].Y, VertList[i + 1].Y) - eps)
                        return true;
                    }
                    else if (VertList[i].Y == Vertex[1].Y)
                    {
                    if (Math.Abs(p.Y - VertList[i].Y) <= eps && 
                    Math.Min(VertList[i].X, VertList[i + 1].X) - eps <= p.X && 
                    p.X <= Math.Max(VertList[i].X, VertList[i + 1].X) - eps)
                        return true;
                    }
                    else
                    {
                    double t1 = (p.X - VertList[i].X) / (VertList[i + 1].X - VertList[i].X), 
                    t2 = (p.Y - VertList[i].Y) / (VertList[i + 1].Y - VertList[i].Y), 
                    lenght = Math.Sqrt(Math.Pow(VertList[i].X - VertList[i + 1].X, 2) + Math.Pow(VertList[i].Y - VertList[i + 1].Y, 2));
                    if (Math.Abs(t1 - t2) * lenght <= 2 * eps && 
                    (Math.Max(t2, t1) - 1) * lenght <= eps && 
                    Math.Min(t2, t1) * lenght >= -eps)
                        return true;
                    }
                }
                if (VertList[0].X == VertList[VertList.Count() - 1].X)
                    {
                    return Math.Abs(p.X - VertList[0].X) <= eps && 
                    Math.Min(VertList[0].Y, VertList[VertList.Count() - 1].Y) - eps <= p.Y && 
                    p.Y <= Math.Max(VertList[0].Y, VertList[VertList.Count() - 1].Y) - eps;
                    }
                    else if (VertList[0].Y == Vertex[VertList.Count() - 1].Y)
                    {
                    return Math.Abs(p.Y - VertList[0].Y) <= eps && 
                    Math.Min(VertList[0].X, VertList[VertList.Count() - 1].X) - eps <= p.X && 
                    p.X <= Math.Max(VertList[0].X, VertList[VertList.Count() - 1].X) - eps;
                    }
                    else
                    {
                    double t1 = (p.X - VertList[0].X) / (VertList[VertList.Count() - 1].X - VertList[0].X), 
                    t2 = (p.Y - VertList[0].Y) / (VertList[VertList.Count() - 1].Y - VertList[0].Y), 
                    lenght = Math.Sqrt(Math.Pow(VertList[0].X - VertList[VertList.Count() - 1].X, 2) + Math.Pow(VertList[0].Y - VertList[VertList.Count() - 1].Y, 2));
                    return Math.Abs(t1 - t2) * lenght <= 2 * eps && 
                    (Math.Max(t2, t1) - 1) * lenght <= eps && Math.Min(t2, t1) * lenght >= -eps;
                    }*/
        }
    }
}