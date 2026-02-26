using System.Numerics;

namespace Geometry
{
    class Ellips : IFigure
    {
        public Point Center { get; }
        public List<Point> Vertex { get; }

        private double RadiusA, RadiusB, Angle;

        public Ellips(double x, double y, double ra, double rb)
        {
            Center = new Point();
            Center.X = x; 
            Center.Y = y;
            Vertex = new List<Point>{};
            RadiusA = ra;
            RadiusB = rb;
            Angle = 0;
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
            RadiusA *= dr;
            RadiusB *= dr;
        }
        public void Rotate(double angle)
        {
            Angle += angle;
        }
        public void Move(double dx, double dy)
        {
            Center.X += dx;
            Center.Y += dy;
        }
        public void UpdateVertex(double dra, double drb)
        {
            RadiusA += dra;
            RadiusB += drb;
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
        }
    }
}