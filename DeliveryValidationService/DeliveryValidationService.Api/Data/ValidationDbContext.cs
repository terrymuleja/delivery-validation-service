using DeliveryValidationService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryValidationService.Api.Data
{
    public class ValidationDbContext : DbContext
    {
        public ValidationDbContext(DbContextOptions<ValidationDbContext> options) : base(options) { }

        public DbSet<ValidationResult> ValidationResults { get; set; }
        public DbSet<FeedbackData> FeedbackData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ValidationResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RequestId);
                entity.Property(e => e.Status).HasConversion<string>();
            });

            modelBuilder.Entity<FeedbackData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RequestId);
            });
        }
    }
}
