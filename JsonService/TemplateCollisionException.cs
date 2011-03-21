using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace JsonWebService {
    /// <summary>
    /// The exception that is thrown when multiple methods share the same VerbAttribute with identical paths &amp; parameter names.
    /// </summary>
    [Serializable]
    public class TemplateCollisionException : Exception, ISerializable {
        public TemplateCollisionException(string path, string[] parameterNames, string verb, string[] methods)
            : base("Two or more methods share an identical UriTemplate, with the same VerbAttribute.") {
                this.Path = path;
                this.ParameterNames = parameterNames;
                this.Verb = verb;
                this.Methods = methods;
        }
        protected TemplateCollisionException(SerializationInfo info, StreamingContext context)
            : base(info, context) {
                if(info != null) {
                    Path = info.GetString("Path");
                    ParameterNames = (string[])info.GetValue("ParameterNames", typeof(string[]));
                    Verb = info.GetString("Verb");
                    Methods = (string[])info.GetValue("Methods", typeof(string[]));
                }
        }
        /// <summary>
        /// Sets the System.Runtime.Serialization.SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or destination</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);

            if(info != null) {
                info.AddValue("Path", Path);
                info.AddValue("ParameterNames", ParameterNames, typeof(string[]));
                info.AddValue("Verb", Verb);
                info.AddValue("Methods", Methods, typeof(string[]));
            }
        }
        /// <summary>
        /// Gets the path of the collision
        /// </summary>
        public string Path {
            get;
            private set;
        }
        /// <summary>
        /// Gets the parameter names of the collision
        /// </summary>
        public string[] ParameterNames {
            get;
            private set;
        }
        /// <summary>
        /// Gets the http verb of the collision
        /// </summary>
        public string Verb {
            get;
            private set;
        }
        /// <summary>
        /// Gets the class methods that share the Path, ParameterNames &amp; Verb
        /// </summary>
        public string[] Methods {
            get;
            private set;
        }
    }
}
