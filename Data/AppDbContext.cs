using Microsoft.EntityFrameworkCore;
using TaskManager.Models;
namespace TaskManager.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<RagDailyUsage> RagDailyUsages => Set<RagDailyUsage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasKey(t => t.TopicId);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Topic)
            .WithOne()
            .HasForeignKey<TaskItem>(t => t.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Topic>()
            .HasOne<Topic>()
            .WithMany()
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Topic>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RagDailyUsage>()
            .HasKey(r => new { r.UserId, r.UsageDate });

        modelBuilder.Entity<RagDailyUsage>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}