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
                new Point(0, 0),
                new Point(2, 2),
                new Point(4, 0),
                new Point(4, 0),
                new Point(6, -2),
                new Point(8, 0)
            };
            
            return new CurvedPolygon(verts);
        }

        [TestMethod]
        public void CurvedPolygon_Constructor_ValidVertices_CreatesObject()
        {
            var verts = new Point[]
            {
                new Point(0, 0), new Point(1, 1), new Point(2, 0),
                new Point(2, 0), new Point(3, -1), new Point(4, 0)
            };

            var polygon = new CurvedPolygon(verts);

            Assert.IsNotNull(polygon);
            Assert.AreEqual(6, polygon.Vertex.Length);
        }

        [TestMethod]
        public void CurvedPolygon_Constructor_WithLessThan6Vertices_ThrowsException()
        {
            var verts = new Point[]
            {
                new Point(0, 0), new Point(1, 1), new Point(2, 0)
            };

            Assert.ThrowsException<IncorrectVertexSpan>(() => 
                new CurvedPolygon(verts));
        }

        [TestMethod]
        public void CurvedPolygon_Constructor_WithVertexCountNotMultipleOf3_ThrowsException()
        {
            var verts = new Point[]
            {
                new Point(0, 0), new Point(1, 1), new Point(2, 0),
                new Point(2, 0), new Point(3, -1)
            };

            Assert.ThrowsException<IncorrectVertexSpan>(() => 
                new CurvedPolygon(verts));
        }

        [TestMethod]
        public void CurvedPolygon_Center_IsCalculatedCorrectly()
        {
            var polygon = CreateTestCurvedPolygon();

            double expectedX = (0 + 2 + 4 + 4 + 6 + 8) / 6.0;
            double expectedY = (0 + 2 + 0 + 0 + (-2) + 0) / 6.0;

            Assert.AreEqual(expectedX, polygon.Center.X, 0.0001);
            Assert.AreEqual(expectedY, polygon.Center.Y, 0.0001);
        }

        [TestMethod]
        public void CurvedPolygon_Move_ChangesAllVerticesAndCenter()
        {
            var polygon = CreateTestCurvedPolygon();
            var originalVertices = polygon.Vertex.ToArray();

            polygon.Move(3, -2);

            for (int i = 0; i < originalVertices.Length; i++)
            {
                Assert.AreEqual(originalVertices[i].X + 3, polygon.Vertex[i].X, 0.0001);
                Assert.AreEqual(originalVertices[i].Y - 2, polygon.Vertex[i].Y, 0.0001);
            }

            Assert.AreEqual(originalVertices.Length * 3, polygon.Center.X * 6, 0.0001);
        }

        [TestMethod]
        public void CurvedPolygon_Scale_ChangesVerticesProportionally()
        {
            var polygon = CreateTestCurvedPolygon();
            var originalVertices = polygon.Vertex.ToArray();

            polygon.Scale(2, 0.5);

            for (int i = 0; i < originalVertices.Length; i++)
            {
                Assert.AreEqual(originalVertices[i].X * 2, polygon.Vertex[i].X, 0.0001);
                Assert.AreEqual(originalVertices[i].Y * 0.5, polygon.Vertex[i].Y, 0.0001);
            }
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
        public void CurvedPolygon_RadialScale_ChangesVertices()
        {
            var polygon = CreateTestCurvedPolygon();
            var originalVertices = polygon.Vertex.ToArray();
            var center = polygon.Center;

            polygon.RadialScale(2);

            for (int i = 0; i < originalVertices.Length; i++)
            {
                double originalDistX = originalVertices[i].X - center.X;
                double originalDistY = originalVertices[i].Y - center.Y;
                
                double newDistX = polygon.Vertex[i].X - center.X;
                double newDistY = polygon.Vertex[i].Y - center.Y;

                Assert.AreEqual(originalDistX * 2, newDistX, 0.0001);
                Assert.AreEqual(originalDistY * 2, newDistY, 0.0001);
            }
        }

        [TestMethod]
        public void CurvedPolygon_RadialScale_WithZeroFactor_ThrowsException()
        {
            var polygon = CreateTestCurvedPolygon();

            Assert.ThrowsException<IncorrectScaleParameter>(() => 
                polygon.RadialScale(0));
        }

        [TestMethod]
        public void CurvedPolygon_Rotate_ChangesVertices()
        {
            var polygon = CreateTestCurvedPolygon();
            var originalVertices = polygon.Vertex.ToArray();
            var center = polygon.Center;

            polygon.Rotate(Math.PI / 2);

            for (int i = 0; i < originalVertices.Length; i++)
            {
                double dx = originalVertices[i].X - center.X;
                double dy = originalVertices[i].Y - center.Y;
                
                double expectedX = center.X - dy;
                double expectedY = center.Y + dx;

                Assert.AreEqual(expectedX, polygon.Vertex[i].X, 0.0001);
                Assert.AreEqual(expectedY, polygon.Vertex[i].Y, 0.0001);
            }
        }

        [TestMethod]
        public void CurvedPolygon_UpdateVertex_ChangesVerticesAndRecalculatesCenter()
        {
            var polygon = CreateTestCurvedPolygon();
            var newVerts = new Point[]
            {
                new Point(1, 1), new Point(2, 2), new Point(3, 1),
                new Point(3, 1), new Point(4, 0), new Point(5, 1)
            };

            polygon.UpdateVertex(newVerts);

            for (int i = 0; i < newVerts.Length; i++)
            {
                Assert.AreEqual(newVerts[i].X, polygon.Vertex[i].X, 0.0001);
                Assert.AreEqual(newVerts[i].Y, polygon.Vertex[i].Y, 0.0001);
            }

            double expectedCenterX = (1 + 2 + 3 + 3 + 4 + 5) / 6.0;
            double expectedCenterY = (1 + 2 + 1 + 1 + 0 + 1) / 6.0;
            
            Assert.AreEqual(expectedCenterX, polygon.Center.X, 0.0001);
            Assert.AreEqual(expectedCenterY, polygon.Center.Y, 0.0001);
        }

        [TestMethod]
        public void CurvedPolygon_UpdateVertex_WithInvalidCount_ThrowsException()
        {
            var polygon = CreateTestCurvedPolygon();
            var invalidVerts = new Point[]
            {
                new Point(1, 1), new Point(2, 2)
            };

            Assert.ThrowsException<IncorrectVertexSpan>(() => 
                polygon.UpdateVertex(invalidVerts));
        }

        [TestMethod]
        public void CurvedPolygon_IsIn_PointOnBoundary_ReturnsTrue()
        {
            var polygon = CreateTestCurvedPolygon();

            var pointOnCurve = new Point(2, 1); 

            var result = polygon.IsIn(pointOnCurve, 0.1);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CurvedPolygon_IsIn_PointInside_ReturnsTrue()
        {
            // Создаем замкнутую фигуру
            var verts = new Point[]
            {
                new Point(2, 0), new Point(3, 1), new Point(2, 2),    // Верхняя правая
                new Point(2, 2), new Point(1, 3), new Point(0, 2),    // Верхняя левая
                new Point(0, 2), new Point(-1, 1), new Point(0, 0),   // Нижняя левая
                new Point(0, 0), new Point(1, -1), new Point(2, 0)    // Нижняя правая
            };
            
            var polygon = new CurvedPolygon(verts);
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
            var point = new Point(2, 1);

            Assert.ThrowsException<IncorrectInaccuracyParameter>(() => 
                polygon.IsIn(point, -0.1));
        }

        [TestMethod]
        public void CurvedPolygon_Operations_ChainCorrectly()
        {
            var polygon = CreateTestCurvedPolygon();
            var originalVertices = polygon.Vertex.ToArray();

            polygon.Move(1, 1);
            polygon.Scale(2, 2);
            polygon.Rotate(Math.PI);
            polygon.Move(-1, -1);

            var center = polygon.Center;
            
            for (int i = 0; i < originalVertices.Length; i++)
            {
                Assert.IsTrue(Math.Abs(polygon.Vertex[i].X - center.X) > 0.0001);
            }
        }

        [TestMethod]
        public void CurvedPolygon_ConsistencyAfterOperations()
        {
            var polygon = CreateTestCurvedPolygon();
            var originalCenter = polygon.Center;
            var originalVertices = polygon.Vertex.ToArray();

            polygon.Move(5, 5);
            polygon.Move(-5, -5);

            // После перемещения туда и обратно должны вернуться в исходное положение
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
