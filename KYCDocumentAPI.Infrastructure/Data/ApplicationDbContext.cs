using KYCDocumentAPI.Core.Entities;
using KYCDocumentAPI.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace KYCDocumentAPI.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentData> DocumentData { get; set; }
        public DbSet<KYCVerification> KYCVerifications { get; set; }
        public DbSet<VerificationResult> VerificationResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.State).HasConversion<string>();
            });

            // Configure Document entity
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DocumentType).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();

                // Relationships
                entity.HasOne(d => d.User)
                      .WithMany(u => u.Documents)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure DocumentData entity
            modelBuilder.Entity<DocumentData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DocumentId).IsUnique();

                // One-to-one relationship with Document
                entity.HasOne(dd => dd.Document)
                      .WithOne(d => d.DocumentData)
                      .HasForeignKey<DocumentData>(dd => dd.DocumentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure KYCVerification entity
            modelBuilder.Entity<KYCVerification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(k => k.User)
                      .WithMany(u => u.KYCVerifications)
                      .HasForeignKey(k => k.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure VerificationResult entity
            modelBuilder.Entity<VerificationResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(vr => vr.Document)
                      .WithMany(d => d.VerificationResults)
                      .HasForeignKey(vr => vr.DocumentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(vr => vr.KYCVerification)
                      .WithMany(k => k.VerificationResults)
                      .HasForeignKey(vr => vr.KYCVerificationId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed data for testing
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Seed a test user
            var testUserId = Guid.NewGuid();
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = testUserId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "9876543210",
                DateOfBirth = new DateTime(1990, 1, 15),
                City = "Mumbai",
                State = State.Maharashtra,
                PinCode = "400001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (BaseEntity)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }

                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
