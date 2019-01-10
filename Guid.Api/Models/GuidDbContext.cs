using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guid.Api.Models
{
    public class GuidDbContext : DbContext
    {
        public GuidDbContext(DbContextOptions<GuidDbContext> options)
            : base(options)
        {
        }

        public DbSet<GuidInfoEntity> GuidInfos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<GuidInfoEntity>()
                .HasIndex(i => i.Guid)
                .IsUnique();
        }
    }
}
