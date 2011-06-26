using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonWebService;
using System.ComponentModel;
using System.Security.Cryptography;
using System.IO;

namespace Testing {
    class Program {
        static void Main(string[] args) {
            ConsoleService ts = new ConsoleService();
            ts.AllowDescribe = true;
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

        [Get("/favicon.ico", Describe=false)]
        public object favico() {
            return Resource(System.IO.File.OpenRead(@"G:\Documents\strider.ico"), "image/x-icon");
        }

        [Get("/gravatar?email={email}", Example="/gravatar?email=striderIIDX@gmail.com")]
        public object Gravatar(string email) {
            byte[] rawHash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(email.ToLower()));
            string hash = BitConverter.ToString(rawHash).Replace("-", "").ToLower();
            return Resource(
                System.Net.WebRequest.Create("http://www.gravatar.com/avatar/" + hash).GetResponse().GetResponseStream(),
                "image/jpg");
        }
    }
}
