using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace JsonWebService
{
    /// <summary>
    /// The exception that is thrown when a placeholder in a UriTemplate does not exist in the method signature. Placeholders have to match a method parameter name.
    /// </summary>
    [Serializable]
    public class InvalidPlaceholderException : Exception, ISerializable
    {
        public InvalidPlaceholderException(string method, string[] placeholders)
            : base("The UriTemplate of the VerbAttribute for the " + method + " method specifies a placeholder that is not defined.")
        {
            this.MethodName = method;
        }
        protected InvalidPlaceholderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if(info != null)
            {
                info.AddValue("MethodName", MethodName);
                info.AddValue("Placeholders", Placeholders);
            }
        }
        /// <summary>
        /// Sets the System.Runtime.Serialization.SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or destination</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if(info != null)
            {
                info.AddValue("MethodName", MethodName);
                info.AddValue("Placeholders", Placeholders);
            }
        }
        /// <summary>
        /// Gets the name of the method with the invalid placeholder in the UriTemplate of the VerbAttribute
        /// </summary>
        public string MethodName
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets the placeholders that are not defined in the method signature
        /// </summary>
        public string[] Placeholders
        {
            get;
            private set;
        }
    }
}