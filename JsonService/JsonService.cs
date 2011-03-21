using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Reflection;
using System.IO;

namespace JsonWebService {
    /// <summary>
    /// Abstract class for json based web services.
    /// </summary>
    public abstract class JsonService {
        IEnumerable<ServiceBridge> methods;
        HttpListener listener;

        public JsonService() {
            this.Host = "+";
            this.Port = 5678;
            this.Authorize = false;
            this.AllowDescribe = true;
            this.DescribePath = "/help";
            this.LogOutput = Console.Out;
            this.listener = new HttpListener();
        }

        /// <summary>
        /// Starts the service, without opening the default browser.
        /// </summary>
        public void Start() {
            Start(false);
        }
        /// <summary>
        /// Starts the service, with the option to open the default browser.
        /// </summary>
        public void Start(bool OpenBrowser) {
            if(!listener.IsListening) {
                InitService();

                listener.Start();
                listener.BeginGetContext(NewRequest, null);                
                Log("Server started @ " + Uri.AbsoluteUri);
                
                if(AllowDescribe && DescriptionUri != null)
                    Log("Service description @ " + DescriptionUri.AbsoluteUri);
                else
                    Log("Service description is not available.");

                if(OpenBrowser)
                    System.Diagnostics.Process.Start(Uri.AbsoluteUri);
            }
        }
        /// <summary>
        /// Stops the service
        /// </summary>
        public void Stop() {
            if(listener.IsListening) {
                listener.Stop();
                Uri = null;
                DescriptionUri = null;
                Log("Server stopped");
            }
        }

        void InitService() {
            if(!System.Net.HttpListener.IsSupported)
                throw new ApplicationException("HttpListener is not supported on this version of Windows.");
            if(string.IsNullOrWhiteSpace(Host))
                Host = "+";
            if(this.Port <= 0 || this.Port > 65535)
                throw new ArgumentException("Port must be greater than 0 and less than 65535");

            Log("Initializing server");
            listener.Prefixes.Clear();
            listener.Prefixes.Add(string.Format("http://{0}:{1}/", this.Host, this.Port));
            Uri = new System.Uri(listener.Prefixes.First().Replace("+", "localhost"));

            Log("Obtaining service method information");
            methods = from mi in GetType().GetMethods(Flags).OfType<MethodInfo>()
                      let attribs = mi.GetCustomAttributes(false).OfType<VerbAttribute>()
                      where attribs.Count() > 0
                      select new ServiceBridge {
                          MethodInfo = mi,
                          Attribute = attribs.Single()
                      };
            
            if(AllowDescribe) {
                Uri temp;
                if(Uri.TryCreate(Uri, DescribePath, out temp))
                    DescriptionUri = temp;
            }
        }
        void NewRequest(IAsyncResult Result) {
            if(listener.IsListening) {
                HttpListenerContext context = listener.EndGetContext(Result);
                listener.BeginGetContext(NewRequest, null);
                ProcessRequest(context);
            }
        }
        void ProcessRequest(HttpListenerContext Context) {
            var Request = Context.Request;
            var Response = Context.Response;
            
            if(methods.Count() == 0) {
                Respond(Response, NoExposedMethods());
                Log("There are no methods exposed by this service!");
                return;
            }

            ServiceBridge m = methods.Where(jm => jm.IsMatch(Request.Url.LocalPath)).FirstOrDefault();

            if(Authorize && !AuthorizeRequest(Request)) {                
                Respond(Response, Unauthorized());
                Log("Unauthorized request");
                return;
            }

            if(AllowDescribe && DescriptionUri != null && Request.Url.LocalPath.Equals(DescriptionUri.LocalPath, StringComparison.InvariantCultureIgnoreCase)) {                
                Respond(Response, Describe());
                Log("Describing service");
                return;
            } 

            if(m == null) {                
                Respond(Response, NoMatchingMethod());
                Log("No suitable method found");
            } else {
                if(!Request.HttpMethod.Equals(m.Attribute.Verb, StringComparison.InvariantCultureIgnoreCase)) {                    
                    Respond(Response, InvalidVerb());
                    Log("Invalid HTTP verb");
                } else {
                    try {
                        dynamic postedDoc = null;

                        if(Request.HasEntityBody) {
                            try {
                                using(StreamReader sr = new StreamReader(Request.InputStream)) {
                                    postedDoc = JsonDocument.Parse(sr.ReadToEnd());
                                }
                            } catch {
                                Respond(Response, InvalidJsonPosted());
                                Log("Data posted to the server was not valid json");
                                return;
                            }
                        }

                        var args = m.MapParameters(Request.QueryString, postedDoc);
                        
                        var result = GetType().InvokeMember(
                            name: m.MethodInfo.Name,
                            invokeAttr: Flags,
                            binder: Type.DefaultBinder,
                            target: this,
                            args: args.Item1,
                            modifiers: null,
                            culture: null,
                            namedParameters: args.Item2
                        );

                        Respond(Response, result);
                        Log(string.Format("Invoked {0}.{1}({2})", m.MethodInfo.DeclaringType.Name, m.MethodInfo.Name, string.Join(", ", args.Item3)));
                    } catch(ArgumentException ae) {           
                        Respond(Response, ParameterFailure(ae));
                        Log("Parameter value missing or invalid");
                    } catch(Exception e) {                        
                        Respond(Response, CallFailure(e));
                        Log("Failure to execute method");
                    }
                }
            }
        }
        void Respond(HttpListenerResponse Response, object content) {
            JsonDocument doc;
            HttpStatusCode code = HttpStatusCode.OK;      

            var tuple = content as Tuple<object, HttpStatusCode>;
            if(tuple != null) {
                doc = new JsonDocument(tuple.Item1);
                code = tuple.Item2;
            } else {
                doc = new JsonDocument(content);
            }

            doc.Formatting = JsonDocument.JsonFormat.None;
            string raw = doc.ToString();

            Response.ContentType = "application/json";
            Response.ContentLength64 = raw.Length;
            Response.StatusCode = (int)code;
            using(StreamWriter sw = new StreamWriter(Response.OutputStream)) {
                sw.Write(raw);
            }
            Response.Close();
        }
        void Log(string msg) {
            if(LogOutput != null) {
                LogOutput.WriteLine(string.Format("[{0:MM/dd/yyyy HH:mm:ss}] {1}", DateTime.Now, msg));
                LogOutput.Flush();
            }
        }
        /// <summary>
        /// Returns all valid request urls for the service.
        /// </summary>
        /// <returns></returns>
        object Describe() {
            return (from m in methods
                    let e = m.GetExampleUri(Uri)
                    select new {
                        path = m.Attribute.Path,
                        desc = m.Attribute.Description,
                        parameters = from pn in m.Attribute.ParameterNames
                                     let p = m.GetParameterInfo(pn)
                                     let r = p.DefaultValue == System.DBNull.Value
                                     select new {
                                         name = pn,
                                         type = p.ParameterType.Name.ToLower(),
                                         required = r,
                                         @default = r ? null : p.DefaultValue
                                     },
                        verb = m.Attribute.Verb,
                        example = e == null ? string.Empty : e.AbsoluteUri
                    }).ToArray();
        }

        /// <summary>
        /// Returns a Tuple with the content to send to the client, along with a HttpStatusCode.
        /// </summary>
        /// <param name="obj">Object to return to the client, can be null</param>
        /// <param name="code">Status code for the client</param>
        /// <returns></returns>
        protected object WithStatusCode(object obj, HttpStatusCode code) {
            return Tuple.Create(obj, code);
        }
        /// <summary>
        /// Returns the json for when the service has no publically available methods
        /// </summary>
        /// <returns></returns>
        protected virtual object NoExposedMethods() {
            return new {
                status = "failed",
                error = "This service is not exposing any methods!"
            };
        }
        /// <summary>
        /// Performs authorization &amp; returns a boolean representing the result.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        protected virtual bool AuthorizeRequest(HttpListenerRequest Request) {
            return false;
        }
        /// <summary>
        /// Returns the json for when no method exists.
        /// </summary>
        /// <returns></returns>
        protected virtual object NoMatchingMethod() {
            return new {
                status = "failed",
                error = "Invalid method call"
            };
        }
        /// <summary>
        /// Returns the json for an error when a parameter value is invalid.
        /// </summary>
        /// <param name="exception">The argument exception thrown.  The Data property will contain 2 entries; value &amp; expected_type</param>
        protected virtual object ParameterFailure(ArgumentException exception) {
            return new {
                status = "failed",
                error = exception.Message,
                parameter = exception.ParamName,
                value = exception.Data["value"],
                expected_type = ((Type)exception.Data["expected_type"]).Name.ToLower()
            };
        }
        /// <summary>
        /// Returns the json for an error calling the requested method.
        /// </summary>
        /// <param name="Message">Exception message</param>
        /// <returns></returns>        
        protected virtual object CallFailure(Exception e) {
            return new {
                status = "failed",
                error = e.Message
            };
        }
        /// <summary>
        /// Returns the json for an invalid verb for the request.
        /// </summary>
        /// <returns></returns>
        protected virtual object InvalidVerb() {
            return new {
                status = "failed",
                error = "http verb specified is not allowed for this method."
            };
        }
        /// <summary>
        /// Returns the json for an unauthorized request.
        /// </summary>
        /// <returns></returns>
        protected virtual object Unauthorized() {
            return new {
                status = "failed",
                error = "Missing or invalid authorization key."
            };
        }
        /// <summary>
        /// Returns the json for an invalid posted json document.
        /// </summary>
        /// <returns></returns>
        protected virtual object InvalidJsonPosted() {
            return new {
                status = "failed",
                message = "json posted to the server was invalid."
            };
        }

        BindingFlags Flags {
            get {
                return BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance;
            }
        }
        /// <summary>
        /// Gets and sets the host the service will run on.  Defaults to localhost.
        /// </summary>
        public string Host {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets the port the service will run on.  Defaults to 5678.
        /// </summary>
        public int Port {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets the path for service description. Defaults to '/help'
        /// </summary>
        public string DescribePath {
            get;
            set;
        }
        /// <summary>
        /// Gets the URI the service is listening on
        /// </summary>
        public Uri Uri {
            get;
            private set;
        }
        /// <summary>
        /// Gets the uri where the service will describe itself.
        /// </summary>
        public Uri DescriptionUri {
            get;
            private set;
        }
        /// <summary>
        /// Gets and sets whether or not to authorize requests.
        /// </summary>
        /// <remarks>Override the AuthorizeRequest method for custom authentication.</remarks>
        public bool Authorize {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets whether or not to allow the service to describe its methods to a client by requesting the path specified in DescribePath.
        /// </summary>
        public bool AllowDescribe {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets the output for logging. Defaults to the console
        /// </summary>
        public TextWriter LogOutput {
            get;
            set;
        }
    }
}
