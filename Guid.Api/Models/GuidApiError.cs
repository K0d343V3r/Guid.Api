using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Models
{
    /// <summary>
    /// Contains error information for API clients.
    /// </summary>
    public class GuidApiError
    {
        /// <summary>
        /// Error code associated with this api error.
        /// </summary>
        public GuidErrorCode Code { get; set; }

        /// <summary>
        /// Any additional details about the error.
        /// </summary>
        public string Details { get; set; }

        public GuidApiError(GuidErrorCode code, string details = null)
        {
            Code = code;
            Details = details;
        }
    }
}
