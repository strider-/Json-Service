﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Reflection;
using System.IO;

namespace JsonWebService
{
    /// <summary>
    /// Abstract class for json based web services.
    /// </summary>
    public abstract class JsonServiceBase
    {
        static object logLock = new object();
        IEnumerable<ServiceBridge> bridges;
        HttpListener listener;
        Dictionary<string, string> customHeaders;
        BindingFlags _bindingFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;

        public JsonServiceBase()
        {
            this.Host = "+";
            this.Port = 5678;
            this.Authorize = false;
            this.AllowDescribe = true;
            this.DescribePath = "/help";
            this.LogOutput = Console.Out;
            this.listener = new HttpListener();
            this.OpenBrowserOnStart = false;
            this.customHeaders = new Dictionary<string, string>();
        }

        /// <summary>
        /// Starts the service
        /// </summary>
        public void Start()
        {
            if(!IsRunning)
            {
                try
                {
                    InitService();
                    CheckForTemplateCollisions();
                    listener.Start();
                }
                catch(Exception e)
                {
                    Log(LogLevel.Error, "Failed to start service: {0}", e.Message);
                    return;
                }

                listener.BeginGetContext(NewRequest, null);
                Log(LogLevel.Info, "Server started @ {0}", Uri.AbsoluteUri);

                if(AllowDescribe && DescriptionUri != null)
                    Log(LogLevel.Info, "Service description @ {0}", DescriptionUri.AbsoluteUri);
                else
                    Log(LogLevel.Warning, "Service description is not available.");

                if(OpenBrowserOnStart)
                    System.Diagnostics.Process.Start(Uri.AbsoluteUri);
            }
        }
        /// <summary>
        /// Stops the service
        /// </summary>
        public void Stop()
        {
            if(IsRunning)
            {
                listener.Stop();
                Uri = null;
                DescriptionUri = null;
                Log(LogLevel.Info, "Server stopped");
            }
        }
        /// <summary>
        /// Starts the service &amp; keeps it running as long as the conditon returns true.
        /// </summary>
        /// <param name="condition"></param>
        public void RunWhile(Func<bool> condition)
        {
            Start();
            while(condition() && IsRunning)
                ;
            Stop();
        }
        void InitService()
        {
            Log(LogLevel.Info, "Initializing server");

            if(!HttpListener.IsSupported)
                throw new ApplicationException("HttpListener is not supported on this version of Windows.");
            if(string.IsNullOrWhiteSpace(Host))
                Host = "+";
            if(this.Port <= 0 || this.Port > 65535)
                throw new ArgumentException("Port must be greater than 0 and less than 65535");

            listener.Prefixes.Clear();
            listener.Prefixes.Add(string.Format("http://{0}:{1}/", this.Host, this.Port));
            Uri = new System.Uri(listener.Prefixes.First().Replace("+", "localhost"));

            Log(LogLevel.Info, "Obtaining service method information");
            
            bridges = from mi in GetType().GetMethods(_bindingFlags).OfType<MethodInfo>()
                      let attribs = mi.GetCustomAttributes(false).OfType<VerbAttribute>()
                      where attribs.Count() > 0
                      select new ServiceBridge
                      {
                          MethodInfo = mi,
                          AttributeCount = attribs.Count(),
                          Attribute = attribs.First()
                      };

            foreach(var method in bridges.Where(m => m.AttributeCount > 1))
            {
                Log(LogLevel.Warning, "{0} has multiple VerbAttributes, defaulting to '{1}' method.", method.QualifiedName, method.Attribute.Verb);
            }

            foreach(var method in bridges.Where(m => m.MethodInfo.ContainsGenericParameters))
            {
                Log(LogLevel.Warning, "{0} contains generic parameters which are not currently supported.", method.QualifiedName);
            }

            Log(LogLevel.Info, "Validating placeholder variables");
            var bads = bridges.Where(m => m.InvalidPlaceholders().Count() > 0);
            if(bads.Count() > 0)
            {
                foreach(var b in bads)
                {
                    Log(LogLevel.Error, "Invalid placeholder(s) on the {0} method: {1}", b.MethodInfo.Name, string.Join(",", b.InvalidPlaceholders()));
                }
                throw new InvalidPlaceholderException(bads.First().MethodInfo.Name, bads.First().InvalidPlaceholders());
            }

            if(AllowDescribe)
            {
                Uri temp;
                if(Uri.TryCreate(Uri, DescribePath, out temp))
                    DescriptionUri = temp;
            }
        }
        void NewRequest(IAsyncResult result)
        {
            if(IsRunning)
            {
                HttpListenerContext context = listener.EndGetContext(result);                
                listener.BeginGetContext(NewRequest, null);
				// get the method with the greatest number of matched parameters
				var bridge = (from m in bridges
							  where m.IsMatch(context.Request.Url.LocalPath, context.Request.HttpMethod, context.Request.QueryString.AllKeys)
							  orderby m.Attribute.ParameterNames.Length descending
							  select m).FirstOrDefault();

				ProcessRequest(new ServiceContext(context, bridge, this, customHeaders));
            }
        }
        void ProcessRequest(ServiceContext context)
        {
			if(bridges.Count() == 0)
			{
				context.Respond(NoExposedMethods());
				Log(LogLevel.Error, "There are no methods exposed by this service!");
				return;
			}

            if(context.RequireAuthorization && !AuthorizeRequest(context.Request))
            {
                context.Respond(Unauthorized());
                Log(LogLevel.Warning, "Unauthorized request");
            }
            else
            {
                switch(context.ResponseType)
                {
                    case ResponseType.Describe:
                        context.Respond(Describe());
                        Log(LogLevel.Info, "Describing service, {0}ms", context.ProcessingTime);
                        break;

                    case ResponseType.NoGenericSupport:
                        Exception exception = new Exception("Methods with generic parameters are not supported.");
                        context.Respond(CallFailure(exception));
                        Log(LogLevel.Warning, exception.Message);
                        break;

                    case ResponseType.NoMethod:
                        context.Respond(NoMatchingMethod());
                        Log(LogLevel.Warning, "No suitable method found for {0}", context.Request.Url.PathAndQuery);
                        break;

                    case ResponseType.WrongVerb:
                        context.Respond(InvalidVerb());
                        Log(LogLevel.Warning, "Invalid HTTP verb");
                        break;

                    case ResponseType.Invoke:
                        InvokeRequestedMethod(context);
                        break;
                }
            }
        }
        void InvokeRequestedMethod(ServiceContext context)
        {
            try
            {
                var map = context.GetMappedParameters();
                var result = GetType().InvokeMember(
                    name: context.Bridge.MethodInfo.Name,
                    invokeAttr: _bindingFlags,
                    binder: Type.DefaultBinder,
                    target: this,
                    args: map.Values.ToArray(),
                    modifiers: null,
                    culture: null,
                    namedParameters: map.Keys.ToArray()
                );

                context.Respond(result);
                string paramString = string.Join(", ", map.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)).ToArray());
                Log(LogLevel.Info, "Invoked {0}({1}), {2}ms", context.Bridge.QualifiedName, paramString, context.ProcessingTime);
            }
            catch(ArgumentException ae)
            {
                context.Respond(ParameterFailure(ae));
                Log(LogLevel.Warning, "Parameter value missing or invalid");
            }
            catch(JsonParserException)
            {
                context.Respond(InvalidJsonPosted());
                Log(LogLevel.Warning, "Unable to parse the posted json document.");
            }
            catch(Exception e)
            {
                context.Respond(CallFailure(e.InnerException ?? e));
                Log(LogLevel.Warning, "Failure to execute method");
            }
        }
        object Describe()
        {
            return from m in bridges
                   where m.Attribute.Describe
                   let e = m.GetExampleUri(Uri)                   
                   select new
                   {
                       path = m.Attribute.Path,
                       desc = m.Attribute.Description,
                       parameters = from pn in m.GetQueryStringParameterNames()
                                    let p = m.GetParameterInfo(pn)
                                    let r = p.DefaultValue == System.DBNull.Value
                                    select new
                                    {
                                        name = pn,
                                        type = p.ParameterType.Name.ToLower(),
                                        required = r,
                                        @default = r ? null : p.DefaultValue
                                    },
                       verb = m.Attribute.Verb,
                       example = e == null ? string.Empty : e.AbsoluteUri
                   };
        }
        void CheckForTemplateCollisions()
        {
            var q = from m in bridges
                    group m by m.GetHashCode() into dupes
                    where dupes.Count() > 1
                    let d = dupes.First().Attribute
                    select new
                    {
                        Path = d.Path,
                        ParameterNames = d.ParameterNames,
                        Verb = d.Verb,
                        Methods = dupes.Select(m => m.MethodInfo.Name).ToArray()
                    };


            if(q.Count() > 0)
            {
                foreach(var c in q)
                {
                    Log(LogLevel.Error, "Template collision detected: Path={0}; Parameters={1}; Verb={2}; Methods={3}",
                        c.Path, string.Join(",", c.ParameterNames), c.Verb, string.Join(",", c.Methods));
                }

                var e = q.First();
                throw new TemplateCollisionException(e.Path, e.ParameterNames, e.Verb, e.Methods);
            }
            else
            {
                Log(LogLevel.Info, "No template collisions detected.");
            }
        }
        /// <summary>
        /// Writes an event to the log specified in the LogOutput property.  Safe to call if LogOutput is null.
        /// </summary>
        /// <param name="level">The severity of the message.</param>
        /// <param name="msg">The message to log, can be a formatted string.</param>
        /// <param name="args">The arguments to the formatted message string, if any.</param>
        protected void Log(LogLevel level, string msg, params object[] args)
        {
            lock(logLock)
            {
                if(LogOutput != null)
                {
                    StackFrame frame = new StackTrace().GetFrame(1);
                    MethodBase mb = frame.GetMethod();
                    // if the previous stack frame is from an invoked method or a non-overridden method, it's a system generated log event,
                    // otherwise it's a user defined log event.
                    string source = mb.Name.Equals("CallSite.Target") || mb.DeclaringType == typeof(JsonServiceBase)
                        ? "System"
                        : "User";

                    try
                    {
                        LogOutput.Write("{0:MM/dd/yyyy HH:mm:ss}\t{1}\t{2}\t", DateTime.Now, source, level);
                        LogOutput.WriteLine(msg, args);
                    }
                    catch(FormatException)
                    {
                        LogOutput.WriteLine("String formatting error for log entry '{0}'", msg);
                    }
                    catch(Exception)
                    {
                        // So how do you log errors when the error log is the problem?
                    }
                    finally
                    {
                        LogOutput.Flush();
                    }
                }
            }
        }
        /// <summary>
        /// Returns the content to the client with a specified HttpStatusCode.
        /// </summary>
        /// <param name="content">Object to return to the client, can be null</param>
        /// <param name="code">Status code for the client</param>
        /// <returns></returns>
        protected object WithStatusCode(object content, HttpStatusCode code)
        {
            JsonServiceResult result = new JsonServiceResult(content);
            result.Code = code;
            return result;
        }
        /// <summary>
        /// Returns a resource to the client with the specified content type.
        /// </summary>
        /// <param name="resource">Resource to return, as a stream</param>
        /// <param name="contentType">content type of the resource</param>
        /// <returns></returns>
        protected object Resource(Stream resource, string contentType)
        {
            return Resource(resource, contentType, HttpStatusCode.OK);
        }
        /// <summary>
        /// Returns a resource to the client with the specified content type and HttpStatusCode.
        /// </summary>
        /// <param name="resource">Resource to return, as a stream</param>
        /// <param name="contentType">content type of the resource</param>
        /// <param name="code">Status code for the client</param>
        /// <returns></returns>
        protected object Resource(Stream resource, string contentType, HttpStatusCode code)
        {
            JsonServiceResult result = new JsonServiceResult(resource, contentType);
            result.Code = code;
            return result;
        }
        /// <summary>
        /// Returns the json for when the service has no publically available methods
        /// </summary>
        /// <returns></returns>
        protected virtual object NoExposedMethods()
        {
            return new
            {
                status = "failed",
                message = "This service is not exposing any methods!"
            };
        }
        /// <summary>
        /// Performs authorization &amp; returns a boolean representing the result.
        /// </summary>
        /// <param name="Request">The incoming request that needs to be validated.</param>
        /// <returns></returns>
        protected virtual bool AuthorizeRequest(HttpListenerRequest Request)
        {
            return false;
        }
        /// <summary>
        /// Returns the json for when no method exists.
        /// </summary>
        /// <returns></returns>
        protected virtual object NoMatchingMethod()
        {
            return new
            {
                status = "failed",
                message = "Invalid method call. Are you missing required parameters?"
            };
        }
        /// <summary>
        /// Returns the json for an error when a parameter value is invalid.
        /// </summary>
        /// <param name="exception">The argument exception thrown.  The Data property will contain 2 entries; value &amp; expected_type</param>
        protected virtual object ParameterFailure(ArgumentException exception)
        {
            return new
            {
                status = "failed",
                message = exception.Message,
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
        protected virtual object CallFailure(Exception e)
        {
            return new
            {
                status = "failed",
                message = e.Message
            };
        }
        /// <summary>
        /// Returns the json for an invalid verb for the request.
        /// </summary>
        /// <returns></returns>
        protected virtual object InvalidVerb()
        {
            return new
            {
                status = "failed",
                message = "http verb specified is not allowed for this method."
            };
        }
        /// <summary>
        /// Returns the json for an unauthorized request.
        /// </summary>
        /// <returns></returns>
        protected virtual object Unauthorized()
        {
            return new
            {
                status = "failed",
                message = "Missing or invalid authorization key."
            };
        }
        /// <summary>
        /// Returns the json for an invalid posted json document.
        /// </summary>
        /// <returns></returns>
        protected virtual object InvalidJsonPosted()
        {
            return new
            {
                status = "failed",
                message = "json posted to the server was invalid."
            };
        }
        /// <summary>
        /// Appends the specified header &amp; value to service responses.
        /// </summary>
        /// <param name="key">The name of the HTTP header</param>
        /// <param name="value">The value of the HTTP header</param>
        /// <remarks>Calls to this method while the service is running are ignored.</remarks>
        protected void AddResponseHeader(string key, string value)
        {
            if(!IsRunning)
            {
                if(key == null)
                    throw new ArgumentNullException("key");
                if(value == null)
                    throw new ArgumentNullException("value");

                customHeaders[key] = value;
            }
        }

        /// <summary>
        /// Gets and sets the host the service will run on.  Defaults to localhost.
        /// </summary>
        public string Host
        {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets the port the service will run on.  Defaults to 5678.
        /// </summary>
        public int Port
        {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets the path for service description. Defaults to '/help'
        /// </summary>
        public string DescribePath
        {
            get;
            set;
        }
        /// <summary>
        /// Gets the URI the service is listening on
        /// </summary>
        public Uri Uri
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets the uri where the service will describe itself.
        /// </summary>
        public Uri DescriptionUri
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets and sets whether or not to authorize requests.
        /// </summary>
        /// <remarks>Override the AuthorizeRequest method for custom authentication.</remarks>
        public bool Authorize
        {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets whether or not to allow the service to describe its methods to a client by requesting the path specified in DescribePath.
        /// </summary>
        public bool AllowDescribe
        {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets the output for logging. Defaults to the console
        /// </summary>
        public TextWriter LogOutput
        {
            get;
            set;
        }
        /// <summary>
        /// Gets whether or not the service is currently accepting requests.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return listener.IsListening;
            }
        }
        /// <summary>
        /// Gets and sets whether or not to open the default browser to the service address when the service starts.
        /// </summary>
        public bool OpenBrowserOnStart { get; set; }
    }
}