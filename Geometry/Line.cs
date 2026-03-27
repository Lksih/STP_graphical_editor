using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    public class Line : IFigure
    {
        public Point Center {get; private set;}
        public ReadOnlySpan<Point> Vertex => VertArray; //Линия определяется этим полем
        private Point[] VertArray;

        public Line(Point a, Point b) //Но фактически для вызова класса нужно два экземпляра класса Point
        {
            VertArray = [a, b];
            Center = (a + b) / 2;
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

            VertArray[0] += d;
            VertArray[1] += d;

            Center = (VertArray[0] + VertArray[1]) / 2;
        }
        public void UpdateVertex(ReadOnlySpan<Point> NewVertex)
        {
            if (NewVertex.Length != 2)
            throw new IncorrectVertexSpan("Линия должна задаваться 2-мя точками");
            VertArray = NewVertex.ToArray();
            Center = (Vertex[0] + Vertex[1]) / 2;
        }
        public bool IsIn(Point p, double eps)
        {
            if (eps < 0)
            throw new IncorrectInaccuracyParameter();

            Point difpa = p - Vertex[0], difpb = p - Vertex[1];
            double normpa = Math.Sqrt((Math.Pow(difpa.X, 2) + Math.Pow(difpb.Y, 2))),
            normpb = Math.Sqrt((Math.Pow(difpb.X, 2) + Math.Pow(difpb.Y, 2)));
            if (normpa <= eps || normpb <= eps)
                return true;
            
            Point difba = Vertex[1] - Vertex[0];
            double normba = Math.Sqrt(Math.Pow(difba.X, 2) + Math.Pow(difba.Y, 2)), 
            cs1 = (difpa.X * difba.X + difpa.Y * difba.Y) / (normpa*normba), 
            cs2 = (difpb.X * (-difba.X) + difpb.Y * (-difba.Y)) / (normpb*normba);
            if (cs1 < 0 || cs2 < 0)
                return false;
            
            double h = Math.Abs(p.X * (Vertex[0].Y - Vertex[1].Y) + Vertex[0].X * (Vertex[1].Y - p.Y) + Vertex[1].X * (p.Y - Vertex[0].Y)) / normba;
            return h <= eps;
            
        }

    }
}