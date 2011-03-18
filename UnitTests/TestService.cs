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

        protected override bool AuthorizeRequest(System.Net.HttpListenerRequest Request) {
            return Request.QueryString["apikey"] != null;
        }
    }
}
