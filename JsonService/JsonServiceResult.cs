using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace JsonWebService {
    internal class JsonServiceResult : IDisposable {
        Stream _data;
        string _ctype;
        HttpStatusCode _code = HttpStatusCode.OK;

        internal JsonServiceResult(object content) {
            JsonDocument doc = new JsonDocument(content);
            doc.Formatting = JsonDocument.JsonFormat.None;

            _data = new MemoryStream(Encoding.UTF8.GetBytes(doc.ToString()));
            _ctype = "application/json";
        }

        internal JsonServiceResult(Stream data, string contentType) {
            _data = data;
            _ctype = contentType;
        }

        public void Dispose() {
            if(_data != null)
                _data.Dispose();
            _data = null;
        }

        public string ContentType {
            get {
                return _ctype;
            }
        }

        public HttpStatusCode Code {
            get {
                return _code;
            }
            set {
                _code = value;
            }
        }
		public Stream Content
		{
			get
			{
				return _data;
			}
		}
    }
}
