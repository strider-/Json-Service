using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService {
    /// <summary>
    /// Represents the POST http verb for json service requests.
    /// </summary>
    public class PostAttribute : EntityAttribute {
        /// <summary>
        /// Sets the request uri template for POST requests that this method will accept.
        /// </summary>
        /// <param name="UriTemplate">Sets the template for a service method call</param>
        public PostAttribute(string UriTemplate)
            : base(UriTemplate) {
        }
        /// <summary>
        /// Gets the http verb (POST)
        /// </summary>
        public override string Verb {
            get {
                return "POST";
            }
        }
    }  
}
