using Api.Common.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Models
{
    public class GuidRepository<T> : DbRepository<T> where T : EntityBase
    {
        public GuidRepository(GuidDbContext context)
            : base(context)
        {
        }
    }
}
