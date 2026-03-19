using System.Numerics;
using System.Runtime.InteropServices;

namespace Geometry
{
    /// <summary>
    /// Криволинейный многоугольник: граница задаётся набором кривых (Curve).
    /// Каждая кривая определяется тремя точками (как в классе Curve).
    /// </summary>
    public class CurvedPolygon : IFigure
    {
        public Point Center { get; private set; }

        // Возвращаем контрольные точки всех кривых, последовательно
        public ReadOnlySpan<Point> Vertex => VertArray;

        private Point[] VertArray;
        private Curve[] Curves;

        /// <summary>
        /// Конструктор принимает точки тройками: каждые 3 точки задают одну кривую-ребро.
        /// Минимум 2 кривых (6 точек).
        /// </summary>
        public CurvedPolygon(ReadOnlySpan<Point> verts)
        {
            if (verts.Length < 6 || verts.Length % 3 != 0)
                throw new IncorrectVertexSpan("Криволинейный многоугольник должен задаваться хотя бы 2 кривыми (6 точек), количество точек кратно 3.");

            VertArray = verts.ToArray();
            BuildCurvesFromVertices();
            RecalculateCenter();
        }

        public void Scale(double dx, double dy)
        {
            if (dx == 0 || dy == 0)
                throw new IncorrectScaleParameter();

            foreach (var curve in Curves)
                curve.Scale(dx, dy);

            SyncVerticesFromCurves();
            RecalculateCenter();
        }

        public void RadialScale(double dr)
        {
            if (dr == 0)
                throw new IncorrectScaleParameter();

            foreach (var curve in Curves)
                curve.RadialScale(dr);

            SyncVerticesFromCurves();
            RecalculateCenter();
        }

        public void Rotate(double angle)
        {
            foreach (var curve in Curves)
                curve.Rotate(angle);

            SyncVerticesFromCurves();
            RecalculateCenter();
        }

        public void Move(double dx, double dy)
        {
            foreach (var curve in Curves)
                curve.Move(dx, dy);

            SyncVerticesFromCurves();
            RecalculateCenter();
        }

        public void UpdateVertex(ReadOnlySpan<Point> newVertex)
        {
            if (newVertex.Length < 6 || newVertex.Length % 3 != 0)
                throw new IncorrectVertexSpan("Криволинейный многоугольник должен задаваться хотя бы 2 кривыми (6 точек), количество точек кратно 3.");

            VertArray = newVertex.ToArray();
            BuildCurvesFromVertices();
            RecalculateCenter();
        }

        public IEnumerable<IDrawFigure> Draw() => throw new NotImplementedException();

        /// <summary>
        /// Проверка принадлежности точки области, ограниченной криволинейным многоугольником.
        /// Реализовано через аппроксимацию каждой кривой отрезками и использование Polygon.IsIn.
        /// </summary>
        public bool IsIn(Point p, double eps)
        {
            if (eps < 0)
                throw new IncorrectInaccuracyParameter();

            // Аппроксимируем каждую кривую ломаной и собираем все точки в одну границу
            var points = new List<Point>();

            foreach (var curve in Curves)
            {
                ReadOnlySpan<Point> v = curve.Vertex;
                Point p0 = v[0];
                Point p1 = v[1];
                Point p2 = v[2];

                double len01 = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2));
                double len12 = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                double approxLen = len01 + len12;

                int segments = (int)(approxLen / (eps > 0 ? eps : 1e-6));
                if (segments < 8) segments = 8;
                if (segments > 256) segments = 256;

                for (int i = 0; i <= segments; i++)
                {
                    // Чтобы не дублировать точки на стыке кривых, пропускаем первую точку
                    // для всех кривых, кроме первой
                    if (points.Count > 0 && i == 0)
                        continue;

                    double t = (double)i / segments;
                    double oneMinusT = 1.0 - t;

                    Point curr = new Point(
                        oneMinusT * oneMinusT * p0.X + 2 * oneMinusT * t * p1.X + t * t * p2.X,
                        oneMinusT * oneMinusT * p0.Y + 2 * oneMinusT * t * p1.Y + t * t * p2.Y
                    );

                    points.Add(curr);
                }
            }

            if (points.Count < 3)
                return false;

            // Используем уже реализованный Polygon для проверки
            var polygon = new Polygon(CollectionsMarshal.AsSpan(points));
            return polygon.IsIn(p, eps);
        }

        private void BuildCurvesFromVertices()
        {
            int curveCount = VertArray.Length / 3;
            Curves = new Curve[curveCount];

            for (int i = 0; i < curveCount; i++)
            {
                var span = new ReadOnlySpan<Point>(VertArray, i * 3, 3);
                Curves[i] = new Curve(span);
            }
        }

        private void SyncVerticesFromCurves()
        {
            VertArray = new Point[Curves.Length * 3];
            for (int i = 0; i < Curves.Length; i++)
            {
                ReadOnlySpan<Point> v = Curves[i].Vertex;
                VertArray[i * 3] = v[0];
                VertArray[i * 3 + 1] = v[1];
                VertArray[i * 3 + 2] = v[2];
            }
        }

        private void RecalculateCenter()
        {
            Center = new Point(0, 0);
            foreach (var vert in VertArray)
                Center += vert;
            Center *= 1 / VertArray.Length;
        }
    }
}

