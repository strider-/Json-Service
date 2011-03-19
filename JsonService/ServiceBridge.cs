using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace JsonWebService {
    /// <summary>
    /// Acts as a bridge between the query and the service method
    /// </summary>
    class ServiceBridge {
        /// <summary>
        /// Gets the metadata for the service method
        /// </summary>
        public MethodInfo MethodInfo {
            get;
            set;
        }
        /// <summary>
        /// Gets the attribute information for the service method
        /// </summary>
        public VerbAttribute Attribute {
            get;
            set;
        }
        /// <summary>
        /// Returns whether or not this method matches the method that was requested.
        /// </summary>
        /// <param name="path">Path the client requested</param>
        /// <returns></returns>
        public bool IsMatch(string path) {
            return Attribute.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Returns a service method parameter information based on its query string parameter name.
        /// </summary>
        /// <param name="ParameterName">Parameter name in the query string.</param>
        /// <returns></returns>
        public ParameterInfo GetParameterInfo(string ParameterName) {
            return MethodInfo.GetParameters().SingleOrDefault(p => p.Name == Attribute.GetPlaceholder(ParameterName));
        }
        /// <summary>
        /// Maps query string values to the service method parameters.
        /// </summary>
        /// <param name="qs">Query string the client passed in.</param>
        /// <returns>Item1: parameter values, Item2: parameter names, Item3: logging strings</returns>
        public Tuple<object[], string[], string[]> MapParameters(System.Collections.Specialized.NameValueCollection qs, dynamic postedDocument) {
            var parms = MethodInfo.GetParameters().OrderBy(p => p.Position).ToArray();
            var result = Tuple.Create(
                new object[parms.Length],
                parms.Select(p => p.Name).ToArray(),
                new string[parms.Length]
            );
            
            for(int i = 0; i < parms.Length; i++) {
                var pm = parms[i];
                bool hasDefault = pm.DefaultValue != System.DBNull.Value;
                string key = Attribute.GetParameterName(pm.Name);

                if(key == null && Attribute is PostAttribute) {
                    if(pm.Name.Equals(((PostAttribute)Attribute).PostedDocument))
                        result.Item1[i] = postedDocument;
                } else if(!qs.AllKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase) && !hasDefault) {
                    throw new ArgumentException(key + " is required.", key, new Exception("Missing required parameter"));
                } else {
                    string val = qs[key];

                    if(val == null && hasDefault) {
                        result.Item1[i] = pm.DefaultValue;
                    } else {
                        try {
                            result.Item1[i] = Convert.ChangeType(val, pm.ParameterType);
                        } catch(Exception e) {
                            throw new ArgumentException("Failed to convert input to required type", key, e);
                        }
                    }

                    result.Item3[i] = string.Format("{0}={1}", result.Item2[i], result.Item1[i]);
                }
            }

            return result;
        }
        /// <summary>
        /// Returns the Uri for an example use, if an example was specified.
        /// </summary>
        /// <param name="ServiceUri">Uri the service is running on</param>
        /// <returns></returns>
        public Uri GetExampleUri(Uri ServiceUri) {
            Uri result = null;

            if(string.IsNullOrWhiteSpace(Attribute.Example) || !Uri.TryCreate(ServiceUri, Attribute.Example, out result))
                return null;           
                
            return result;
        }
    }
}
