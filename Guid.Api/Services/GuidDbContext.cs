using Guid.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Services
{
    /// <summary>
    /// Entity framework database context for this service.
    /// </summary>
    public class GuidDbContext : DbContext
    {
        public GuidDbContext(DbContextOptions<GuidDbContext> options)
            : base(options)
        {
        }

        public DbSet<GuidInfoEntity> GuidInfos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // make guid a unique index
            builder.Entity<GuidInfoEntity>()
                .HasIndex(i => i.Guid)
                .IsUnique();
        }
    }
}
