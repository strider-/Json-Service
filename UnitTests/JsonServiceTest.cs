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
        [Description("Calling a method directly (with no parameters) should return an anon object")]
        public void NoParameterMethodCall() {
            Assert.IsNotNull(ts.Root());
        }

        [TestMethod]
        [Description("Invoking the method via service (with no parameters) should return a json document")]
        public void NoParameterServiceCall() {
            dynamic doc = GetDocument("");

            Assert.AreEqual(doc.status, "ok");
        }

        [TestMethod]
        [Description("Calling a method directly (with parameters) should return an anon object")]
        public void ParameterMethod() {
            Assert.IsNotNull(ts.Sum(2, 2));
        }

        [TestMethod]
        [Description("Invoking the method via service (with parameters) should return a json document")]
        public void ParameterService() {
            dynamic doc = GetDocument("add?value1=2&value2=3");

            Assert.AreEqual(doc.sum, 5);
        }

        [TestMethod]
        [Description("Invalid parameter types should fail")]
        public void ParameterTypeFailure() {
            dynamic doc = GetDocument("add?value1=X&value2=Y");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        [Description("The casing of the parameter names shouldn't matter")]
        public void ParameterNameCasingIrrelevant() {
            dynamic doc = GetDocument("add?VALUE1=3&vAlUe2=10");

            Assert.AreEqual(doc.sum, 13);
        }

        [TestMethod]
        [Description("Attempting to invoke a method that does not exist should fail")]
        public void InvalidCall() {
            dynamic doc = GetDocument("idontexist");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        [Description("An unhandled exception in the invoked method should fail, but still return json to the client")]
        public void CallFailure() {
            dynamic doc = GetDocument("gonnafail");

            Assert.AreEqual(doc.status, "failed");
            Assert.AreEqual(doc.error, "You should still get a json response with an unhandled exception.");
        }

        [TestMethod]
        [Description("Service descriptions should not fail when the DescribeUri is not null and AllowDescribe is true")]
        public void AllowDescribeOn() {
            ts.Authorize = false;
            ts.AllowDescribe = true;
            dynamic doc = GetDocument("help");

            Assert.IsNotNull(ts.DescriptionUri);
            Assert.AreNotEqual(doc.status, "failed");
        }

        [TestMethod]
        [Description("Service description should fail when AllowDescribe is false")]
        public void AllowDescribeOff() {
            ts.Authorize = false;
            ts.AllowDescribe = false;
            dynamic doc = GetDocument("help");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        [Description("Attempting to invoke a method when Authorize is true (with no credentials) should fail")]
        public void AuthorizeOn() {
            ts.Authorize = true;
            dynamic doc = GetDocument("");

            Assert.AreEqual(doc.status, "failed");
            ts.Authorize = false;
        }

        [TestMethod]
        [Description("Attempting to invoke a method when Authorize is true (with valid credentials) should succeed")]
        public void AuthorizeOnWithValidCreds() {
            ts.Authorize = true;
            dynamic doc = GetDocument("?apikey=anythingwillwork");

            Assert.AreNotEqual(doc.status, "failed");
            ts.Authorize = false;
        }

        [TestMethod]
        [Description("Attempting to invoke a method when Authorize is false should succeed")]
        public void AuthorizeOff() {
            ts.Authorize = false;
            dynamic doc = GetDocument("?apikey=anythingwillwork");

            Assert.AreEqual(doc.status, "ok");
        }

        [TestMethod]
        [Description("Incorrect verbs should be rejected")]
        public void HttpVerbReject() {
            dynamic doc = GetDocument("save?id=1");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        [Description("Correct verbs should be accepted")]
        public void HttpVerbAccept() {
            dynamic doc = PostDocument("save?id=1", string.Empty);

            Assert.AreEqual(doc.status, "ok");
        }

        [TestMethod]
        [Description("Missing parameters with no default values should cause a failure")]
        public void MissingRequiredParameter() {
            dynamic doc = GetDocument("add?value1=3");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        [Description("Missing parameters that have a default value should succeed")]
        public void MissingNonRequiredParameter() {
            dynamic doc = GetDocument("mult?value2=3");

            Assert.AreEqual(doc.product, 0);
        }

        [TestMethod]
        [Description("Posting a json document to a service method that accepts one should succeed")]
        public void PostingJsonDocument() {
            dynamic doc = PostDocument("save?id=2", "{ \"name\": \"Mike\", \"age\": 31 }");

            Assert.AreEqual(doc.status, "ok");
            Assert.AreEqual(doc.name, "Mike");
            Assert.AreEqual(doc.age, 31);
        }

        [TestMethod]
        [Description("Rejecting a broken json document")]
        public void InvalidJsonPosted() {
            dynamic doc = PostDocument("save?id=3", "{ not valid json }");

            Assert.AreEqual(doc.status, "failed");
        }

        [TestMethod]
        [Description("Paths in the UriTemplate should be respected")]
        public void MultipartPaths() {
            dynamic doc = GetDocument("i/am/a/multipart/path?with=strings_and_things");

            Assert.AreEqual(doc.status, "ok");
            Assert.AreEqual(doc.vars, "strings_and_things");
        }

        [TestMethod]
        [Description("The appearance of a leading slash on the UriTemplate & Example properties of a VerbAttribute shouldn't matter.")]
        public void LeadingSlashIrrelevant() {
            dynamic doc = GetDocument("/slashprefix");

            Assert.AreEqual(doc.status, "ok");
        }

        [TestMethod]
        [Description("Custom set service description paths should be a-ok")]
        public void CustomDescriptionPath() {
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
        [Description("You don't have to return an anonymous object, whatever object returned should be properly serialized to json.")]
        public void ObjectSerialization() {
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
        [Description("Status code specifed when using WithStatusCode should be sent to the client")]
        public void CustomStatusCode() {
            try {
                dynamic doc = GetDocument("statuscode");
                Assert.Fail();
            } catch(WebException e) {
                Assert.AreEqual(((HttpWebResponse)e.Response).StatusCode, HttpStatusCode.NotImplemented);
            }
        }

        [TestMethod]
        [Description("Template matching should always match the method that has the most number of required parameters met.")]
        public void MaxParameterMatching() {
            dynamic doc = GetDocument("op?a=5");

            Assert.AreEqual(doc.Value, 5);

            doc = GetDocument("op?a=5&b=10");

            Assert.AreEqual(doc.Value, 50);

            doc = GetDocument("op?a=5&b=7&c=3");

            Assert.AreEqual(doc.Value, 105);
        }

        [TestMethod]
        [Description("Making sure logging output happens when the LogOutput is not null.")]
        public void Logging() {
            const int entry_count = 4;

            JsonService_Accessor target = new JsonService_Accessor(new PrivateObject(ts));
            StringBuilder sb = new StringBuilder();
            using(StringWriter sw = new StringWriter(sb)) {
                target.LogOutput = sw;
                target.Log(JsonService_Accessor.LogLevel.Info, "Logging Works: {0}", true);
            }
            
            string[] log = sb.ToString().Split('\t');
            if(log.Length != entry_count)
                Assert.Fail("Log entry has an incorrect number of fields. Count: {0}, Expected: {1}", log.Length, entry_count);

            DateTime dt;
            Assert.IsTrue(DateTime.TryParse(log[0], out dt));
            Assert.AreEqual(log[1], "User");
            Assert.AreEqual(log[2], "Info");
            Assert.IsTrue(log[3].StartsWith("Logging Works: True"));
            Assert.IsTrue(log[3].EndsWith("\n"));
        }
    }
}
