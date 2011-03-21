using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace JsonWebService {
    /// <summary>
    /// The exception that is thrown when multiple methods share the same VerbAttribute with identical paths &amp; parameter names.
    /// </summary>
    public class TemplateCollisionException : Exception, ISerializable {
        public TemplateCollisionException(string path, string[] parameterNames, string verb, string[] methods)
            : base("Two or more methods share an identical UriTemplate, with the same VerbAttribute.") {
                this.Path = path;
                this.ParameterNames = parameterNames;
                this.Verb = verb;
                this.Methods = methods;
        }
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Path", Path);
            info.AddValue("ParameterNames", ParameterNames, typeof(Array));
            info.AddValue("Verb", Verb);
            info.AddValue("Methods", Methods, typeof(Array));
            base.GetObjectData(info, context);
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
