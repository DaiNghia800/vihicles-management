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
        public DbSet<Station> Stations { get; set; }
        public DbSet<Public_Transport.Models.Entities.Route> Routes { get; set; }
        public DbSet<RouteDetail> RouteDetails { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<BlogPosts> BlogPosts { get; set; }
        public DbSet<BlogCategories> BlogCategories { get; set; }
        
        // --- THÊM MỚI: Tickets và Payments ---
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== FUNCTION =====
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

            // ===== PERMISSION =====
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

            // ===== ROLES =====
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

            // ===== PERMISSION TYPE =====
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

            // ===== USERS =====
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
                entity.Property(e => e.DateOfBirth)
                    .HasColumnType("date")
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

            modelBuilder.Entity<BlogCategories>(entity =>
            {
                entity.HasKey(e => e.Uid);
                entity.Property(e => e.Name)
                    .HasMaxLength(200)
                    .IsRequired();
            });

            modelBuilder.Entity<BlogPosts>(entity =>
            {
                entity.HasKey(e => e.Uid);

                entity.Property(e => e.Title)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.Content)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();

                entity.HasOne(e => e.Users)
                    .WithMany()
                    .HasForeignKey(e => e.AuthorUid)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryUid)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // --- THÊM MỚI: Cấu hình Ticket và Payment ---
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.TicketId);

                entity.HasOne(e => e.Trip)
                    .WithMany()
                    .HasForeignKey(e => e.TripId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.BookingDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Status)
                    .HasDefaultValue("Booked");
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);

                entity.HasOne(e => e.Ticket)
                    .WithOne(t => t.Payment)
                    .HasForeignKey<Payment>(e => e.TicketId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.PaymentDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Status)
                    .HasDefaultValue("Pending");
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

            // ===== DRIVER =====
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasKey(d => d.DriverId);

                entity.Property(d => d.LicenseNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(d => d.LicenseNumber)
                    .IsUnique();

                entity.Property(d => d.LicenseType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(d => d.LicenseExpiry)
                    .HasColumnType("datetime");

                entity.Property(d => d.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.Property(d => d.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(d => d.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                // Relationship with Users
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship with Vehicle
                entity.HasOne(d => d.VehicleAssigned)
                    .WithMany()
                    .HasForeignKey(d => d.VehicleAssignedId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== STATION =====
            modelBuilder.Entity<Station>(entity =>
            {
                entity.HasKey(s => s.StationId);
                
                entity.Property(s => s.StationName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.Address)
                    .HasColumnType("nvarchar(255)");
            });

            // ===== ROUTE =====
            modelBuilder.Entity<Public_Transport.Models.Entities.Route>(entity =>
            {
                entity.HasKey(r => r.RouteId);

                entity.Property(r => r.RouteName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(r => r.BasePrice)
                    .HasColumnType("decimal(18, 2)");
            });

            // ===== ROUTE DETAIL =====
            modelBuilder.Entity<RouteDetail>(entity =>
            {
                entity.HasKey(rd => rd.DetailId);

                entity.HasOne(rd => rd.Route)
                    .WithMany(r => r.RouteDetails)
                    .HasForeignKey(rd => rd.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rd => rd.Station)
                    .WithMany(s => s.RouteDetails)
                    .HasForeignKey(rd => rd.StationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(rd => rd.OrderIndex)
                    .IsRequired();
            });

            // ===== TRIP =====
            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasKey(t => t.TripId);

                entity.Property(t => t.DepartureTime)
                    .HasColumnType("datetime");

                entity.Property(t => t.ArrivalTime)
                    .HasColumnType("datetime");

                entity.Property(t => t.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Scheduled");

                entity.HasOne(t => t.Route)
                    .WithMany(r => r.Trips)
                    .HasForeignKey(t => t.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.Driver)
                    .WithMany(d => d.Trips)  
                    .HasForeignKey(t => t.DriverId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(t => t.Vehicle)
                    .WithMany(v => v.Trips)
                    .HasForeignKey(t => t.VehicleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
