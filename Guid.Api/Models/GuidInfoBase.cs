using Api.Common;
using Api.Common.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Models
{
    /// <summary>
    /// Base class for GuidInfo data transfer object (DTO).
    /// </summary>
    public class GuidInfoBase
    {
        /// <summary>
        /// Expiration time for Guid (serialized as UNIX time).
        /// </summary>
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? Expire { get; set; }

        /// <summary>
        /// User associated with guid.
        /// </summary>
        public string User { get; set; }
    }
}
