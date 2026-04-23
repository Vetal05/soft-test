using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Models;

namespace NewsAggregator.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Source>(e => e.HasIndex(s => s.Url).IsUnique());
        modelBuilder.Entity<Article>(e =>
        {
            e.HasIndex(a => a.Url).IsUnique();
            e.HasOne(a => a.Source)
                .WithMany(s => s.Articles)
                .HasForeignKey(a => a.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Bookmark>(e =>
        {
            e.HasIndex(b => new { b.UserId, b.ArticleId }).IsUnique();
            e.HasOne(b => b.Article)
                .WithMany(a => a.Bookmarks)
                .HasForeignKey(b => b.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
