using Microsoft.VisualStudio.TestTools.UnitTesting;
using Geometry;
using System;

namespace TestProject
{
    [TestClass]
    public class CurvedPolygonTests
    {
        private CurvedPolygon CreateTestCurvedPolygon()
        {
            var verts = new Point[]
            {
                new Point(2, 0), new Point(3, 1), new Point(2, 2),
                new Point(0, 2), new Point(-1, 1), new Point(0, 0)
            };

            return new CurvedPolygon(verts);
        }

        [TestMethod]
        public void CurvedPolygon_Constructor_ValidVertices_CreatesObject()
        {
            var verts = new Point[]
            {
                new Point(2, 0), new Point(3, 1), new Point(2, 2),
                new Point(0, 2), new Point(-1, 1), new Point(0, 0)
            };

            var polygon = new CurvedPolygon(verts);

            Assert.IsNotNull(polygon);
            Assert.AreEqual(6, polygon.Vertex.Length);
        }

        [TestMethod]
        public void CurvedPolygon_Constructor_WithInvalidOddCount_ThrowsException()
        {
            var verts = new Point[]
            {
                new Point(0, 0),
                new Point(1, 1),
                new Point(2, 0),
                new Point(3, 1),
                new Point(4, 0)
            };

            Assert.ThrowsException<IncorrectVertexSpan>(() =>
                new CurvedPolygon(verts));
        }

        [TestMethod]
        public void CurvedPolygon_Constructor_WithLessThanFourVertices_ThrowsException()
        {
            var verts = new Point[]
            {
                new Point(0, 0),
                new Point(1, 1)
            };

            Assert.ThrowsException<IncorrectVertexSpan>(() =>
                new CurvedPolygon(verts));
        }

        [TestMethod]
        public void CurvedPolygon_Center_IsCalculatedCorrectly()
        {
            var polygon = CreateTestCurvedPolygon();

            double expectedX = (2 + 3 + 2 + 0 + (-1) + 0) / 6.0;
            double expectedY = (0 + 1 + 2 + 2 + 1 + 0) / 6.0;

            Assert.AreEqual(expectedX, polygon.Center.X, 0.0001);
            Assert.AreEqual(expectedY, polygon.Center.Y, 0.0001);
        }

        [TestMethod]
        public void CurvedPolygon_Move_ChangesVerticesAndCenter()
        {
            var polygon = CreateTestCurvedPolygon();
            var originalVertices = polygon.Vertex.ToArray();
            var originalCenter = polygon.Center;

            polygon.Move(3, -2);

            for (int i = 0; i < originalVertices.Length; i++)
            {
                Assert.AreEqual(originalVertices[i].X + 3, polygon.Vertex[i].X, 0.0001);
                Assert.AreEqual(originalVertices[i].Y - 2, polygon.Vertex[i].Y, 0.0001);
            }

            Assert.AreEqual(originalCenter.X + 3, polygon.Center.X, 0.0001);
            Assert.AreEqual(originalCenter.Y - 2, polygon.Center.Y, 0.0001);
        }

        [TestMethod]
        public void CurvedPolygon_Scale_WithZeroFactor_ThrowsException()
        {
            var polygon = CreateTestCurvedPolygon();

            Assert.ThrowsException<IncorrectScaleParameter>(() =>
                polygon.Scale(0, 2));

            Assert.ThrowsException<IncorrectScaleParameter>(() =>
                polygon.Scale(2, 0));
        }

        [TestMethod]
        public void CurvedPolygon_RadialScale_WithZeroFactor_ThrowsException()
        {
            var polygon = CreateTestCurvedPolygon();

            Assert.ThrowsException<IncorrectScaleParameter>(() =>
                polygon.RadialScale(0));
        }

        [TestMethod]
        public void CurvedPolygon_UpdateVertex_ChangesVerticesAndCenter()
        {
            var polygon = CreateTestCurvedPolygon();
            var newVerts = new Point[]
            {
                new Point(1, 1), new Point(2, 2), new Point(1, 3),
                new Point(-1, 3), new Point(-2, 2), new Point(-1, 1)
            };

            polygon.UpdateVertex(newVerts);

            for (int i = 0; i < newVerts.Length; i++)
            {
                Assert.AreEqual(newVerts[i].X, polygon.Vertex[i].X, 0.0001);
                Assert.AreEqual(newVerts[i].Y, polygon.Vertex[i].Y, 0.0001);
            }

            double expectedCenterX = (1 + 2 + 1 + (-1) + (-2) + (-1)) / 6.0;
            double expectedCenterY = (1 + 2 + 3 + 3 + 2 + 1) / 6.0;

            Assert.AreEqual(expectedCenterX, polygon.Center.X, 0.0001);
            Assert.AreEqual(expectedCenterY, polygon.Center.Y, 0.0001);
        }

        [TestMethod]
        public void CurvedPolygon_UpdateVertex_WithInvalidCount_ThrowsException()
        {
            var polygon = CreateTestCurvedPolygon();
            var invalidVerts = new Point[]
            {
                new Point(1, 1),
                new Point(2, 2),
                new Point(3, 3)
            };

            Assert.ThrowsException<IncorrectVertexSpan>(() =>
                polygon.UpdateVertex(invalidVerts));
        }

        [TestMethod]
        public void CurvedPolygon_IsIn_PointInside_ReturnsTrue()
        {
            var polygon = CreateTestCurvedPolygon();
            var pointInside = new Point(1, 1);

            var result = polygon.IsIn(pointInside, 0.1);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CurvedPolygon_IsIn_PointOutside_ReturnsFalse()
        {
            var polygon = CreateTestCurvedPolygon();
            var pointOutside = new Point(10, 10);

            var result = polygon.IsIn(pointOutside, 0.1);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CurvedPolygon_IsIn_WithNegativeEps_ThrowsException()
        {
            var polygon = CreateTestCurvedPolygon();
            var point = new Point(1, 1);

            Assert.ThrowsException<IncorrectInaccuracyParameter>(() =>
                polygon.IsIn(point, -0.1));
        }

        [TestMethod]
        public void CurvedPolygon_Move_ThenMoveBack_ReturnsToOriginalState()
        {
            var polygon = CreateTestCurvedPolygon();
            var originalCenter = polygon.Center;
            var originalVertices = polygon.Vertex.ToArray();

            polygon.Move(5, 5);
            polygon.Move(-5, -5);

            Assert.AreEqual(originalCenter.X, polygon.Center.X, 0.0001);
            Assert.AreEqual(originalCenter.Y, polygon.Center.Y, 0.0001);

            for (int i = 0; i < originalVertices.Length; i++)
            {
                Assert.AreEqual(originalVertices[i].X, polygon.Vertex[i].X, 0.0001);
                Assert.AreEqual(originalVertices[i].Y, polygon.Vertex[i].Y, 0.0001);
            }
        }
    }
}