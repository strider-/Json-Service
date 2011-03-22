using JsonWebService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;

namespace UnitTests {
    /// <summary>
    ///This is a test class for JsonDocumentTest and is intended
    ///to contain all JsonDocumentTest Unit Tests
    ///</summary>
    [TestClass()]
    public class JsonDocumentTest {

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
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        ///A test for JsonDocument Constructor
        ///</summary>
        [TestMethod()]
        public void JsonDocumentConstructorTest1() {
            object content = new {
                content = ""
            };
            JsonDocument.JsonFormat Formatting = JsonDocument.JsonFormat.Spaces;
            int IndentSize = 3;
            JsonDocument target = new JsonDocument(content, Formatting, IndentSize);

            Assert.AreEqual(target.Formatting, JsonDocument.JsonFormat.Spaces);
            Assert.AreEqual(target.IndentSize, 3);
        }

        /// <summary>
        ///A test for JsonDocument Constructor
        ///</summary>
        [TestMethod()]
        public void JsonDocumentConstructorTest2() {
            object content = new {
                content = ""
            };
            JsonDocument target = new JsonDocument(content);

            Assert.AreEqual(target.Formatting, JsonDocument.JsonFormat.Tabs);
            Assert.AreEqual(target.IndentSize, 4);
        }

        /// <summary>
        ///A test for NextToken
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void NextTokenTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.json = "{ \"content\": \"string\" }".ToCharArray();
            JsonDocument_Accessor.JsonToken expected = JsonDocument_Accessor.JsonToken.OpenBrace;
            JsonDocument_Accessor.JsonToken actual = target.NextToken();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for Parse
        ///</summary>
        [TestMethod()]
        public void ParseTest() {
            string JsonObject = "{ \"content\": \"string\", \"type\": 3 }";
            dynamic actual = JsonDocument.Parse(JsonObject);

            Assert.AreEqual(actual.content, "string");
            Assert.AreEqual(actual.type, 3);
        }

        /// <summary>
        ///A test for PeekToken
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void PeekTokenTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.json = "[ 0, 1, \"2\", true, null ]".ToCharArray();
            JsonDocument_Accessor.JsonToken expected = JsonDocument_Accessor.JsonToken.OpenBracket;
            JsonDocument_Accessor.JsonToken actual = target.PeekToken();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for ToString
        ///</summary>
        [TestMethod()]
        public void ToStringTest() {
            JsonDocument target = new JsonDocument(new {
                name = "Mike",
                wife = new {
                    name = "Cheryl"
                }
            });
            target.Formatting = JsonDocument.JsonFormat.None;
            string expected = "{\"name\": \"Mike\",\"wife\": {\"name\": \"Cheryl\"}}";
            string actual = target.ToString();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for clean
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void cleanTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            string value = "\r\n\t";
            string expected = "\\r\\n\\t";
            string actual = target.clean(value);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for consumeWhiteSpace
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void consumeWhiteSpaceTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor(); 
            int index = target.parseIndex;
            target.json = " \r \t 0, 1, \"2\", true, null ]".ToCharArray();
            target.consumeWhiteSpace();

            Assert.AreEqual(target.parseIndex, 5);
        }

        /// <summary>
        ///A test for getJsonArray
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void getJsonArrayTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.Formatting = JsonDocument.JsonFormat.None;
            IEnumerable array = new object[] { 0, 1.1, true, false, null, "test" };

            string expected = "[0,1.1,true,false,null,\"test\"]";
            string actual = target.getJsonArray(array, 0);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for getJsonObject
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void getJsonObjectTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.Formatting = JsonDocument.JsonFormat.None;
            object obj = new {
                name = "Mike",
                age = 31,
                sucks = true
            };

            string expected = "{\"name\": \"Mike\",\"age\": 31,\"sucks\": true}";
            string actual = target.getJsonObject(obj, 0);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for getJsonValue
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void getJsonValueTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.Formatting = JsonDocument.JsonFormat.None;

            object o = "Mike";
            string expected = "\"Mike\"";
            string actual = target.getJsonValue(o, 0);
            Assert.AreEqual(expected, actual);

            o = 3.14;
            expected = "3.14";
            actual = target.getJsonValue(o, 0);
            Assert.AreEqual(expected, actual);

            o = false;
            expected = "false";
            actual = target.getJsonValue(o, 0);
            Assert.AreEqual(expected, actual);

            o = new object[] { 0, "1", true };
            expected = "[0,\"1\",true]";
            actual = target.getJsonValue(o, 0);
            Assert.AreEqual(expected, actual);

            o = new {
                value = "test"
            };
            expected = "{\"value\": \"test\"}";
            actual = target.getJsonValue(o, 0);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for isArray
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void isArrayTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            object obj = new bool[] { true, false };
            bool expected = true;
            bool actual = target.isArray(obj);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for parseArray
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void parseArrayTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.Formatting = JsonDocument.JsonFormat.None;
            target.json = "[0,1.1,true,\"null\"]".ToCharArray();
            ArrayList expected = new ArrayList(new object[] { 0.0, 1.1, true, "null" });
            ArrayList actual = target.parseArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for parseNumber
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void parseNumberTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.Formatting = JsonDocument.JsonFormat.None;
            target.json = "3.14159] ".ToCharArray();
            double expected = 3.14159d;
            double actual = target.parseNumber();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for parseObject
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void parseObjectTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.Formatting = JsonDocument.JsonFormat.None;
            target.json = "{\"key\": \"value\",\"bool\": false}".ToCharArray();

            Hashtable expected = new Hashtable();
            expected["key"] = "value";
            expected["bool"] = false;
            Hashtable actual = target.parseObject();

            Assert.AreEqual(expected["key"], actual["key"]);
            Assert.AreEqual(expected["bool"], actual["bool"]);
        }

        /// <summary>
        ///A test for parseString
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void parseStringTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.Formatting = JsonDocument.JsonFormat.None;
            target.json = "\"This \r is \n a string\t\"".ToCharArray();
            string expected = "This \r is \n a string\t";
            string actual = target.parseString();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for parseValue
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void parseValueTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor();
            target.Formatting = JsonDocument.JsonFormat.None;
            target.json = "{\"key\": false}".ToCharArray();
            Hashtable expected = new Hashtable();
            expected["key"] = false;
            Hashtable actual = (Hashtable)target.parseValue();
            Assert.AreEqual(expected["key"], actual["key"]);
        }

        /// <summary>
        ///A test for tabString
        ///</summary>
        [TestMethod()]
        [DeploymentItem("JsonService.dll")]
        public void tabStringTest() {
            JsonDocument_Accessor target = new JsonDocument_Accessor(); 
            target.Formatting = JsonDocument.JsonFormat.Spaces;
            target.IndentSize = 2;
            int count = 4;
            string expected = "        ";
            string actual = target.tabString(count);
            Assert.AreEqual(expected, actual);

            target.Formatting = JsonDocument.JsonFormat.Tabs;
            target.IndentSize = 3;
            count = 2;
            expected = "\t\t";
            actual = target.tabString(count);
            Assert.AreEqual(expected, actual);
        }
    }
}
