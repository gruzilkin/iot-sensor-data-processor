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

        public virtual DbSet<CalibrationModels> CalibrationModels { get; set; }
        public virtual DbSet<SensorCalibrationData> SensorCalibrationData { get; set; }
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
            modelBuilder.Entity<CalibrationModels>(entity =>
            {
                entity.HasKey(e => e.DeviceId)
                    .HasName("calibration_models_pkey");

                entity.ToTable("calibration_models");

                entity.Property(e => e.DeviceId)
                    .HasColumnName("device_id")
                    .HasMaxLength(32);

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.Property(e => e.Model)
                    .IsRequired()
                    .HasColumnName("model");
            });

            modelBuilder.Entity<SensorCalibrationData>(entity =>
            {
                entity.ToTable("sensor_calibration_data");

                entity.HasIndex(e => new { e.DeviceId, e.ReceivedAt })
                    .HasName("sensor_calibration_data_idx_1");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.DeviceId)
                    .IsRequired()
                    .HasColumnName("device_id")
                    .HasMaxLength(32);

                entity.Property(e => e.Humidity)
                    .HasColumnName("humidity")
                    .HasColumnType("numeric");

                entity.Property(e => e.IsInvalid)
                    .HasColumnName("is_invalid")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.IsOutlier)
                    .HasColumnName("is_outlier")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Ppm)
                    .HasColumnName("ppm")
                    .HasColumnType("numeric");

                entity.Property(e => e.R0)
                    .HasColumnName("r0")
                    .HasColumnType("numeric");

                entity.Property(e => e.ReceivedAt).HasColumnName("received_at");

                entity.Property(e => e.Temperature)
                    .HasColumnName("temperature")
                    .HasColumnType("numeric");

                entity.Property(e => e.Uptime).HasColumnName("uptime");
            });

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
