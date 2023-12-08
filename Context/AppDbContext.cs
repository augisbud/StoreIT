using Microsoft.EntityFrameworkCore;
using StoreIT.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FileEntry> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntry>()
            .HasKey(f => f.Id);

        modelBuilder.Entity<FileEntry>()
            .Property(f => f.Id)
            .ValueGeneratedOnAdd();
    }
}