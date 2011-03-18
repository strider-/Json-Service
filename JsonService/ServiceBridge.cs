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
        /// <returns></returns>
        public Tuple<object[], string[]> MapParameters(System.Collections.Specialized.NameValueCollection qs) {
            var parms = MethodInfo.GetParameters().ToArray();
            Tuple<object[], string[]> result = new Tuple<object[], string[]>(
                new object[parms.Length],
                new string[parms.Length]
            );

            for(int i = 0; i < parms.Length; i++) {
                var pm = parms[i];
                bool hasDefault = pm.DefaultValue != System.DBNull.Value;
                string key = Attribute.GetParameterName(pm.Name);
                result.Item2[i] = pm.Name;

                if(!qs.AllKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase) && !hasDefault) {
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
                }
            }

            return result;
        }
    }
}
