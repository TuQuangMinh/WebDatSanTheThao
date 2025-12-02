using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace web.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<San> Sans { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SanImage> SanImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<BookingSlot> BookingSlots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình cho San entity
            modelBuilder.Entity<San>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(s => s.PricePerHour)
                    .HasColumnType("decimal(18,2)")
                    .HasPrecision(18, 2);
            });

            // Cấu hình cho Category entity
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            // Cấu hình cho SanImage entity
            modelBuilder.Entity<SanImage>(entity =>
            {
                entity.HasKey(si => si.Id);
                entity.Property(si => si.Url)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasOne<San>()
                    .WithMany(s => s.Images)
                    .HasForeignKey(si => si.SanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình cho Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.OrderDate)
                    .IsRequired();
                entity.Property(o => o.UserId)
                    .IsRequired();
                entity.Property(o => o.TotalPrice)
                    .HasColumnType("decimal(18,2)")
                    .HasPrecision(18, 2);
            });

            // Cấu hình cho OrderDetail entity
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.HasKey(od => od.Id);
                entity.Property(od => od.Price)
                    .HasColumnType("decimal(18,2)")
                    .HasPrecision(18, 2);
                entity.HasOne(od => od.Order)
                    .WithMany(o => o.OrderDetails)
                    .HasForeignKey(od => od.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(od => od.San)
                    .WithMany()
                    .HasForeignKey(od => od.SanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình cho BookingSlot entity
            modelBuilder.Entity<BookingSlot>(entity =>
            {
                entity.HasKey(bs => bs.Id);
                entity.Property(bs => bs.BookingDate)
                    .IsRequired();
                entity.Property(bs => bs.StartTime)
                    .IsRequired();
                entity.Property(bs => bs.EndTime)
                    .IsRequired();
                entity.Property(bs => bs.Status)
                    .IsRequired()
                    .HasDefaultValue(BookingStatus.Available);

                entity.HasOne(bs => bs.San)
                    .WithMany()
                    .HasForeignKey(bs => bs.SanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(bs => bs.OrderDetail)
                    .WithOne()
                    .HasForeignKey<BookingSlot>(bs => bs.OrderDetailId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Đảm bảo không có slot trùng lặp cho cùng sân, ngày và thời gian
                entity.HasIndex(bs => new { bs.SanId, bs.BookingDate, bs.StartTime, bs.EndTime })
                    .IsUnique();
            });
        }
    }
}