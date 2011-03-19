using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService {
    /// <summary>
    /// Represents the POST http verb for json service requests.
    /// </summary>
    public class PostAttribute : VerbAttribute {
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
        /// <summary>
        /// Gets and sets the parameter a posted json document will be put into, if any.  The parameter should be declared as dynamic.
        /// </summary>
        public string PostedDocument {
            get;
            set;
        }
    }  
}
