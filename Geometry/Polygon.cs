using System.Numerics;

namespace Geometry
{
    class Polygon : IFigure
    {
        public Point Center { get; }
        public List<Point> Vertex { get; }

        public Polygon()
        {
            Center = new Point();
            Vertex = new List<Point>{};
        }
        public void AddPoint(double x, double y)
        {
            Point point = new Point();
            point.X = x;
            point.Y = y;
            Vertex.Add(point);
            double sumx = 0, sumy = 0;
            foreach (var vert in Vertex)
            {
                sumx += vert.X;
                sumy += vert.Y;
            }
            //Дописать алгоритм поиска центроида(Вариант - алгоритм Вайсфельда)
        }
        public void Scale(double dx, double dy)
        {
            foreach (var vert in Vertex)
            {
                vert.X = Center.X + (vert.X - Center.X) * dx;
                vert.Y = Center.Y + (vert.Y - Center.Y) * dy;
            }
        }
        public void RadialScale(double dr)
        {
            foreach (var vert in Vertex)
            {
                vert.X = Center.X + (vert.X - Center.X) * dr;
                vert.Y = Center.Y + (vert.Y - Center.Y) * dr;
            }
        }
        public void Rotate(double angle)
        {
            foreach (var vert in Vertex)
            {
                double tx = vert.X - Center.X, ty = vert.Y - Center.Y, currAngle = Math.Atan2(ty, tx), distance = Math.Sqrt(Math.Pow(tx, 2) + Math.Pow(ty, 2));
                vert.X = Center.X + distance * Math.Cos(angle + currAngle);
                vert.Y = Center.Y + distance * Math.Sin(angle + currAngle);
            }
        }
        public void Move(double dx, double dy)
        {
            foreach (var vert in Vertex)
            {
                vert.X += dx;
                vert.Y += dy;
            }
            Center.X += dx;
            Center.Y += dy;
        }
        public void UpdateVertex(int index, double x, double y)
        {
            Vertex[index].X = x;
            Vertex[index].Y = y;
            Center.X = Math.Min(Vertex[0].X, Vertex[1].X) + (Math.Max(Vertex[0].X, Vertex[1].X) - Math.Min(Vertex[0].X, Vertex[1].X)) / 2.0; 
            Center.Y = Math.Min(Vertex[0].Y, Vertex[1].Y) + (Math.Max(Vertex[0].Y, Vertex[1].Y) - Math.Min(Vertex[0].Y, Vertex[1].Y)) / 2.0;
            //Также дописать алгоритм поиска центроида
        }
        public IEnumerable<IDrawFigure> Draw()
        {
            
        }
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
            //Реализовать алгоритм попадания точки с использованием псевдонормалей
        }
    }
}