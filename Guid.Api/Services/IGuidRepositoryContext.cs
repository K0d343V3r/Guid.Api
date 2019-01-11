using Api.Common.Repository;
using Guid.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Services
{
    /// <summary>
    /// Repository pattern context for guid service entities.
    /// </summary>
    public interface IGuidRepositoryContext : IRepositoryContext
    {
        IRepository<GuidInfoEntity> GuidInfos { get; }
    }
}
