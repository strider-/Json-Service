using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService
{
    /// <summary>
    /// Represents the DELETE http verb for json service requests.
    /// </summary>
    public class DeleteAttribute : VerbAttribute
    {
        /// <summary>
        /// Sets the request uri template for DELETE requests that this method will accept.
        /// </summary>
        /// <param name="UriTemplate">Sets the template for a service method call</param>
        public DeleteAttribute(string UriTemplate)
            : base(UriTemplate)
        {
        }
        /// <summary>
        /// Gets the http verb (DELETE)
        /// </summary>
        public override string Verb
        {
            get
            {
                return "DELETE";
            }
        }
    }
}