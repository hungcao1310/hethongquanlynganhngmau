using Microsoft.EntityFrameworkCore;
using BloodBankManager.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BloodBankManager.Data
{
    public class BloodBankContext : IdentityDbContext<ApplicationUser>
    {
        public BloodBankContext(DbContextOptions<BloodBankContext> options)
            : base(options)
        {
        }

        public DbSet<BloodType> BloodTypes { get; set; }
        public DbSet<Blood> Bloods { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<BloodTransfusion> BloodTransfusions { get; set; }
        public DbSet<BloodInventory> BloodInventories { get; set; }
        public DbSet<BloodExpiryAlert> BloodExpiryAlerts { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure BloodType
            modelBuilder.Entity<BloodType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TypeName).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.TypeName).IsUnique();
            });

            // Configure Blood
            modelBuilder.Entity<Blood>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DonorName).HasMaxLength(200);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.DiscardReason).HasMaxLength(500);
                entity.HasIndex(e => e.UnitNumber).IsUnique();
                entity.HasIndex(e => e.ExpiryDate);
                entity.HasIndex(e => e.Status);

                entity.HasOne(e => e.BloodType)
                    .WithMany(bt => bt.Bloods)
                    .HasForeignKey(e => e.BloodTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Patient
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PatientCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Gender).HasMaxLength(20);
                entity.Property(e => e.Hospital).HasMaxLength(200);
                entity.Property(e => e.Ward).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.MedicalCondition).HasMaxLength(500);
                entity.HasIndex(e => e.PatientCode).IsUnique();

                entity.HasOne(e => e.BloodType)
                    .WithMany(bt => bt.Patients)
                    .HasForeignKey(e => e.BloodTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure BloodTransfusion
            modelBuilder.Entity<BloodTransfusion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PerformedBy).HasMaxLength(200);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.HasIndex(e => e.TransfusionDate);

                entity.HasOne(e => e.Patient)
                    .WithMany(p => p.Transfusions)
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Blood)
                    .WithMany()
                    .HasForeignKey(e => e.BloodId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure BloodInventory
            modelBuilder.Entity<BloodInventory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BloodTypeId).IsUnique();

                entity.HasOne(e => e.BloodType)
                    .WithMany()
                    .HasForeignKey(e => e.BloodTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure BloodExpiryAlert
            modelBuilder.Entity<BloodExpiryAlert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AcknowledgedBy).HasMaxLength(200);
                entity.HasIndex(e => e.AlertDate);
                entity.HasIndex(e => e.Status);

                entity.HasOne(e => e.Blood)
                    .WithMany()
                    .HasForeignKey(e => e.BloodId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial blood types
            SeedBloodTypes(modelBuilder);
        }

        private void SeedBloodTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BloodType>().HasData(
                new BloodType { Id = 1, TypeName = BloodTypeEnum.O_NEGATIVE, Description = "O âm tính - Nhóm máu phổ biến" },
                new BloodType { Id = 2, TypeName = BloodTypeEnum.O_POSITIVE, Description = "O dương tính" },
                new BloodType { Id = 3, TypeName = BloodTypeEnum.A_NEGATIVE, Description = "A âm tính" },
                new BloodType { Id = 4, TypeName = BloodTypeEnum.A_POSITIVE, Description = "A dương tính - Nhóm máu phổ biến" },
                new BloodType { Id = 5, TypeName = BloodTypeEnum.B_NEGATIVE, Description = "B âm tính" },
                new BloodType { Id = 6, TypeName = BloodTypeEnum.B_POSITIVE, Description = "B dương tính" },
                new BloodType { Id = 7, TypeName = BloodTypeEnum.AB_NEGATIVE, Description = "AB âm tính - Nhóm máu hiếm" },
                new BloodType { Id = 8, TypeName = BloodTypeEnum.AB_POSITIVE, Description = "AB dương tính" }
            );
        }
    }
}
