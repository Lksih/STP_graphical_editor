using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    public class Line : IFigure
    {
        public Point Center => (VertArray[0] + VertArray[1]) * 0.5;
        public ReadOnlySpan<Point> Vertex => VertArray; //Линия определяется этим полем
        private Point[] VertArray;

        public Line(Point a, Point b) //Но фактически для вызова класса нужно два экземпляра класса Point
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

            Point difp1 = p - Vertex[0], difp2 = p - Vertex[1];
            double normp1 = Math.Sqrt((Math.Pow(difp1.X, 2) + Math.Pow(difp1.Y, 2))),
            normp2 = Math.Sqrt((Math.Pow(difp2.X, 2) + Math.Pow(difp2.Y, 2)));
            if (normp1 <= eps || normp2 <= eps)
                return true;
            
            Point dif12 = Vertex[1] - Vertex[0];
            double norm12 = Math.Sqrt(Math.Pow(dif12.X, 2) + Math.Pow(dif12.Y, 2)), 
            cs1 = (difp1.X * dif12.Y + difp1.Y * dif12.X) / (normp1*norm12), 
            cs2 = (difp2.X * (-dif12.Y) + difp2.Y * (-dif12.X)) / (normp2*norm12);
            if (cs1 < 0 || cs2 < 0)
                return false;
            
            double h = Math.Abs(p.X * (Vertex[0].Y - Vertex[1].Y) + Vertex[0].X * (Vertex[1].Y - p.Y) + Vertex[1].X * (p.Y - Vertex[0].Y)) / norm12;
            return h <= eps;
            
        }

    }
}