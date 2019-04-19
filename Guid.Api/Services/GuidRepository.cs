using Api.Common.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Services
{
    /// <summary>
    /// Repository for guid service entities.
    /// </summary>
    /// <typeparam name="T">The wrapper database entity.</typeparam>
    public class GuidRepository<T> : DbRepository<T> where T : EntityBase
    {
        public GuidRepository(GuidDbContext context)
            : base(context)
        {
        }
    }
}
