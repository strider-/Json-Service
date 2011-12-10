using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService
{
    /// <summary>
    /// Represents the PUT http verb for json service requests.
    /// </summary>
    public class PutAttribute : EntityAttribute
    {
        /// <summary>
        /// Sets the request uri template for PUT requests that this method will accept.
        /// </summary>
        /// <param name="UriTemplate">Sets the template for a service method call</param>
        public PutAttribute(string UriTemplate)
            : base(UriTemplate)
        {
        }
        /// <summary>
        /// Gets the http verb (PUT)
        /// </summary>
        public override string Verb
        {
            get
            {
                return "PUT";
            }
        }
    }
}