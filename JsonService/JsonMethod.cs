using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace JsonWebService {
    /// <summary>
    /// Acts as a bridge between the query and the service method
    /// </summary>
    class JsonMethod {
        public MethodInfo MethodInfo {
            get;
            set;
        }
        public VerbAttribute Attribute {
            get;
            set;
        }
        public bool IsMatch(string path) {
            return Attribute.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase);
        }
        public object[] GetArgs(System.Collections.Specialized.NameValueCollection qs) {
            List<object> args = new List<object>();

            foreach(var pm in MethodInfo.GetParameters()) {
                bool hasDefault = pm.DefaultValue != System.DBNull.Value;
                string key = Attribute.GetKey(pm.Name);

                if(!qs.AllKeys.Contains(key) && !hasDefault) {
                    throw new ArgumentException(key + " is required.", key, new Exception("Missing required parameter"));
                } else {
                    string val = qs[key];

                    if(val == null && hasDefault)
                        args.Add(pm.DefaultValue);
                    else {
                        try {
                            args.Add(Convert.ChangeType(val, pm.ParameterType));
                        } catch(Exception e) {
                            throw new ArgumentException("Failed to convert input to required type", key, e);
                        }
                    }
                }
            }

            return args.ToArray();
        }
    }
}
