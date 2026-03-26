using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    public class CurvedPolygon : IFigure
    {
        public Point Center { get; private set; }

        public ReadOnlySpan<Point> Vertex => VertArray; //Класс восстанавливается через набор точек

        private Point[] VertArray;

        public CurvedPolygon(ReadOnlySpan<Point> verts)
        {
            if (verts.Length < 4 || verts.Length % 2 != 0)
                throw new IncorrectVertexSpan("Криволинейный многоугольник должен задаваться хотя бы 2 кривыми (4 точки), количество точек кратно 2.");

            VertArray = verts.ToArray();
            Center = new Point(0, 0);
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

        public void UpdateVertex(ReadOnlySpan<Point> newVertex)
        {
            if (newVertex.Length < 4 || newVertex.Length % 2 != 0)
                throw new IncorrectVertexSpan("Криволинейный многоугольник должен задаваться хотя бы 2 кривыми (4 точки), количество точек кратно 2.");

            VertArray = newVertex.ToArray();
            Center = new Point(0, 0);
            foreach (var vert in VertArray)
                Center += vert;
            Center *= 1.0 / VertArray.Length;
        }

        public IEnumerable<IDrawFigure> Draw() => throw new NotImplementedException();

        public bool IsIn(Point p, double eps)
        {
            if (eps < 0)
                throw new IncorrectInaccuracyParameter();

            var points = new List<Point>();
            Point p0, p1, p2;
            double len01, len12, approxLen;
            int segments;
            for (int i = 0; i < Vertex.Length - 2; i += 3)
            {
                p0 = Vertex[i];
                p1 = Vertex[i + 1];
                p2 = Vertex[i + 2];

                len01 = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2));
                len12 = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                approxLen = len01 + len12;

                segments = (int)(approxLen / (eps > 0 ? eps : 1e-6));
                if (segments < 8) segments = 8;
                if (segments > 256) segments = 256;

                for (int j = 0; j <= segments; j++)
                {
                    // антидубляция крайних точек (кроме первой)
                    if (points.Count > 0 && j == 0)
                        continue;

                    double t = (double)j / segments;
                    double oneMinusT = 1.0 - t;

                    Point curr = new Point(
                        oneMinusT * oneMinusT * p0.X + 2 * oneMinusT * t * p1.X + t * t * p2.X,
                        oneMinusT * oneMinusT * p0.Y + 2 * oneMinusT * t * p1.Y + t * t * p2.Y
                    );

                    points.Add(curr);
                }
            }
            p0 = Vertex[Vertex.Length - 2];
            p1 = Vertex[Vertex.Length - 1];
            p2 = Vertex[0];

            len01 = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2));
            len12 = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            approxLen = len01 + len12;

            segments = (int)(approxLen / (eps > 0 ? eps : 1e-6));
            if (segments < 8) segments = 8;
            if (segments > 256) segments = 256;

                for (int j = 0; j <= segments; j++)
                {
                    // антидубляция крайних точек (кроме первой)
                    if (points.Count > 0 && j == 0)
                        continue;

                    double t = (double)j / segments;
                    double oneMinusT = 1.0 - t;

                    Point curr = new Point(
                        oneMinusT * oneMinusT * p0.X + 2 * oneMinusT * t * p1.X + t * t * p2.X,
                        oneMinusT * oneMinusT * p0.Y + 2 * oneMinusT * t * p1.Y + t * t * p2.Y
                    );

                    points.Add(curr);
                }
            if (points.Count < 3)
                return false;

            var polygon = new Polygon(CollectionsMarshal.AsSpan(points));
            return polygon.IsIn(p, eps);
        }

    }
}

