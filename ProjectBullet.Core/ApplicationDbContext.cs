using Microsoft.EntityFrameworkCore;
using ProjectBullet.Core.Entities;

namespace ProjectBullet.Core;

/// <summary>
/// The <see cref="DbContext"/> for the ProjectBullet core domain.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
        // Enable WAL mode for better concurrent read/write performance
        Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }

    public DbSet<ProxyEntity> Proxies { get; set; }
    public DbSet<ProxyGroupEntity> ProxyGroups { get; set; }
    public DbSet<WordlistEntity> Wordlists { get; set; }
    public DbSet<JobEntity> Jobs { get; set; }
    public DbSet<RecordEntity> Records { get; set; }
    public DbSet<HitEntity> Hits { get; set; }
    public DbSet<GuestEntity> Guests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProxyGroupEntity>()
            .HasMany(g => g.Proxies)
            .WithOne(u => u.Group)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ProxyGroupEntity>()
            .HasOne(g => g.Owner)
            .WithMany(u => u.ProxyGroups)
            .OnDelete(DeleteBehavior.SetNull);
        
        base.OnModelCreating(modelBuilder);
    }
}
