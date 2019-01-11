using Api.Common.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Models
{
    /// <summary>
    /// Repository pattern context for guid service entities.
    /// </summary>
    public interface IGuidRepositoryContext : IRepositoryContext
    {
        IRepository<GuidInfoEntity> GuidInfos { get; }
    }
}
