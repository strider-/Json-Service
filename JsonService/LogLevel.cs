using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonWebService {
    /// <summary>
    /// Log message severity indicator
    /// </summary>
    public enum LogLevel {
        /// <summary>
        /// General information
        /// </summary>
        Info,
        /// <summary>
        /// Something may be wrong, but it's nothing that would stop the service.
        /// </summary>
        Warning,
        /// <summary>
        /// Gamebreaking stuff right here.
        /// </summary>
        Error
    }

}
