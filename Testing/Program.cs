using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonWebService;

namespace Testing {
    class Program {
        static void Main(string[] args) {
            TestService ts = new TestService();
            ts.Start(true);
            while(Console.ReadKey(true).Key != ConsoleKey.Escape)
                ;
            ts.Stop();
        }
    }

    class TestService : JsonService {
        [Get("/", Description = "Service status OK!", Example = "/")]
        public object Root() {
            return new {
                status = "ok",
                message = "service is up and running!",
                helpurl = DescriptionUri == null ? null : DescriptionUri.AbsoluteUri
            };
        }
    }
}
