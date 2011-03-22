using System;
using System.Net;
using JsonWebService;

namespace UnitTests {
    class MockService : JsonService {
        [Get("/", Description = "Service status OK!", Example = "/")]
        public object Root() {
            return new {
                status = "ok",
                message = "service is up and running!",
                helpurl = Uri.AbsoluteUri + "help"
            };
        }

        [Get("add?value1={a}&value2={b}")]
        public object Sum(int a, int b) {
            return new {
                sum = a + b
            };
        }

        [Get("mult?value1={a}&value2={b}")]
        public object Product(int a = 0, int b = 0) {
            return new {
                product = a * b
            };
        }

        [Get("/op?a={x}")]
        public object OpA(int x) {
            return x;
        }
        [Get("/op?a={x}&b={y}")]
        public object OpB(int x, int y) {
            return x * y;
        }
        [Get("/op?a={x}&b={y}&c={z}")]
        public object OpC(int x, int y, int z) {
            return x * y * z;
        }

        [Get("/slashprefix")]
        public object Slash() {
            return new {
                status = "ok",
                message = "lead with a slash or don't."
            };
        }

        [Get("/gonnafail")]
        public object ThrowsException() {
            throw new Exception("You should still get a json response with an unhandled exception.");
        }

        [Get("i/am/a/multipart/path?with={vars}")]
        public object PathCheck(string vars) {
            return new {
                status = "ok",
                message = "multi-part paths are a-ok",
                vars = vars
            };
        }

        [Post("save?id={id}", PostedDocument = "document")]
        public object Update(int id, dynamic document) {
            return new {
                status = "ok",
                message = "record updated",
                name = document == null ? "" : document.name,
                age = document == null ? 0 : document.age
            };
        }

        [Get("/customobj")]
        public object CustomObject() {
            CustomObject obj = new CustomObject {
                A = "Testing",
                B = "Obj",
                C = 1980,
                D = new string[] { "I", "II", "III", "IV", "V" },
                E = new CustomObject {
                    A = "B should be null.",
                    D = new string[] { "1", "2", "3", "4", "5" },
                    F = true
                },
                F = false,
                G = 3.14f,
                H = '!'
            };

            return obj;
        }

        [Get("/statuscode")]
        public object StatusCode() {
            return WithStatusCode(null, HttpStatusCode.NotImplemented);
        }

        protected override bool AuthorizeRequest(System.Net.HttpListenerRequest Request) {
            return Request.QueryString["apikey"] != null;
        }
    }
}
