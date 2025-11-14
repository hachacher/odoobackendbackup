using System;
namespace OdooBackend.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using OdooBackend.Models;

    public class InventorySessionConfiguration : IEntityTypeConfiguration<InventorySession>
    {
        public void Configure(EntityTypeBuilder<InventorySession> builder)
        {
            builder.ToTable("InventorySessions");

            // Primary key
            builder.HasKey(s => s.Id);

            // Identity column
            builder.Property(s => s.Id)
                   .ValueGeneratedOnAdd();

            // UserId with FK to Users table
            builder.Property(s => s.UserId)
                   .IsRequired();

            builder.HasOne(s => s.User)
                   .WithMany(u => u.InventorySessions)
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Location (nullable)
            builder.Property(s => s.Location)
                   .HasColumnType("nvarchar(max)");
            builder.Property(s => s.SessionName)
                    .HasColumnType("nvarchar(max)");
            // StartDate with default GETDATE()
            builder.Property(s => s.StartDate)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            // EndDate (nullable)
            builder.Property(s => s.EndDate);

            // IsPosted with default 0
            builder.Property(s => s.IsPosted)
                   .IsRequired()
                   .HasDefaultValue(false);
        }
    }

}

