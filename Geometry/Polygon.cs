using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    public class Polygon : IFigure
    {
        public Point Center { get; private set;}
        public ReadOnlySpan<Point> Vertex { get => VertArray; }
        private Point[] VertArray;

        public Polygon(ReadOnlySpan<Point> Verts)
        {
            if (Verts.Length < 3)
            throw new IncorrectVertexSpan("Количество точек в фигуре должно быть  не меньше 3-х");
            Center = new Point(0, 0);
            VertArray = [.. Verts];
            foreach (var vert in VertArray)
                Center += vert;
            Center *= 1 / VertArray.Length;
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
                Point vert = VertArray[i];
                double tx = vert.X - Center.X, 
                ty = vert.Y - Center.Y, 
                currAngle = Math.Atan2(ty, tx), 
                distance = Math.Sqrt(Math.Pow(tx, 2) + Math.Pow(ty, 2));
                Point d = new Point(Math.Cos(angle + currAngle), Math.Sin(angle + currAngle));
                d *= distance;
                VertArray[i] = Center + d;
            }
        }
        public void Move(double dx, double dy)
        {
            Point d = new Point(dx, dy);
            for (int i = 0; i < VertArray.Length; i++)
                VertArray[i] += d;
            Center += d;
        }
        public void UpdateVertex(ReadOnlySpan<Point> NewVertex)
        {
            if (NewVertex.Length < 3)
            throw new IncorrectVertexSpan("Количество точек в фигуре должно быть  не меньше 3-х");
            VertArray = [.. NewVertex];
            Center = new Point(0, 0);
            foreach (var vert in VertArray)
                Center += vert;
            Center *= 1 / VertArray.Length;
        }
        public IEnumerable<IDrawFigure> Draw() => throw new NotImplementedException();
        public bool IsIn(Point p, double eps)
        {
            if (eps < 0)
            throw new IncorrectInaccuracyParameter();
            double sgn = 0, sgn2 = 0, summ = 0;
            Point dif1 = VertArray[0] - p, dif2 = VertArray[VertArray.Length - 1] - p;
            if (dif1.X == 0 && dif1.Y == 0 || dif2.X == 0 && dif2.Y == 0)
                return true;
            double angle = Math.Acos((dif1.X * dif2.Y + dif1.Y * dif2.X) / 
            Math.Sqrt((Math.Pow(dif1.X, 2) + Math.Pow(dif1.Y, 2))*(Math.Pow(dif2.X, 2) + Math.Pow(dif2.Y, 2))));
            summ += angle;
            double pseudonorm = dif1.X * dif2.Y - dif1.Y * dif2.X;
            if (pseudonorm != 0)
                sgn = pseudonorm / Math.Abs(pseudonorm);
            for (int i = 0; i < VertArray.Length - 1; i++)
            {
                dif1 = VertArray[i] - p;
                dif2 = VertArray[i + 1] - p;
                if (dif1.X == 0 && dif1.Y == 0 || dif2.X == 0 && dif2.Y == 0)
                    return true;
                angle = Math.Acos((dif1.X * dif2.Y + dif1.Y * dif2.X) / 
                Math.Sqrt((Math.Pow(dif1.X, 2) + Math.Pow(dif1.Y, 2))*(Math.Pow(dif2.X, 2) + Math.Pow(dif2.Y, 2))));
                pseudonorm = dif1.X * dif2.Y - dif1.Y * dif2.X;
                if (pseudonorm != 0)
                {
                    sgn2 = pseudonorm / Math.Abs(pseudonorm);
                    if (sgn == 0)
                        sgn = sgn2;
                    else
                        angle *= sgn2 * sgn;
                }
                summ += angle;
            }
            
            if (Math.Round(summ, 14) == Math.Round(Math.PI, 14))
                return true;
            else
            {
                for (int i = 0; i < VertArray.Length - 1; i++)
                {
                    if (VertArray[i].X == VertArray[i + 1].X)
                    {
                    if (Math.Abs(p.X - VertArray[i].X) <= eps && 
                    Math.Min(VertArray[i].Y, VertArray[i + 1].Y) - eps <= p.Y && 
                    p.Y <= Math.Max(VertArray[i].Y, VertArray[i + 1].Y) - eps)
                        return true;
                    }
                    else if (VertArray[i].Y == VertArray[1].Y)
                    {
                    if (Math.Abs(p.Y - VertArray[i].Y) <= eps && 
                    Math.Min(VertArray[i].X, VertArray[i + 1].X) - eps <= p.X && 
                    p.X <= Math.Max(VertArray[i].X, VertArray[i + 1].X) - eps)
                        return true;
                    }
                    else
                    {
                    double t1 = (p.X -VertArray[i].X) / (VertArray[i + 1].X - VertArray[i].X), 
                    t2 = (p.Y - VertArray[i].Y) / (VertArray[i + 1].Y - VertArray[i].Y), 
                    lenght = Math.Sqrt(Math.Pow(VertArray[i].X - VertArray[i + 1].X, 2) + Math.Pow(VertArray[i].Y - VertArray[i + 1].Y, 2));
                    if (Math.Abs(t1 - t2) * lenght <= 2 * eps && (Math.Max(t2, t1) - 1) * lenght <= eps && Math.Min(t2, t1) * lenght >= -eps)
                        return true;
                    }
                }
                if (VertArray[0].X == VertArray[VertArray.Length - 1].X)
                    {
                    return Math.Abs(p.X - VertArray[0].X) <= eps && 
                    Math.Min(VertArray[0].Y, VertArray[VertArray.Length - 1].Y) - eps <= p.Y && 
                    p.Y <= Math.Max(VertArray[0].Y, VertArray[VertArray.Length - 1].Y) - eps;
                    }
                    else if (VertArray[0].Y == Vertex[VertArray.Length - 1].Y)
                    {
                    return Math.Abs(p.Y - VertArray[0].Y) <= eps && 
                    Math.Min(VertArray[0].X, VertArray[VertArray.Length - 1].X) - eps <= p.X && 
                    p.X <= Math.Max(VertArray[0].X, VertArray[VertArray.Length - 1].X) - eps;
                    }
                    else
                    {
                    double t1 = (p.X - VertArray[0].X) / (VertArray[VertArray.Length - 1].X - VertArray[0].X), 
                    t2 = (p.Y - VertArray[0].Y) / (VertArray[VertArray.Length - 1].Y - VertArray[0].Y), 
                    lenght = Math.Sqrt(Math.Pow(VertArray[0].X - VertArray[VertArray.Length - 1].X, 2) + 
                    Math.Pow(VertArray[0].Y - VertArray[VertArray.Length - 1].Y, 2));
                    return Math.Abs(t1 - t2) * lenght <= 2 * eps && 
                    (Math.Max(t2, t1) - 1) * lenght <= eps && Math.Min(t2, t1) * lenght >= -eps;
                    }
            }
        }
    }
}