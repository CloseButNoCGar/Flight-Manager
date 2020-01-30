using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlightManager.UnitTests
{
    [TestClass]
    public class PointItemTests
    {
        [TestMethod]
        public void DistanceBetweenPoints_SamePoint_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double result;

            // Act
            result = point.DistanceBetweenPoints(point);

            // Assert
            Assert.AreEqual(result, 0);
        }

        [TestMethod]
        public void DistanceBetweenPoints_FlipPoints_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            PointItem point1 = new PointItem(0, 0, 0);
            double result;
            double result1;

            // Act
            result = point.DistanceBetweenPoints(point1);
            result1 = point1.DistanceBetweenPoints(point);

            // Assert
            Assert.AreEqual(result, result1);
        }

        [TestMethod]
        public void DistanceBetweenPoints_KnownDistance_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            PointItem point1 = new PointItem(0, 0, 0);
            double result;
            double knownDistance = 1568520.55679858;

            // Act
            result = point.DistanceBetweenPoints(point1);

            // Assert
            Assert.AreEqual(knownDistance, result, 0.00000001);
        }

        [TestMethod]
        public void DistanceToEllipseTangent_BaseTangent0Gradient_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double gradient = 0;
            bool perpendicular = false;
            double result;
            double expected = 120;

            // Act
            result = point.DistanceToEllipseTangent(perpendicular, gradient);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void DistanceToEllipseTangent_BaseTangentInfGradient_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double gradient = double.PositiveInfinity;
            bool perpendicular = false;
            double result;
            double expected = 265;

            // Act
            result = point.DistanceToEllipseTangent(perpendicular, gradient);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void DistanceToEllipseTangent_BaseTangent1Gradient_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double gradient = 1;
            bool perpendicular = false;
            double result;
            double expected = 202.39194648;

            // Act
            result = point.DistanceToEllipseTangent(perpendicular, gradient);

            // Assert
            Assert.AreEqual(expected, result, 0.00000001);
        }

        [TestMethod]
        public void DistanceToEllipseTangent_BaseTangentNegative1Gradient_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double gradient = -1;
            bool perpendicular = false;
            double result;
            double expected = 188.24981085;

            // Act
            result = point.DistanceToEllipseTangent(perpendicular, gradient);

            // Assert
            Assert.AreEqual(expected, result, 0.00000001);
        }

        [TestMethod]
        public void DistanceToEllipseTangent_PerpendicularTangent0Gradient_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double gradient = 0;
            bool perpendicular = true;
            double result;
            double expected = 140;

            // Act
            result = point.DistanceToEllipseTangent(perpendicular, gradient);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void DistanceToEllipseTangent_PerpendicularTangentInfGradient_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double gradient = double.PositiveInfinity;
            bool perpendicular = true;
            double result;
            double expected = 265;

            // Act
            result = point.DistanceToEllipseTangent(perpendicular, gradient);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void DistanceToEllipseTangent_PerpendicularTangent1Gradient_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double gradient = 1;
            bool perpendicular = true;
            double result;
            double expected = -202.39194648;

            // Act
            result = point.DistanceToEllipseTangent(perpendicular, gradient);

            // Assert
            Assert.AreEqual(expected, result, 0.00000001);
        }

        [TestMethod]
        public void DistanceToEllipseTangent_PerpendicularTangentNegative1Gradient_ReturnsTrue()
        {
            // Arrange
            PointItem point = new PointItem(10, 10, 0);
            double gradient = -1;
            bool perpendicular = true;
            double result;
            double expected = 188.24981085;

            // Act
            result = point.DistanceToEllipseTangent(perpendicular, gradient);

            // Assert
            Assert.AreEqual(expected, result, 0.00000001);
        }
    }
}
