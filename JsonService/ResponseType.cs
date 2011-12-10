using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService
{
    internal enum ResponseType
    {
        Describe,
        NoMethod,
        NoGenericSupport,
        WrongVerb,
        Invoke
    }
}