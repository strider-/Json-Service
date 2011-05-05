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

        protected enum LogLevel {
            /// <summary>
            /// General information
            /// </summary>
            Info,
            /// <summary>
            /// Something may be wrong, but it's nothing serious
            /// </summary>
            Warning,
            /// <summary>
            /// Gamebreaking stuff right here.
            /// </summary>
            Error
        }

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
        /// <exception cref="JsonWebService.TemplateCollisionException">thrown if multiple methods have the same VerbAttribute with identical UriTemplates.</exception>
        /// <exception cref="JsonWebService.InvalidPlaceholderException">thrown if the UriTemplate of a VerbAttribute has a placeholder that does not match a variable in the method signature</exception>
        public void Start() {
            Start(false);
        }
        /// <summary>
        /// Starts the service, with the option to open the default browser.
        /// </summary>
        /// <exception cref="JsonWebService.TemplateCollisionException">thrown if multiple methods have the same VerbAttribute with identical UriTemplates.</exception>
        /// <exception cref="JsonWebService.InvalidPlaceholderException">thrown if the UriTemplate of a VerbAttribute has a placeholder that does not match a variable in the method signature</exception>
        public void Start(bool OpenBrowser) {
            if(!listener.IsListening) {
                InitService();

                CheckForTemplateCollisions();

                try {
                    listener.Start();
                } catch(HttpListenerException hle) {
                    Log(LogLevel.Error, "Failed to start service: {0}", hle.Message);
                    return;
                }

                listener.BeginGetContext(NewRequest, null);
                Log(LogLevel.Info, "Server started @ {0}", Uri.AbsoluteUri);

                if(AllowDescribe && DescriptionUri != null)
                    Log(LogLevel.Info, "Service description @ {0}", DescriptionUri.AbsoluteUri);
                else
                    Log(LogLevel.Info, "Service description is not available.");

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
                Log(LogLevel.Info, "Server stopped");
            }
        }

        void InitService() {
            if(!System.Net.HttpListener.IsSupported)
                throw new ApplicationException("HttpListener is not supported on this version of Windows.");
            if(string.IsNullOrWhiteSpace(Host))
                Host = "+";
            if(this.Port <= 0 || this.Port > 65535)
                throw new ArgumentException("Port must be greater than 0 and less than 65535");

            Log(LogLevel.Info, "Initializing server");
            listener.Prefixes.Clear();
            listener.Prefixes.Add(string.Format("http://{0}:{1}/", this.Host, this.Port));
            Uri = new System.Uri(listener.Prefixes.First().Replace("+", "localhost"));

            Log(LogLevel.Info, "Obtaining service method information");
            methods = from mi in GetType().GetMethods(Flags).OfType<MethodInfo>()
                      let attribs = mi.GetCustomAttributes(false).OfType<VerbAttribute>()
                      where attribs.Count() > 0
                      select new ServiceBridge {
                          MethodInfo = mi,
                          Attribute = attribs.Single()
                      };

            Log(LogLevel.Info, "Validating placeholder variables");
            var bads = methods.Where(m => m.InvalidPlaceholders().Count() > 0);
            if(bads.Count() > 0) {
                foreach(var b in bads) {
                    Log(LogLevel.Error, "Invalid placeholder(s) on the {0} method: {1}", b.MethodInfo.Name, string.Join(",", b.InvalidPlaceholders()));
                }
                throw new InvalidPlaceholderException(bads.First().MethodInfo.Name, bads.First().InvalidPlaceholders());
            }

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
                Log(LogLevel.Error, "There are no methods exposed by this service!");
                return;
            }

            // return the method with the greatest number of matched parameters
            ServiceBridge bridge = (from m in methods
                                    where m.IsMatch(Request.Url.LocalPath, Request.HttpMethod, Request.QueryString.AllKeys)
                                    orderby m.Attribute.ParameterNames.Length descending
                                    select m).FirstOrDefault();

            if(Authorize && !AuthorizeRequest(Request)) {
                Respond(Response, Unauthorized());
                Log(LogLevel.Error, "Unauthorized request");
                return;
            }

            if(AllowDescribe && DescriptionUri != null && Request.Url.LocalPath.Equals(DescriptionUri.LocalPath, StringComparison.InvariantCultureIgnoreCase)) {
                Respond(Response, Describe());
                Log(LogLevel.Info, "Describing service");
                return;
            }

            if(bridge == null) {
                Respond(Response, NoMatchingMethod());
                Log(LogLevel.Error, "No suitable method found");
            } else {
                if(!Request.HttpMethod.Equals(bridge.Attribute.Verb, StringComparison.InvariantCultureIgnoreCase)) {
                    Respond(Response, InvalidVerb());
                    Log(LogLevel.Error, "Invalid HTTP verb");
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
                                Log(LogLevel.Error, "Data posted to the server was not valid json");
                                return;
                            }
                        }

                        var args = bridge.MapParameters(Request.QueryString, postedDoc);

                        var result = GetType().InvokeMember(
                            name: bridge.MethodInfo.Name,
                            invokeAttr: Flags,
                            binder: Type.DefaultBinder,
                            target: this,
                            args: args.Item1,
                            modifiers: null,
                            culture: null,
                            namedParameters: args.Item2
                        );

                        Respond(Response, result);
                        Log(LogLevel.Info, "Invoked {0}.{1}({2})", bridge.MethodInfo.DeclaringType.Name, bridge.MethodInfo.Name, string.Join(", ", args.Item3));
                    } catch(ArgumentException ae) {
                        Respond(Response, ParameterFailure(ae));
                        Log(LogLevel.Error, "Parameter value missing or invalid");
                    } catch(Exception e) {
                        // the base exception will always be a invocation exception, the inner exception is the heart of the problem.
                        Respond(Response, CallFailure(e.InnerException));
                        Log(LogLevel.Error, "Failure to execute method");
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
            byte[] raw = Encoding.UTF8.GetBytes(doc.ToString());

            Response.StatusCode = (int)code;
            Response.StatusDescription = code.ToString();
            Response.ContentType = "application/json";
            Response.ContentLength64 = raw.Length;
            using(Stream s = Response.OutputStream) {
                int offset = 0, size;
                while(offset != raw.Length) {
                    size = Math.Min(raw.Length - offset, 0x8000);
                    s.Write(raw, offset, size);
                    offset += size;
                }
            }

            Response.Close();
        }
        /// <summary>
        /// Returns all valid request urls for the service.
        /// </summary>
        /// <returns></returns>
        object Describe() {
            return from m in methods
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
                   };
        }
        void CheckForTemplateCollisions() {
            var q = from m in methods
                    group m by m.GetHashCode() into dupes
                    where dupes.Count() > 1
                    let d = dupes.First().Attribute
                    select new {
                        Path = d.Path,
                        ParameterNames = d.ParameterNames,
                        Verb = d.Verb,
                        Methods = dupes.Select(m => m.MethodInfo.Name).ToArray()
                    };


            if(q.Count() > 0) {
                foreach(var c in q) {
                    Log(LogLevel.Error, "Template collision detected: Path={0}; Parameters={1}; Verb={2}; Methods={3}",
                        c.Path, string.Join(",", c.ParameterNames), c.Verb, string.Join(",", c.Methods));
                }

                var e = q.First();
                throw new TemplateCollisionException(e.Path, e.ParameterNames, e.Verb, e.Methods);
            } else {
                Log(LogLevel.Info, "No template collisions detected.");
            }
        }

        /// <summary>
        /// Writes an event to the log specified in the LogOutput property.
        /// </summary>
        /// <param name="level">The severity of the message.</param>
        /// <param name="msg">The message to log, can be a formatted string.</param>
        /// <param name="args">The arguments to the formatted message string, if any.</param>
        protected void Log(LogLevel level, string msg, params object[] args) {
            if(LogOutput != null) {
                try {
                    LogOutput.Write("[{0:MM/dd/yyyy HH:mm:ss}]\t{1}\t", DateTime.Now, level);
                    LogOutput.WriteLine(msg, args);
                    LogOutput.Flush();
                } catch {
                    // What do you do when you log errors, but the error log has errors?
                }
            }
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
        /// <param name="Request">The incoming request that needs to be validated.</param>
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
        /// <param name="e">Exception encountered when invoking the service method.</param>
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
                return BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;
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
        /// <summary>
        /// Gets whether or not the service is currently accepting requests.
        /// </summary>
        public bool IsRunning {
            get {
                return listener.IsListening;
            }
        }
    }
}
