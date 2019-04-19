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
    /// Database entity for a guid and its meta data.
    /// </summary>
    public class GuidInfoEntity : EntityBase, IUpdatable<GuidInfoBase>
    {
        /// <summary>
        /// The Guid.
        /// </summary>
        public System.Guid Guid { get; set; }

        /// <summary>
        /// Expiration time for Guid.  Default is 30 days from time of creation.
        /// </summary>
        public DateTime Expire { get; set; }

        /// <summary>
        /// User associated with Guid.
        /// </summary>
        public string User { get; set; }

        public void UpdateFrom(GuidInfoBase fromBase)
        {
            if (fromBase.Expire.HasValue)
            {
                Expire = fromBase.Expire.Value;
            }
            if (!string.IsNullOrWhiteSpace(fromBase.User))
            {
                User = fromBase.User;
            }
        }

        public GuidInfo ToGuidInfo()
        {
            return new GuidInfo()
            {
                // Requirement: return as a 32-character string, all uppercase
                Guid = Guid.ToString("N").ToUpper(),
                // make sure we mark all our dates as UTC for correct JSON serialization.
                Expire = new DateTime(Expire.Ticks, DateTimeKind.Utc),
                User = User
            };
        }
    }
}
