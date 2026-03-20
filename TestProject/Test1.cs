using Microsoft.VisualStudio.TestTools.UnitTesting;
using Geometry;

namespace TestProject
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void Point_Add_WorksCorrectly()
        {
            var a = new Point(1, 2);
            var b = new Point(3, 4);

            var result = a + b;

            Assert.AreEqual(4, result.X, 0.0001);
            Assert.AreEqual(6, result.Y, 0.0001);
        }

        [TestMethod]
        public void Line_Center_IsCalculatedCorrectly()
        {
            var line = new Line(new Point(0, 0), new Point(4, 4));

            Assert.AreEqual(2, line.Center.X, 0.0001);
            Assert.AreEqual(2, line.Center.Y, 0.0001);
        }

        [TestMethod]
        public void Line_Move_ChangesVertices()
        {
            var line = new Line(new Point(0, 0), new Point(2, 2));

            line.Move(3, 1);

            Assert.AreEqual(3, line.Vertex[0].X, 0.0001);
            Assert.AreEqual(1, line.Vertex[0].Y, 0.0001);
            Assert.AreEqual(5, line.Vertex[1].X, 0.0001);
            Assert.AreEqual(3, line.Vertex[1].Y, 0.0001);
        }

        [TestMethod]
        public void Line_IsIn_PointOnLine_ReturnsTrue()
        {
            var line = new Line(new Point(0, 0), new Point(4, 4));

            var result = line.IsIn(new Point(2, 2), 0.001);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Line_IsIn_NegativeEps_ThrowsException()
        {
            var line = new Line(new Point(0, 0), new Point(4, 4));

            Assert.ThrowsException<IncorrectInaccuracyParameter>(() =>
                line.IsIn(new Point(2, 2), -1));
        }

        [TestMethod]
        public void Ellipse_Move_ChangesCenter()
        {
            var ellipse = new Ellipse(new Point(1, 1), 2, 3);

            ellipse.Move(4, -1);

            Assert.AreEqual(5, ellipse.Center.X, 0.0001);
            Assert.AreEqual(0, ellipse.Center.Y, 0.0001);
        }

        [TestMethod]
        public void Ellipse_IsIn_CenterPoint_ReturnsTrue()
        {
            var ellipse = new Ellipse(new Point(0, 0), 5, 3);

            var result = ellipse.IsIn(new Point(0, 0), 0.001);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Ellipse_Scale_WithZero_ThrowsException()
        {
            var ellipse = new Ellipse(new Point(0, 0), 5, 3);

            Assert.ThrowsException<IncorrectScaleParameter>(() =>
                ellipse.Scale(0, 2));
        }

        [TestMethod]
        public void Curve_Constructor_WithWrongVertexCount_ThrowsException()
        {
            var verts = new Point[]
            {
                new Point(0, 0),
                new Point(1, 1)
            };

            Assert.ThrowsException<IncorrectVertexSpan>(() =>
                new Curve(verts));
        }

        [TestMethod]
        public void Curve_Move_ChangesCenter()
        {
            var verts = new Point[]
            {
                new Point(0, 0),
                new Point(2, 0),
                new Point(1, 2)
            };

            var curve = new Curve(verts);

            curve.Move(3, 1);

            Assert.AreEqual(4, curve.Center.X, 0.0001);
            Assert.AreEqual(1.6666667, curve.Center.Y, 0.0001);
        }

        [TestMethod]
        public void Polygon_Constructor_WithLessThanThreeVertices_ThrowsException()
        {
            var verts = new Point[]
            {
                new Point(0, 0),
                new Point(1, 1)
            };

            Assert.ThrowsException<IncorrectVertexSpan>(() =>
                new Polygon(verts));
        }

        [TestMethod]
        public void Polygon_IsIn_PointInsideTriangle_ReturnsTrue()
        {
            var verts = new Point[]
            {
                new Point(0, 0),
                new Point(4, 0),
                new Point(0, 4)
            };

            var polygon = new Polygon(verts);

            var result = polygon.IsIn(new Point(1, 1), 0.001);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Polygon_Move_ChangesCenter()
        {
            var verts = new Point[]
            {
                new Point(0, 0),
                new Point(4, 0),
                new Point(0, 2)
            };

            var polygon = new Polygon(verts);

            polygon.Move(1, 3);

            Assert.AreEqual(2.3333333, polygon.Center.X, 0.0001);
            Assert.AreEqual(3.6666667, polygon.Center.Y, 0.0001);
        }
    }
}