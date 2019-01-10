using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Models
{
    public class GuidApiError
    {
        public GuidErrorCode Code { get; set; }
        public string Details { get; set; }

        public GuidApiError(GuidErrorCode code, string details = null)
        {
            Code = code;
            Details = details;
        }
    }
}
