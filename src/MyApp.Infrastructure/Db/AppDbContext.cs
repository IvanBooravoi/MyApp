using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Db {
    public class AppDbContext : DbContext {
        public DbSet<User> Users => Set<User>();
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<User>(entity => {
                entity.ToTable("users");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.UserName).IsRequired().HasMaxLength(50);
                entity.Property(x => x.Email).IsRequired().HasMaxLength(100);
                entity.Property(x => x.PasswordHash).IsRequired();
                entity.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
                entity.HasIndex(x => x.Email).IsUnique();
                entity.HasIndex(x => x.UserName).IsUnique();

            });
        }
    }
}
