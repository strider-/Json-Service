using JsonWebService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnitTests {
    /// <summary>
    ///This is a test class for TemplateCollisionExceptionTest and is intended
    ///to contain all TemplateCollisionExceptionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TemplateCollisionExceptionTest {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        /// <summary>
        ///A test for TemplateCollisionException Serialization
        ///</summary>
        [TestMethod]
        public void TemplateCollisionExceptionSerialization() {
            string[] parms = new string[] { },
                     meths = new string[] { "Help", "Describe" };
            TemplateCollisionException e = new TemplateCollisionException("/help", parms, "GET", meths);

            using(Stream s = new MemoryStream()) {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(s, e);
                s.Position = 0;
                e = (TemplateCollisionException)formatter.Deserialize(s);
            }

            Assert.AreEqual(e.Path, "/help");
            CollectionAssert.AreEqual(e.ParameterNames, parms);
            Assert.AreEqual(e.Verb, "GET");
            CollectionAssert.AreEqual(e.Methods, meths);
        }
    }
}
