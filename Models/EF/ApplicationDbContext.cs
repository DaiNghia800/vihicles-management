using Microsoft.EntityFrameworkCore;
using Public_Transport.Models.Entities;
using System.Data;
using System.Security;

namespace Public_Transport.Models.EF
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }


        // --- CÁC BẢNG USER/PHÂN QUYỀN ---
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Function> Functions { get; set; }
        public DbSet<PermissionType> PermissionTypes { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Function>(entity =>
            {
                entity.HasKey(f => f.Uid);
                entity.Property(f => f.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(f => f.Code)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(f => f.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Active");
                entity.Property(f => f.Deleted)
                    .HasDefaultValue(false);
                entity.HasMany(f => f.Permissions)
                    .WithOne(p => p.Function)
                    .HasForeignKey(p => p.FunctionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Uid);

                entity.HasOne(p => p.Role)
                    .WithMany(r => r.Permissions)
                    .HasForeignKey(p => p.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Function)
                    .WithMany(f => f.Permissions)
                    .HasForeignKey(p => p.FunctionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.PermissionType)
                    .WithMany(pt => pt.Permissions)
                    .HasForeignKey(p => p.PermissionTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(p => p.Allowed)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<Roles>(entity =>
            {
                entity.HasKey(e => e.Uid);
                entity.Property(e => e.RoleName)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.CreatedAt)
                   .HasColumnType("datetime")
                   .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.CreatedBy)
                   .HasMaxLength(100)
                   .IsUnicode(true)
                   .IsRequired(false);
                entity.Property(e => e.UpdatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(true)
                    .IsRequired(false);
                entity.Property(e => e.Deleted)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<PermissionType>(entity =>
            {
                entity.HasKey(pt => pt.Id);
                entity.Property(pt => pt.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(pt => pt.Code)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.Uid);
                entity.Property(e => e.FullName)
                    .HasMaxLength(200)
                    .IsUnicode(true)
                    .IsRequired();
                entity.Property(e => e.ImgUser)
                     .HasColumnType("nvarchar(max)")
                    .IsRequired(false);

                entity.Property(e => e.Email)
                    .HasConversion(v => v.ToLower(), v => v)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .IsRequired();
                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsRequired(false);
                entity.Property(e => e.Address)
                    .HasColumnType("nvarchar(255)")
                    .IsRequired(false);
                entity.Property(e => e.PasswordHash)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();
                entity.Property(e => e.OtpCode)
                    .HasMaxLength(6)
                    .IsUnicode(false)
                    .IsRequired(false);
                entity.Property(e => e.OtpExpiry)
                    .HasColumnType("datetime")
                    .IsRequired(false);

                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleUid)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.CreatedBy)
                   .HasMaxLength(100)
                   .IsUnicode(true)
                   .IsRequired(false);
                entity.Property(e => e.UpdatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(true)
                    .IsRequired(false);
                entity.Property(e => e.Deleted)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.ToTable("Vehicles");

                entity.HasKey(v => v.VehicleId);

                entity.Property(v => v.LicensePlate)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(v => v.Thumbnail)
                      .HasColumnType("nvarchar(max)")
                      .IsUnicode(false)
                      .IsRequired(false);

                entity.Property(v => v.VehicleType)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(v => v.SeatCapacity)
                      .IsRequired();

                entity.Property(v => v.Status)
                      .HasMaxLength(30)
                      .HasDefaultValue("Active");

                entity.Property(v => v.CreatedAt)
                      .HasDefaultValueSql("GETDATE()");

                entity.Property(v => v.UpdatedAt)
                      .HasDefaultValueSql("GETDATE()");

                entity.Property(v => v.CreatedBy)
                      .HasMaxLength(100);

                entity.Property(v => v.UpdatedBy)
                      .HasMaxLength(100);

                entity.Property(v => v.Deleted)
                      .HasDefaultValue(false);
            });
        }
    }
}
