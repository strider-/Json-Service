using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonWebService;

namespace UnitTests {
    class TestService : JsonService {
        [Get("/", Description = "Service status OK!", Example = "/")]
        public object Root() {
            return new {
                status = "ok",
                message = "service is up and running!",
                helpurl = Uri.AbsoluteUri + "help"
            };
        }

        [Get("add?value1={a}&value2={b}", Description = "Returns the sum of 2 numbers.", Example = "add?value1=3&value2=5")]
        public object Sum(int a, int b) {
            return new {
                sum = a + b
            };
        }

        [Get("mult?value1={a}&value2={b}", Description = "Returns the product of 2 numbers.", Example = "mult?value1=2&value2=7")]
        public object Product(int a = 0, int b = 0) {
            return new {
                product = a * b
            };
        }

        [Post("save?id={id}", Description = "Updates a record.", Example = "save?id=0", PostedDocument = "document")]
        public object Update(int id, dynamic document) {
            return new {
                status = "ok",
                message = "record updated",
                name = document == null ? "" : document.name,
                age = document == null ? 0 : document.age
            };
        }

        protected override bool AuthorizeRequest(System.Net.HttpListenerRequest Request) {
            return Request.QueryString["apikey"] != null;
        }
    }
}
