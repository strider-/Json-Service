using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonWebService;

namespace Testing {
    class Program {
        static void Main(string[] args) {
            ConsoleService ts = new ConsoleService();
            ts.Start(true);
            while(Console.ReadKey(true).Key != ConsoleKey.Escape)
                ;
            ts.Stop();
        }
    }

    class ConsoleService : JsonService {
        [Get("/", Description = "Service status OK!", Example = "/")]
        public object Root() {
            return new {
                status = "ok",
                message = "service is up and running!",
                helpurl = DescriptionUri == null ? null : DescriptionUri.AbsoluteUri
            };
        }

        [Get("/test?id={x}")]
        public object A(int x) {
            return x;
        }
        [Get("/test?a={x}&b={y}")]
        public object B(int x, int y=0) {
            return x * y;
        }
        [Get("/test?c={x}&d={y}&e={z}")]
        public object C(int x, int y, int z) {
            return (x * y) + z;
        }
    }
}
