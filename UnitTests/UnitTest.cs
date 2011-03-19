using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonWebService;

namespace UnitTests {
    /// <summary>
    /// Testing json service features
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

        public dynamic PostDocument(string RelativeUrl, string JsonToPost) {
            string raw = wc.UploadString(ts.Uri.AbsoluteUri + RelativeUrl, "POST", JsonToPost);
            return JsonDocument.Parse(raw);
        }

        [TestMethod]
        public void NoParameterMethodCall() {
            Assert.IsNotNull(ts.Root());
        }

        [TestMethod]
        public void NoParameterServiceCall() {            
            dynamic doc = GetDocument("");

            Assert.IsTrue(doc.status == "ok");
        }

        [TestMethod]
        public void ParameterMethod() {
            Assert.IsNotNull(ts.Sum(2, 2));
        }

        [TestMethod]
        public void ParameterService() {
            dynamic doc = GetDocument("add?value1=2&value2=3");

            Assert.IsTrue(doc.sum == 5);
        }

        [TestMethod]
        public void ParameterTypeFailure() {
            dynamic doc = GetDocument("add?value1=X&value2=Y");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void ParameterNameCasingIrrelevant() {
            dynamic doc = GetDocument("add?VALUE1=3&vAlUe2=10");

            Assert.IsTrue(doc.sum == 13);
        }

        [TestMethod]
        public void InvalidCall() {
            dynamic doc = GetDocument("idontexist");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void AllowDescribeOn() {
            ts.Authorize = false;
            ts.AllowDescribe = true;
            dynamic doc = GetDocument("help");
            
            Assert.IsFalse(doc.status == "failed");
        }

        [TestMethod]
        public void AllowDescribeOff() {
            ts.Authorize = false;
            ts.AllowDescribe = false;
            dynamic doc = GetDocument("help");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void AuthorizeOn() {
            ts.Authorize = true;
            dynamic doc = GetDocument("");

            Assert.IsTrue(doc.status == "failed");
            ts.Authorize = false;
        }

        [TestMethod]
        public void AuthorizeOff() {
            ts.Authorize = false;
            dynamic doc = GetDocument("?apikey=anythingwillwork");

            Assert.IsTrue(doc.status == "ok");
        }

        [TestMethod]
        public void HttpVerbReject() {
            dynamic doc = GetDocument("save?id=1");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void HttpVerbAccept() {
            dynamic doc = PostDocument("save?id=1", string.Empty);

            Assert.IsTrue(doc.status == "ok");
        }

        [TestMethod]
        public void MissingRequiredParameter() {
            dynamic doc = GetDocument("add?value1=3");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void MissingNonRequiredParameter() {
            dynamic doc = GetDocument("mult?value2=3");

            Assert.IsTrue(doc.product == 0);
        }

        [TestMethod]
        public void PostingJsonDocument() {
            dynamic doc = PostDocument("save?id=2", "{ \"name\": \"Mike\", \"age\": 31 }");

            Assert.IsTrue(doc.status == "ok");
            Assert.IsTrue(doc.name == "Mike");
            Assert.IsTrue(doc.age == 31);
        }

        [TestMethod]
        public void InvalidJsonPosted() {
            dynamic doc = PostDocument("save?id=3", "{ not valid json }");

            Assert.IsTrue(doc.status == "failed");
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
