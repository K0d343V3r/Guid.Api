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
    /// Base class for GuidInfo entity.
    /// </summary>
    public class GuidInfoBase
    {
        /// <summary>
        /// Expiration time for Guid.  Default is 30 days from time of creation.
        /// </summary>
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? Expire { get; set; }

        /// <summary>
        /// User associated with Guid.
        /// </summary>
        public string User { get; set; }
    }
}
