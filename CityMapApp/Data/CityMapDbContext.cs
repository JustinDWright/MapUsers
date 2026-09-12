using CityMapApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CityMapApp.Data;

public sealed class CityMapDbContext(DbContextOptions<CityMapDbContext> options)
    : DbContext(options)
{
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Submission>(entity =>
        {
            entity.Property(submission => submission.City).HasMaxLength(100).IsRequired();
            entity.Property(submission => submission.State).HasMaxLength(50).IsRequired();
            entity.Property(submission => submission.Name).HasMaxLength(100);
            entity.Property(submission => submission.EmailAddress).HasMaxLength(254);
            entity.Property(submission => submission.UserToken).HasMaxLength(64).IsRequired();
            entity.HasIndex(submission => submission.UserToken).IsUnique();
        });
    }
}
