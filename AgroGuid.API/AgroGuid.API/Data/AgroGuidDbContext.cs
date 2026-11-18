
using AgroGuid.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AgroGuid.API.Data;

public class AgroGuidDbContext : DbContext
{
    public AgroGuidDbContext(DbContextOptions<AgroGuidDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Crop> Crops { get; set; }
    public DbSet<Field> Fields { get; set; }
    public DbSet<FieldHistory> FieldHistory { get; set; }
    public DbSet<AuthSession> AuthSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Login)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Crop>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Field>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Field>()
            .HasOne<Crop>()
            .WithMany()
            .HasForeignKey(f => f.CropId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
