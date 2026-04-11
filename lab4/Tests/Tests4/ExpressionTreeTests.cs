using System;
using TestFramework;
using TestFramework.Attributes;

namespace MyTestProject
{
    [TestClass]
    [Category("ExpressionTree")]
    public class ExpressionTreeTests
    {
        [TestMethod]
        public void TestSimpleComparison_Pass()
        {
            int score = 85;
            Assert.That(() => score > 50);
        }

        [TestMethod]
        public void TestSimpleComparison_Fail()
        {
            int speed = 120;
            int limit = 90;
            Assert.That(() => speed <= limit, "Speeding detected!");
        }

        [TestMethod]
        public void TestComplexMath_Fail()
        {
            int a = 5;
            int b = 10;
            int c = 2;
            int target = 100;

            Assert.That(() => (a + b) * c >= target, "Calculation error in business logic");
        }

        [TestMethod]
        public void TestLogicalOperators_Fail()
        {
            bool isRegistered = true;
            bool hasSubscription = false;
            bool isAdmin = false;

            Assert.That(() => (isRegistered && hasSubscription) || isAdmin, "Access Denied: User has no rights");
        }

        [TestMethod]
        public void TestStringOperations_Fail()
        {
            string status = "Pending";
            string expected = "Completed";

            Assert.That(() => status == expected, "Order status mismatch");
        }

        [TestMethod]
        public void TestDeepNesting_Fail()
        {
            int x = 1;
            int y = 2;
            int z = 3;
            int w = 4;

            Assert.That(() => ((x + y) + z) + w > 50, "Sum is too low");
        }
    }
}