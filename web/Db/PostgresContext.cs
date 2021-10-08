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

        public virtual DbSet<SensorSCD30> SensorSCD30 { get; set; }

        public virtual DbSet<SensorSGP40> SensorSGP40 { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // throw something?
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SensorSCD30>(entity =>
            {
                entity.ToTable("sensor_data_scd30");

                entity.HasIndex(e => new { e.DeviceId, e.ReceivedAt })
                    .HasName("sensor_data_scd30_idx_1");

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

                entity.Property(e => e.Render)
                    .HasColumnName("render")
                    .HasColumnType("boolean");
            });

            modelBuilder.Entity<SensorSGP40>(entity =>
            {
                entity.ToTable("sensor_data_sgp40");

                entity.HasIndex(e => new { e.DeviceId, e.ReceivedAt })
                    .HasName("sensor_data_sgp40_idx_1");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.DeviceId)
                    .IsRequired()
                    .HasColumnName("device_id")
                    .HasMaxLength(32);

                entity.Property(e => e.Voc)
                    .HasColumnName("voc")
                    .HasColumnType("numeric");

                entity.Property(e => e.ReceivedAt).HasColumnName("received_at");

                entity.Property(e => e.Render)
                    .HasColumnName("render")
                    .HasColumnType("boolean");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
