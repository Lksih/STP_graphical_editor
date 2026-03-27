using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    public class Curve : IFigure
    {
        public Point Center { get; private set;}
        public ReadOnlySpan<Point> Vertex => VertArray; //Кривая задаётся этим полем
        private Point[] VertArray;

        public Curve(ReadOnlySpan<Point> Verts) //Экземпляр класса тоже вызывается им
        {
            if (Verts.Length != 3) //В спане должно быть ровно 3 точки
            throw new IncorrectVertexSpan("Кривая должна задаваться 3-мя вершинами");
            Center = new Point(0, 0);
            VertArray = [.. Verts];
            foreach (var vert in VertArray)
                Center += vert;
            Center *= 1.0 / VertArray.Length;
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
                double cnstX = 1e-15, cnstY = 1e-15;
                while (VertArray[i].X == Center.X && dist.X != 0)
                    {
                        VertArray[i].X += dist.X > 0 ? cnstX : -cnstX;
                        cnstX *= 10;
                    }
                while (VertArray[i].Y == Center.Y && dist.Y != 0)
                    {
                        VertArray[i].Y += dist.Y > 0 ? cnstY : -cnstY;
                        cnstY *= 10;
                    }
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
                Point dst = VertArray[i] - Center;
                double x = dst.X * Math.Cos(angle) - dst.Y * Math.Sin(angle),
                y = dst.X * Math.Sin(angle) + dst.Y * Math.Cos(angle);
                VertArray[i] = Center + new Point(x, y);
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
            if (NewVertex.Length != 3)
            throw new IncorrectVertexSpan("Кривая должна задаваться 3-мя вершинами");
            VertArray = [.. NewVertex];
            Center = new Point(0, 0);
            foreach (var vert in VertArray)
                Center += vert;
            Center *= 1.0 / VertArray.Length;
        }

        public bool IsIn(Point p, double eps) // через аппроксимацию отрезками
        {
            if (eps < 0)
                throw new IncorrectInaccuracyParameter();

            
            Point p0 = VertArray[0];
            Point p1 = VertArray[1];
            Point p2 = VertArray[2];

            
            foreach (var v in VertArray)
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
            if (eps < 0)
            throw new IncorrectInaccuracyParameter();

            Point difpa = p - a, difpb = p - b;
            double normpa = Math.Sqrt((Math.Pow(difpa.X, 2) + Math.Pow(difpa.Y, 2))),
            normpb = Math.Sqrt((Math.Pow(difpb.X, 2) + Math.Pow(difpb.Y, 2)));
            if (normpa <= eps || normpb <= eps)
                return true;
            
            Point difba = b - a;
            double normba = Math.Sqrt(Math.Pow(difba.X, 2) + Math.Pow(difba.Y, 2)), 
            cs1 = (difpa.X * difba.Y + difpa.Y * difba.X) / (normpa*normba), 
            cs2 = (difpb.X * (-difba.Y) + difpb.Y * (-difba.X)) / (normpb*normba);
            if (cs1 < 0 || cs2 < 0)
                return false;
            
            double h = Math.Abs(p.X * (a.Y - b.Y) + a.X * (b.Y - p.Y) + b.X * (p.Y - a.Y)) / normba;
            return h <= eps;
        }
    }
}
