using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Models
{
    /// <summary>
    /// GuidInfo data transfer object (DTO).
    /// </summary>
    public class GuidInfo : GuidInfoBase
    {
        /// <summary>
        /// The guid.
        /// </summary>
        public string Guid { get; set; }
    }
}
