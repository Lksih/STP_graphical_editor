using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    public class Polygon : IFigure
    {
        public Point Center { get; private set;}
        public ReadOnlySpan<Point> Vertex => VertArray; //Полигон задаётся этим полем
        private Point[] VertArray;

        public Polygon(ReadOnlySpan<Point> Verts) //Оно же используется для создания класса
        {
            if (Verts.Length < 3) //Точек в спане должно быть 3 или больше
            throw new IncorrectVertexSpan("Количество точек в фигуре должно быть  не меньше 3-х"); 
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
            if (NewVertex.Length < 3)
            throw new IncorrectVertexSpan("Количество точек в фигуре должно быть  не меньше 3-х");
            VertArray = [.. NewVertex];
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

            double sgn = 0, sgn2 = 0, summ = 0;
            Point dif1 = VertArray[0] - p, dif2 = VertArray[VertArray.Length - 1] - p;
            if (dif1.X == 0 && dif1.Y == 0 || dif2.X == 0 && dif2.Y == 0)
                return true;
            double angle = Math.Acos((dif1.X * dif2.X + dif1.Y * dif2.Y) / 
            Math.Sqrt((Math.Pow(dif1.X, 2) + Math.Pow(dif1.Y, 2))*(Math.Pow(dif2.X, 2) + Math.Pow(dif2.Y, 2))));
            summ += angle;
            double pseudonorm = dif1.X * dif2.Y - dif1.Y * dif2.X;
            if (pseudonorm != 0)
                sgn = pseudonorm / Math.Abs(pseudonorm);
            for (int i = 1; i < VertArray.Length; i++)
            {
                dif1 = VertArray[i] - p;
                dif2 = VertArray[i - 1] - p;
                if (dif1.X == 0 && dif1.Y == 0 || dif2.X == 0 && dif2.Y == 0)
                    return true;
                angle = Math.Acos((dif1.X * dif2.X + dif1.Y * dif2.Y) / 
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
            
            if (Math.Round(summ, 10) == Math.Round(2 * Math.PI, 10))
                return true;
            else
            {
                Point difpa, difpb, difba;
                double normpa, normpb, normba, cs1, cs2, h;
                for (int i = 0; i < Vertex.Length - 1; i++)
                {
                    difpa = p - Vertex[i]; difpb = p - Vertex[i + 1];
                    normpa = Math.Sqrt((Math.Pow(difpa.X, 2) + Math.Pow(difpa.Y, 2)));
                    normpb = Math.Sqrt((Math.Pow(difpb.X, 2) + Math.Pow(difpb.Y, 2)));
                    if (normpa <= eps || normpb <= eps)
                        return true;
            
                    difba = Vertex[i + 1] - Vertex[i];
                    normba = Math.Sqrt(Math.Pow(difba.X, 2) + Math.Pow(difba.Y, 2));
                    cs1 = (difpa.X * difba.Y + difpa.Y * difba.X) / (normpa*normba); 
                    cs2 = (difpb.X * (-difba.Y) + difpb.Y * (-difba.X)) / (normpb*normba);
                    if (cs1 < 0 || cs2 < 0)
                        continue;
            
                    h = Math.Abs(p.X * (Vertex[i].Y - Vertex[i+1].Y) + Vertex[i].X * (Vertex[i+1].Y - p.Y) + Vertex[i+1].X * (p.Y - Vertex[i].Y)) / normba;
                    if (h <= eps)
                        return true;
                }

                difpa = p - Vertex[0]; difpb = p - Vertex[Vertex.Length - 1];
                normpa = Math.Sqrt((Math.Pow(difpa.X, 2) + Math.Pow(difpa.Y, 2)));
                normpb = Math.Sqrt((Math.Pow(difpb.X, 2) + Math.Pow(difpb.Y, 2)));
                if (normpa <= eps || normpb <= eps)
                    return true;
            
                difba = Vertex[Vertex.Length - 1] - Vertex[0];
                normba = Math.Sqrt(Math.Pow(difba.X, 2) + Math.Pow(difba.Y, 2));
                cs1 = (difpa.X * difba.Y + difpa.Y * difba.X) / (normpa*normba);
                cs2 = (difpb.X * (-difba.Y) + difpb.Y * (-difba.X)) / (normpb*normba);
                if (cs1 < 0 || cs2 < 0)
                    return false;
            
                h = Math.Abs(p.X * (Vertex[0].Y - Vertex[Vertex.Length - 1].Y) + Vertex[0].X * (Vertex[Vertex.Length - 1].Y - p.Y) + Vertex[Vertex.Length - 1].X * (p.Y - Vertex[0].Y)) / normba;
                return h <= eps;
            }
        }
    }
}