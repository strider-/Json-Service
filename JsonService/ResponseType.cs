using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService {
    enum ResponseType {
        Describe,
        NoMethod,
        NoGenericSupport,
        WrongVerb,
        Invoke
    }
}