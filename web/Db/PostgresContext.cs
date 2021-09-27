using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace web.Db
{
    public partial class PostgresContext : DbContext
    {
        public PostgresContext()
        {
        }

        public PostgresContext(DbContextOptions<PostgresContext> options)
            : base(options)
        {
        }

        public virtual DbSet<SensorData> SensorData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // throw something?
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SensorData>(entity =>
            {
                entity.ToTable("sensor_data");

                entity.HasIndex(e => new { e.DeviceId, e.ReceivedAt })
                    .HasName("sensor_data_idx_1");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.DeviceId)
                    .IsRequired()
                    .HasColumnName("device_id")
                    .HasMaxLength(32);

                entity.Property(e => e.Humidity)
                    .HasColumnName("humidity")
                    .HasColumnType("numeric");

                entity.Property(e => e.Ppm)
                    .HasColumnName("ppm")
                    .HasColumnType("numeric");

                entity.Property(e => e.ReceivedAt).HasColumnName("received_at");

                entity.Property(e => e.Temperature)
                    .HasColumnName("temperature")
                    .HasColumnType("numeric");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
