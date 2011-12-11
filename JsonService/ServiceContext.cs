using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;

namespace JsonWebService
{
    /// <summary>
    /// Custom context for incoming service requests
    /// </summary>
    class ServiceContext
    {
        JsonServiceBase _service;
        HttpListenerResponse _response;
        IDictionary<string, string> _headers;

        public ServiceContext(HttpListenerContext context, ServiceBridge bridge, JsonServiceBase service, IDictionary<string, string> customHeaders)
        {
            Timestamp = DateTime.Now;
            Request = context.Request;
            Bridge = bridge;
            _response = context.Response;
            _service = service;
            _headers = customHeaders ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Informs the service about how it needs to respond to the client.
        /// </summary>
        /// <returns></returns>
        public ResponseType DetermineResponse()
        {
            // describe the service to the client
            if(_service.AllowDescribe && _service.DescriptionUri != null && Request.Url.LocalPath.Equals(_service.DescriptionUri.LocalPath, StringComparison.InvariantCultureIgnoreCase))
                return ResponseType.Describe;

            // no matching method found
            if(Bridge == null)
                return ResponseType.NoMethod;

            // doesn't support generics in method parameters
            if(Bridge.MethodInfo.ContainsGenericParameters)
                return ResponseType.NoGenericSupport;

            // accessed the method using the wrong http verb
            if(!Bridge.Attribute.Verb.Equals(Request.HttpMethod, StringComparison.InvariantCultureIgnoreCase))
                return ResponseType.WrongVerb;

            // require authorization before invocation
            AuthorizeBeforeInvoke = _service.Authorize && !Bridge.Attribute.AllowUnauthorized;

            // all good, run that method
            return ResponseType.Invoke;
        }
        /// <summary>
        /// Returns json or resources to the client.
        /// </summary>
        /// <param name="content">The content to return to the client.</param>
        public void Respond(object content)
        {
            if(_service.IsRunning)
            {
                using(JsonServiceResult result = (content as JsonServiceResult) ?? new JsonServiceResult(content))
                {
                    var data = result.Content;

                    _response.StatusCode = (int)result.Code;
                    _response.StatusDescription = result.Code.ToString();
                    _response.ContentType = result.ContentType;
                    _headers.ToList().ForEach(kvp =>
                    {
                        _response.AddHeader(kvp.Key, kvp.Value);
                    });

                    if(data.CanSeek)
                    {
                        data.Position = 0;
                        _response.ContentLength64 = data.Length;
                    }
                    data.CopyTo(_response.OutputStream);
                    _response.Close();
                    data.Close();
                }
            }
        }

        /// <summary>
        /// Maps query string values to the service method parameters.
        /// </summary>
        /// <exception cref="System.ArgumentException">Parameter is invalid type or cannot be converted to expected type.</exception>
        /// <exception cref="JsonWebService.JsonParserException">The json posted to the service is not valid or could not be parsed.</exception>
        /// <returns></returns>
        public Dictionary<string, object> GetMappedParameters()
        {
            var qs = Request.QueryString;
            var result = new Dictionary<string, object>();

            foreach(var param in Bridge.MethodInfo.GetParameters().OrderBy(p => p.Position))
            {
                object value = null;

                if(Bridge.Attribute is EntityAttribute && param.Name.Equals(((EntityAttribute)Bridge.Attribute).EntityDocument))
                {
                    if(Request.HasEntityBody)
                    {
                        try
                        {
                            dynamic jsonDoc;
                            using(StreamReader sr = new StreamReader(Request.InputStream))
                            {
                                jsonDoc = JsonDocument.Parse(sr.ReadToEnd());
                            }
                            value = jsonDoc;
                        }
                        catch
                        {
                            throw new JsonParserException();
                        }
                    }
                }
                else
                {
                    string key = Bridge.Attribute.GetParameterName(param.Name);
                    string val = qs[key];



                    if(val == null && param.DefaultValue != System.DBNull.Value)
                    {
                        value = param.DefaultValue;
                    }
                    else
                    {
                        try
                        {
                            value = Convert.ChangeType(val, param.ParameterType);
                        }
                        catch(Exception e)
                        {
                            string msg = string.Format("Cannot convert value '{0}' to {1}", val, param.ParameterType.Name.ToLower());
                            ArgumentException ae = new ArgumentException(msg, key, e);
                            ae.Data["value"] = val;
                            ae.Data["expected_type"] = param.ParameterType;
                            throw ae;
                        }
                    }
                }

                result[param.Name] = value;
            }
            return result;
        }
        /// <summary>
        /// Gets the http request
        /// </summary>
        public HttpListenerRequest Request
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets the date &amp; time this request was initiated
        /// </summary>
        public DateTime Timestamp
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets the total time in milliseconds it took to process the request.
        /// </summary>
        public double ProcessingTime
        {
            get
            {
                return DateTime.Now.Subtract(Timestamp).TotalMilliseconds;
            }
        }
        /// <summary>
        /// Gets the bridge between the service method and the http request
        /// </summary>
        public ServiceBridge Bridge
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets whether or not to authorize the request before invoking the requested method
        /// </summary>
        public bool AuthorizeBeforeInvoke
        {
            get;
            private set;
        }
    }
}