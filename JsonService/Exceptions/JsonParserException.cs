using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace JsonWebService
{
    /// <summary>
    /// The exception that is thrown when a json document that has been posted to the server cannot be parsed.
    /// </summary>
    [Serializable]
    public class JsonParserException : Exception
    {
        public JsonParserException()
            : base()
        {
        }

        protected JsonParserException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}