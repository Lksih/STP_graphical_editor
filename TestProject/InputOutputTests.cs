using Microsoft.VisualStudio.TestTools.UnitTesting;
using Geometry;
using InputOutput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestProject
{
    [TestClass]
    public class InputOutputTests
    {
        private static string CreateTempFilePath()
        {
            return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        }

        [TestMethod]
        public void SaveAndLoadFigures_Line_RoundTripPreservesData()
        {
            string path = CreateTempFilePath();

            try
            {
                var figures = new List<IFigure>
                {
                    new Line(new Point(0, 0), new Point(4, 4))
                };

                FigureJsonIo.SaveFigures(figures, path);
                var loaded = FigureJsonIo.LoadFigures(path);

                Assert.AreEqual(1, loaded.Count);
                Assert.IsInstanceOfType(loaded[0], typeof(Line));

                var line = (Line)loaded[0];
                Assert.AreEqual(0, line.Vertex[0].X, 0.0001);
                Assert.AreEqual(0, line.Vertex[0].Y, 0.0001);
                Assert.AreEqual(4, line.Vertex[1].X, 0.0001);
                Assert.AreEqual(4, line.Vertex[1].Y, 0.0001);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveAndLoadFigures_Curve_RoundTripPreservesVertices()
        {
            string path = CreateTempFilePath();

            try
            {
                var figures = new List<IFigure>
                {
                    new Curve(new Point[]
                    {
                        new Point(0, 0),
                        new Point(2, 2),
                        new Point(4, 0)
                    })
                };

                FigureJsonIo.SaveFigures(figures, path);
                var loaded = FigureJsonIo.LoadFigures(path);

                Assert.AreEqual(1, loaded.Count);
                Assert.IsInstanceOfType(loaded[0], typeof(Curve));

                var curve = (Curve)loaded[0];
                Assert.AreEqual(3, curve.Vertex.Length);
                Assert.AreEqual(0, curve.Vertex[0].X, 0.0001);
                Assert.AreEqual(2, curve.Vertex[1].X, 0.0001);
                Assert.AreEqual(4, curve.Vertex[2].X, 0.0001);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveAndLoadFigures_Polygon_RoundTripPreservesVertices()
        {
            string path = CreateTempFilePath();

            try
            {
                var figures = new List<IFigure>
                {
                    new Polygon(new Point[]
                    {
                        new Point(0, 0),
                        new Point(4, 0),
                        new Point(0, 4)
                    })
                };

                FigureJsonIo.SaveFigures(figures, path);
                var loaded = FigureJsonIo.LoadFigures(path);

                Assert.AreEqual(1, loaded.Count);
                Assert.IsInstanceOfType(loaded[0], typeof(Polygon));

                var polygon = (Polygon)loaded[0];
                Assert.AreEqual(3, polygon.Vertex.Length);
                Assert.AreEqual(4, polygon.Vertex[1].X, 0.0001);
                Assert.AreEqual(4, polygon.Vertex[2].Y, 0.0001);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void LoadFigures_EmptyJsonArray_ReturnsEmptyList()
        {
            string path = CreateTempFilePath();

            try
            {
                File.WriteAllText(path, "[]");

                var loaded = FigureJsonIo.LoadFigures(path);

                Assert.IsNotNull(loaded);
                Assert.AreEqual(0, loaded.Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void LoadFigures_UnsupportedType_ThrowsNotSupportedException()
        {
            string path = CreateTempFilePath();

            try
            {
                File.WriteAllText(path, """
                [
                  {
                    "Type": "UnknownFigure",
                    "Points": []
                  }
                ]
                """);

                Assert.ThrowsException<NotSupportedException>(() =>
                    FigureJsonIo.LoadFigures(path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void LoadFigures_LineWithWrongPointsCount_ThrowsInvalidDataException()
        {
            string path = CreateTempFilePath();

            try
            {
                File.WriteAllText(path, """
                [
                  {
                    "Type": "Line",
                    "Points": [
                      { "X": 1, "Y": 2 }
                    ]
                  }
                ]
                """);

                Assert.ThrowsException<InvalidDataException>(() =>
                    FigureJsonIo.LoadFigures(path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void LoadFigures_PolygonWithTooFewPoints_ThrowsInvalidDataException()
        {
            string path = CreateTempFilePath();

            try
            {
                File.WriteAllText(path, """
                [
                  {
                    "Type": "Polygon",
                    "Points": [
                      { "X": 0, "Y": 0 },
                      { "X": 1, "Y": 1 }
                    ]
                  }
                ]
                """);

                Assert.ThrowsException<InvalidDataException>(() =>
                    FigureJsonIo.LoadFigures(path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void LoadFigures_CurvedPolygonWithInvalidPoints_ThrowsInvalidDataException()
        {
            string path = CreateTempFilePath();

            try
            {
                File.WriteAllText(path, """
                [
                  {
                    "Type": "CurvedPolygon",
                    "Points": [
                      { "X": 0, "Y": 0 },
                      { "X": 1, "Y": 1 },
                      { "X": 2, "Y": 0 },
                      { "X": 3, "Y": 1 }
                    ]
                  }
                ]
                """);

                Assert.ThrowsException<InvalidDataException>(() =>
                    FigureJsonIo.LoadFigures(path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void LoadFigures_InvalidJson_ThrowsException()
        {
            string path = CreateTempFilePath();

            try
            {
                File.WriteAllText(path, "{ invalid json ]");

                Assert.ThrowsException<Exception>(() =>
                    FigureJsonIo.LoadFigures(path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveFigures_UnsupportedFigureType_ThrowsNotSupportedException()
        {
            string path = CreateTempFilePath();

            try
            {
                var figures = new List<IFigure>
                {
                    new FakeFigure()
                };

                Assert.ThrowsException<NotSupportedException>(() =>
                    FigureJsonIo.SaveFigures(figures, path));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private sealed class FakeFigure : IFigure
        {
            public Point Center => new Point(0, 0);
            public ReadOnlySpan<Point> Vertex => [];
            public void Scale(double dx, double dy) { }
            public void RadialScale(double dr) { }
            public void Rotate(double angle) { }
            public void Move(double dx, double dy) { }
            public void UpdateVertex(ReadOnlySpan<Point> vertex) { }
            public IEnumerable<IDrawFigure> Draw() => Array.Empty<IDrawFigure>();
            public bool IsIn(Point p, double eps) => false;
        }
    }
}