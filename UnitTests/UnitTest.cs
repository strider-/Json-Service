using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonWebService;

namespace UnitTests {
    /// <summary>
    /// Testing service features
    /// </summary>
    [TestClass]
    public class UnitTest {
        static TestService ts;
        static WebClient wc;

        [ClassInitialize]
        public static void StartServer(TestContext context) {
            ts = new TestService();
            ts.LogOutput = null;
            ts.Start(false);

            wc = new WebClient();
        }
        [ClassCleanup]
        public static void StopServer() {
            wc = null;
            ts.Stop();
        }

        public dynamic GetDocument(string RelativeUrl) {
            string raw = wc.DownloadString(ts.Uri.AbsoluteUri + RelativeUrl);
            return JsonDocument.Parse(raw);
        }

        [TestMethod]
        public void NoParameterMethodTest() {
            Assert.IsNotNull(ts.Root());
        }

        [TestMethod]
        public void NoParameterServiceTest() {
            string raw = wc.DownloadString(ts.Uri);
            dynamic doc = JsonDocument.Parse(raw);

            Assert.IsTrue(doc.status == "ok");
        }

        [TestMethod]
        public void ParameterMethodTest() {
            Assert.IsNotNull(ts.Sum(2, 2));
        }

        [TestMethod]
        public void ParameterServiceTest() {
            dynamic doc = GetDocument("add?value1=2&value2=3");

            Assert.IsTrue(doc.sum == 5);
        }

        [TestMethod]
        public void ParameterTypeFailure() {
            dynamic doc = GetDocument("add?value1=2&value2=X");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void InvalidCall() {
            dynamic doc = GetDocument("idontexist");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void AllowDescribeOnTest() {
            ts.Authorize = false;
            ts.AllowDescribe = true;
            dynamic doc = GetDocument("help");
            
            Assert.IsFalse(doc.status == "failed");
        }

        [TestMethod]
        public void AllowDescribeOffTest() {
            ts.Authorize = false;
            ts.AllowDescribe = false;
            dynamic doc = GetDocument("help");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void AuthorizeOnTest() {
            ts.Authorize = true;
            dynamic doc = GetDocument("");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void AuthorizeOffTest() {
            ts.Authorize = false;
            dynamic doc = GetDocument("?apikey=anythingwillwork");

            Assert.IsTrue(doc.status == "ok");
        }

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

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
    }    
}
