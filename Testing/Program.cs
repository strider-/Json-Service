﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonWebService;
using System.ComponentModel;

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
    }
}
