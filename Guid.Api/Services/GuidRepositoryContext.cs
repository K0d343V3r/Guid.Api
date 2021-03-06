﻿using Api.Common.Repository;
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
    public class GuidRepositoryContext : DbRepositoryContext, IGuidRepositoryContext
    {
        public IRepository<GuidInfoEntity> GuidInfos { get; private set; }

        public GuidRepositoryContext(GuidDbContext context)
            : base(context)
        {
            GuidInfos = new GuidRepository<GuidInfoEntity>((GuidDbContext)_context);
        }
    }
}
