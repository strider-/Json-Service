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
            // the calling a method directly (with no parameters) should return an anon object
            Assert.IsNotNull(ts.Root());
        }

        [TestMethod]
        public void NoParameterServiceCall() {
            // the invoking the method via service (with no parameters) should return a json document
            dynamic doc = GetDocument("");

            Assert.IsTrue(doc.status == "ok");
        }

        [TestMethod]
        public void ParameterMethod() {
            // the calling a method directly (with parameters) should return an anon object
            Assert.IsNotNull(ts.Sum(2, 2));
        }

        [TestMethod]
        public void ParameterService() {
            // the invoking the method via service (with parameters) should return a json document
            dynamic doc = GetDocument("add?value1=2&value2=3");

            Assert.IsTrue(doc.sum == 5);
        }

        [TestMethod]
        public void ParameterTypeFailure() {
            // Invalid parameter types should fail
            dynamic doc = GetDocument("add?value1=X&value2=Y");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void ParameterNameCasingIrrelevant() {
            // The casing of the parameter names shouldn't matter
            dynamic doc = GetDocument("add?VALUE1=3&vAlUe2=10");

            Assert.IsTrue(doc.sum == 13);
        }

        [TestMethod]
        public void InvalidCall() {
            // Attempting to invoke a method that does not exist should fail
            dynamic doc = GetDocument("idontexist");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void AllowDescribeOn() {
            // Service descriptions should not fail when the DescribeUri is not null and AllowDescribe is true
            ts.Authorize = false;
            ts.AllowDescribe = true;
            dynamic doc = GetDocument("help");

            Assert.IsNotNull(ts.DescriptionUri);
            Assert.IsFalse(doc.status == "failed");
        }

        [TestMethod]
        public void AllowDescribeOff() {
            // Service description should fail when AllowDescribe is false
            ts.Authorize = false;
            ts.AllowDescribe = false;
            dynamic doc = GetDocument("help");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void AuthorizeOn() {
            // Attempting to invoke a method when Authorize is true (with no credentials) should fail
            ts.Authorize = true;
            dynamic doc = GetDocument("");

            Assert.IsTrue(doc.status == "failed");
            ts.Authorize = false;
        }

        [TestMethod]
        public void AuthorizeOnWithValidCreds() {
            // Attempting to invoke a method when Authorize is true (with valid credentials) should succeed
            ts.Authorize = true;
            dynamic doc = GetDocument("?apikey=anythingwillwork");

            Assert.IsFalse(doc.status == "failed");
            ts.Authorize = false;
        }

        [TestMethod]
        public void AuthorizeOff() {
            // Attempting to invoke a method when Authorize is false should succeed
            ts.Authorize = false;
            dynamic doc = GetDocument("?apikey=anythingwillwork");

            Assert.IsTrue(doc.status == "ok");
        }

        [TestMethod]
        public void HttpVerbReject() {
            // Incorrect verbs should be rejected
            dynamic doc = GetDocument("save?id=1");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void HttpVerbAccept() {
            // Correct verbs should be accepted
            dynamic doc = PostDocument("save?id=1", string.Empty);

            Assert.IsTrue(doc.status == "ok");
        }

        [TestMethod]
        public void MissingRequiredParameter() {
            // Missing parameters with no default values should cause a failure
            dynamic doc = GetDocument("add?value1=3");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void MissingNonRequiredParameter() {
            // Missing parameters that have a default value should succeed
            dynamic doc = GetDocument("mult?value2=3");

            Assert.IsTrue(doc.product == 0);
        }

        [TestMethod]
        public void PostingJsonDocument() {
            // Posting a json document to a service method that accepts one should succeed
            dynamic doc = PostDocument("save?id=2", "{ \"name\": \"Mike\", \"age\": 31 }");

            Assert.IsTrue(doc.status == "ok");
            Assert.IsTrue(doc.name == "Mike");
            Assert.IsTrue(doc.age == 31);
        }

        [TestMethod]
        public void InvalidJsonPosted() {
            // Rejecting a broken json document
            dynamic doc = PostDocument("save?id=3", "{ not valid json }");

            Assert.IsTrue(doc.status == "failed");
        }

        [TestMethod]
        public void MultipartPaths() {
            // Paths in the UriTemplate should be respected
            dynamic doc = GetDocument("i/am/a/multipart/path?with=strings_and_things");

            Assert.IsTrue(doc.status == "ok");
            Assert.IsTrue(doc.vars == "strings_and_things");
        }

        [TestMethod]
        public void LeadingSlashIrrelevant() {
            // The appearance of a leading slash on the UriTemplate & Example properties of a VerbAttribute shouldn't matter.
            dynamic doc = GetDocument("/slashprefix");

            Assert.IsTrue(doc.status == "ok");
        }

        [TestMethod]
        public void CustomDescriptionPath() {
            // Custom set service description paths should be a-ok
            ts.Stop();
            ts.DescribePath = "/I/Need/Help";
            ts.AllowDescribe = true;
            ts.Start(false);

            dynamic doc = GetDocument("I/Need/Help");

            Assert.IsFalse(doc.status == "failed");

            ts.Stop();
            ts.DescribePath = "/help";
            ts.Start(false);
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
