using System;
using TestFramework;
using TestFramework.Attributes;

namespace MyTestProject
{
    [TestClass]
    [Category("F")]
    public class AuthorATests
    {
        [TestMethod]
        [Author("AuthorA")]
        [Priority(1)]
        public void A_P1_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorA")]
        [Priority(1)]
        public void A_P1_Test2() => Assert.IsTrue(true);

        [TestMethod]
        [Author("AuthorA")]
        [Priority(2)]
        public void A_P2_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorA")]
        [Priority(2)]
        public void A_P2_Test2() => Assert.IsTrue(true);

        [TestMethod]
        [Author("AuthorA")]
        [Priority(3)]
        public void A_P3_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorA")]
        [Priority(3)]
        public void A_P3_Test2() => Assert.IsTrue(true);

        [TestMethod]
        [Author("AuthorA")]
        [Priority(4)]
        public void A_P4_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorA")]
        [Priority(4)]
        public void A_P4_Test2() => Assert.IsTrue(true);

        [TestMethod]
        [Author("AuthorA")]
        [Priority(5)]
        public void A_P5_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorA")]
        [Priority(5)]
        public void A_P5_Test2() => Assert.IsTrue(true);
    }

    [TestClass]
    [Category("FilterTest")]
    public class AuthorBTests
    {
        [TestMethod]
        [Author("AuthorB")]
        [Priority(1)]
        public void B_P1_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorB")]
        [Priority(1)]
        public void B_P1_Test2() => Assert.IsTrue(true);

        [TestMethod]
        [Author("AuthorB")]
        [Priority(2)]
        public void B_P2_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorB")]
        [Priority(2)]
        public void B_P2_Test2() => Assert.IsTrue(true);

        [TestMethod]
        [Author("AuthorB")]
        [Priority(3)]
        public void B_P3_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorB")]
        [Priority(3)]
        public void B_P3_Test2() => Assert.IsTrue(true);

        [TestMethod]
        [Author("AuthorB")]
        [Priority(4)]
        public void B_P4_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorB")]
        [Priority(4)]
        public void B_P4_Test2() => Assert.IsTrue(true);

        [TestMethod]
        [Author("AuthorB")]
        [Priority(5)]
        public void B_P5_Test1() => Assert.IsTrue(true);
        [TestMethod]
        [Author("AuthorB")]
        [Priority(5)]
        public void B_P5_Test2() => Assert.IsTrue(true);
    }
}