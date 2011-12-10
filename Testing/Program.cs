using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonWebService;
using System.ComponentModel;
using System.Security.Cryptography;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleService ts = new ConsoleService();
            ts.AllowDescribe = true;
            ts.OpenBrowserOnStart = true;

            ts.RunWhile(() => Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }

    class ConsoleService : JsonService
    {
        [Get("/", Description = "Service status OK!", Example = "/")]
        public object Root()
        {
            return new
            {
                status = "ok",
                message = "service is up and running!",
                helpurl = DescriptionUri == null ? null : DescriptionUri.AbsoluteUri
            };
        }

        [Get("/favicon.ico", Describe = false)]
        public object favico()
        {
            return Resource(System.IO.File.OpenRead(@"G:\Documents\strider.ico"), "image/x-icon");
        }

        [Get("/xml", Example = "/xml")]
        public object XmlTest()
        {
            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("root",
                    new XElement("greeting", "hi.")
                )
            );
            return Xml(doc);
        }

        [Get("/add?num1={x}&num2={y}")]
        public object Add(int x, int y = 0)
        {
            return new { result = x + y };
        }

        public object Xml(XDocument document)
        {
            // This is a JSON based web service, ya jerk!
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using(XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                document.WriteTo(writer);
            }
            return Resource(ms, "text/xml");
        }

        [Get("/gravatar?email={email}", Example = "/gravatar?email=1@2.com")]
        public object Gravatar(string email)
        {
            byte[] rawHash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(email.ToLower()));
            string hash = rawHash.Select(b => b.ToString("x2")).Aggregate((c, n) => c += n);
            return Resource(
                System.Net.WebRequest.Create("http://www.gravatar.com/avatar/" + hash).GetResponse().GetResponseStream(),
                "image/jpg");
        }
    }
}