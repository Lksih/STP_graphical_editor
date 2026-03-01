using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    class Polygon : IFigure
    {
        public Point Center { get; private set;}
        public ReadOnlySpan<Point> Vertex { get => CollectionsMarshal.AsSpan(VertList); }
        private List<Point> VertList;

        public Polygon(ReadOnlySpan<Point> Verts)
        {
            Center = new Point(0, 0);
            VertList = [.. Verts];
            foreach (var vert in VertList)
                Center.Addition(vert);
            Center.Multiply(1 / VertList.Count());
        }
        public void Scale(double dx, double dy)
        {
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
                double tx = vert.X - Center.X, ty = vert.Y - Center.Y, currAngle = Math.Atan2(ty, tx), distance = Math.Sqrt(Math.Pow(tx, 2) + Math.Pow(ty, 2));
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
            VertList = [.. NewVertex];
            foreach (var vert in VertList)
                Center.Addition(vert);
            Center.Multiply(1 / VertList.Count());
        }
        public IEnumerable<IDrawFigure> Draw() => throw new NullReferenceException();
        public bool IsIn(Point p, double eps)
        {
            throw new NullReferenceException();
        }
    }
}