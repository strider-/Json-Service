using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonWebService;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnitTests {
    /// <summary>
    /// Testing json service features
    /// </summary>
    [TestClass]
    public class JsonServiceTest {
        static MockService ts;
        static WebClient wc;

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


        [ClassInitialize]
        public static void StartServer(TestContext context) {
            ts = new MockService();
            ts.Port = 9999;
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

            Assert.AreEqual(doc.status, "ok");
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

            Assert.AreEqual(doc.sum, 5);
        }

        [TestMethod]
        public void ParameterTypeFailure() {
            // Invalid parameter types should fail
            dynamic doc = GetDocument("add?value1=X&value2=Y");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        public void ParameterNameCasingIrrelevant() {
            // The casing of the parameter names shouldn't matter
            dynamic doc = GetDocument("add?VALUE1=3&vAlUe2=10");

            Assert.AreEqual(doc.sum, 13);
        }

        [TestMethod]
        public void InvalidCall() {
            // Attempting to invoke a method that does not exist should fail
            dynamic doc = GetDocument("idontexist");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        public void CallFailure() {
            // An unhandled exception in the invoked method should fail, but still return json to the client
            dynamic doc = GetDocument("gonnafail");

            Assert.AreEqual(doc.status, "failed");
            Assert.AreEqual(doc.error, "You should still get a json response with an unhandled exception.");
        }

        [TestMethod]
        public void AllowDescribeOn() {
            // Service descriptions should not fail when the DescribeUri is not null and AllowDescribe is true
            ts.Authorize = false;
            ts.AllowDescribe = true;
            dynamic doc = GetDocument("help");

            Assert.IsNotNull(ts.DescriptionUri);
            Assert.AreNotEqual(doc.status, "failed");
        }

        [TestMethod]
        public void AllowDescribeOff() {
            // Service description should fail when AllowDescribe is false
            ts.Authorize = false;
            ts.AllowDescribe = false;
            dynamic doc = GetDocument("help");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        public void AuthorizeOn() {
            // Attempting to invoke a method when Authorize is true (with no credentials) should fail
            ts.Authorize = true;
            dynamic doc = GetDocument("");

            Assert.AreEqual(doc.status, "failed");
            ts.Authorize = false;
        }

        [TestMethod]
        public void AuthorizeOnWithValidCreds() {
            // Attempting to invoke a method when Authorize is true (with valid credentials) should succeed
            ts.Authorize = true;
            dynamic doc = GetDocument("?apikey=anythingwillwork");

            Assert.AreNotEqual(doc.status, "failed");
            ts.Authorize = false;
        }

        [TestMethod]
        public void AuthorizeOff() {
            // Attempting to invoke a method when Authorize is false should succeed
            ts.Authorize = false;
            dynamic doc = GetDocument("?apikey=anythingwillwork");

            Assert.AreEqual(doc.status, "ok");
        }

        [TestMethod]
        public void HttpVerbReject() {
            // Incorrect verbs should be rejected
            dynamic doc = GetDocument("save?id=1");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        public void HttpVerbAccept() {
            // Correct verbs should be accepted
            dynamic doc = PostDocument("save?id=1", string.Empty);

            Assert.AreEqual(doc.status, "ok");
        }

        [TestMethod]
        public void MissingRequiredParameter() {
            // Missing parameters with no default values should cause a failure
            dynamic doc = GetDocument("add?value1=3");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        public void MissingNonRequiredParameter() {
            // Missing parameters that have a default value should succeed
            dynamic doc = GetDocument("mult?value2=3");

            Assert.AreEqual(doc.product, 0);
        }

        [TestMethod]
        public void PostingJsonDocument() {
            // Posting a json document to a service method that accepts one should succeed
            dynamic doc = PostDocument("save?id=2", "{ \"name\": \"Mike\", \"age\": 31 }");

            Assert.AreEqual(doc.status, "ok");
            Assert.AreEqual(doc.name, "Mike");
            Assert.AreEqual(doc.age, 31);
        }

        [TestMethod]
        public void InvalidJsonPosted() {
            // Rejecting a broken json document
            dynamic doc = PostDocument("save?id=3", "{ not valid json }");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        public void MultipartPaths() {
            // Paths in the UriTemplate should be respected
            dynamic doc = GetDocument("i/am/a/multipart/path?with=strings_and_things");

            Assert.AreEqual(doc.status, "ok");
            Assert.AreEqual(doc.vars, "strings_and_things");
        }

        [TestMethod]
        public void LeadingSlashIrrelevant() {
            // The appearance of a leading slash on the UriTemplate & Example properties of a VerbAttribute shouldn't matter.
            dynamic doc = GetDocument("/slashprefix");

            Assert.AreEqual(doc.status, "ok");
        }

        [TestMethod]
        public void CustomDescriptionPath() {
            // Custom set service description paths should be a-ok
            ts.Stop();
            ts.DescribePath = "/I/Need/Help";
            ts.AllowDescribe = true;
            ts.Start(false);

            dynamic doc = GetDocument("I/Need/Help");

            Assert.AreNotEqual(doc.status, "failed");

            ts.Stop();
            ts.DescribePath = "/help";
            ts.Start(false);
        }

        [TestMethod]
        public void ObjectSerialization() {
            // You don't have to return an anonymous object, whatever object returned should be properly serialized to json.
            dynamic doc = GetDocument("customobj");

            Assert.AreNotEqual(doc.status, "failed");
            Assert.AreEqual(doc.A, "Testing");
            Assert.AreEqual(doc.C, 1980);
            Assert.AreEqual(doc.D[2], "III");
            Assert.IsFalse(doc.F);
            Assert.IsNull(doc.E.B);
            Assert.IsTrue(doc.E.F);
        }

        [TestMethod]
        public void CustomStatusCode() {
            // Status code specifed when using WithStatusCode should be sent to the client
            try {
                dynamic doc = GetDocument("statuscode");
                Assert.Fail();
            } catch(WebException e) {
                Assert.AreEqual(((HttpWebResponse)e.Response).StatusCode, HttpStatusCode.NotImplemented);
            }
        }

        [TestMethod]
        public void MaxParameterMatching() {
            dynamic doc = GetDocument("op?a=5");

            Assert.AreEqual(doc.Value, 5);

            doc = GetDocument("op?a=5&b=10");

            Assert.AreEqual(doc.Value, 50);

            doc = GetDocument("op?a=5&b=7&c=3");

            Assert.AreEqual(doc.Value, 105);
        }
    }
}
