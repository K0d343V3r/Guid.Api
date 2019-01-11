using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Models
{
    /// <summary>
    /// Error codes returned from API calls.
    /// </summary>
    public enum GuidErrorCode
    {
        /// <summary>
        /// The requested guid was not found.
        /// </summary>
        GuidNotFound,

        /// <summary>
        /// The requested guid has expired.
        /// </summary>
        GuidExpired,

        /// <summary>
        /// The supplied user is invalid (empty).
        /// </summary>
        InvalidUser
    }
}
