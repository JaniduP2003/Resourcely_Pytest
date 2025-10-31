using Backend_Resourcely.Models;
using Microsoft.EntityFrameworkCore;

//Imagine your backend as a city and the MODELS are building blueprints
// like user blueprint for pepoles houses
//BUT BLUEPRINTS DONT MAKE A CITY YOU NEED THE BUILDERS
// AND APPDBCONTEX IS THE BUILDER MAKE THE PLANES REAL HOUSES 

namespace Backend_Resourcely.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public virtual DbSet<Booking> Bookings { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Resource> Resources { get; set; }

        // NEW: student + batch
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<Batch> Batches { get; set; }

        // NEW: lecturer + department
        public DbSet<LecturerProfile> LecturerProfiles { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique(); // unique username is handy

                entity.Property(u => u.CreatedAt)
                        .HasColumnType("datetime2(6)")                  // optional but explicit
                        .HasDefaultValueSql("SYSDATETIME()");

                // 1:1 optional: User <-> StudentProfile (only for role = Student)
                // PK=FK pattern on StudentProfile.UserId
                entity.HasOne(u => u.StudentProfile)
                      .WithOne(sp => sp.User)
                      .HasForeignKey<StudentProfile>(sp => sp.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 1:1 optional: User <-> LecturerProfile (only for role = Lecturer)
                entity.HasOne(u => u.LecturerProfile)
                      .WithOne(lp => lp.User)
                      .HasForeignKey<LecturerProfile>(lp => lp.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // StudentProfile entity (strict 1:1 with User; many-to-one to Batch)
            modelBuilder.Entity<StudentProfile>(entity =>
            {
                entity.HasKey(sp => sp.UserId); // PK == FK to Users.Id
                entity.Property(sp => sp.UserId).ValueGeneratedNever();

                entity.HasOne(sp => sp.Batch)
                      .WithMany(b => b.Students)
                      .HasForeignKey(sp => sp.BatchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // LecturerProfile entity (strict 1:1 with User; many-to-one to Department)
            modelBuilder.Entity<LecturerProfile>(entity =>
            {
                entity.HasKey(lp => lp.UserId); // PK == FK to Users.Id
                entity.Property(lp => lp.UserId).ValueGeneratedNever();

                entity.HasOne(lp => lp.Department)
                      .WithMany(d => d.LecturerProfiles)
                      .HasForeignKey(lp => lp.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Batch entity (authoritative source of batch name)
            modelBuilder.Entity<Batch>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
                entity.Property(b => b.Code).HasMaxLength(50);
                entity.Property(b => b.StartDate).HasColumnType("date");
                entity.Property(b => b.EndDate).HasColumnType("date");
                entity.HasIndex(b => b.Name); // common filter
            });

            // Department entity
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(d => d.Name);
            });

            // Building entity
            modelBuilder.Entity<Building>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
                entity.Property(b => b.Description).HasMaxLength(500);
            });

            // Floor entity
            modelBuilder.Entity<Floor>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.Property(f => f.Name).IsRequired().HasMaxLength(100);
                entity.Property(f => f.Description).HasMaxLength(500);

                entity.HasOne(f => f.Building)
                    .WithMany(b => b.Floors)
                    .HasForeignKey(f => f.BuildingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Block entity
            modelBuilder.Entity<Block>(entity =>
            {
                entity.HasKey(bl => bl.Id);
                entity.Property(bl => bl.Name).IsRequired().HasMaxLength(100);
                entity.Property(bl => bl.Description).HasMaxLength(500);

                entity.HasOne(bl => bl.Floor)
                    .WithMany(f => f.Blocks)
                    .HasForeignKey(bl => bl.FloorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Resource entity
            modelBuilder.Entity<Resource>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
                entity.Property(r => r.Type).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Description).HasMaxLength(500);
                entity.Property(r => r.Capacity).IsRequired();

                entity.HasOne(r => r.Block)
                    .WithMany(bl => bl.Resources)
                    .HasForeignKey(r => r.BlockId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Booking entity
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Reason).IsRequired().HasMaxLength(2000);
                entity.Property(b => b.Contact).IsRequired().HasMaxLength(200);
                entity.Property(b => b.Capacity).IsRequired();
                entity.Property(b => b.BookingAt).IsRequired();
                entity.Property(b => b.EndAt).IsRequired();
                entity.Property(b => b.UserId).IsRequired();

                entity.HasOne(b => b.Resource)
                    .WithMany(r => r.Bookings)
                    .HasForeignKey(b => b.ResourceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ explicit relationship to User now that UserId is int
                entity.HasOne(b => b.User)
                    .WithMany() // or .WithMany(u => u.Bookings) if you add a collection on User
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
