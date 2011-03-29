using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService {
    /// <summary>
    /// Represents HTTP verbs that allow for request entities.
    /// </summary>
    public abstract class EntityAttribute : VerbAttribute {
        public EntityAttribute(string UriTemplate)
            : base(UriTemplate) {
        }
        /// <summary>
        /// Gets and sets the parameter a json document entity will be put into, if any.  The parameter should be declared as dynamic.
        /// </summary>
        public string EntityDocument {
            get;
            set;
        }
    }
}
