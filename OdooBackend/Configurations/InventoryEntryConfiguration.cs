using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OdooBackend.Models;

namespace OdooBackend.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class InventoryEntryConfiguration : IEntityTypeConfiguration<InventoryEntry>
    {
        public void Configure(EntityTypeBuilder<InventoryEntry> builder)
        {
            builder.ToTable("InventoryEntries");

            // Primary Key
            builder.HasKey(e => e.Id);

            // ID (auto-incremented)
            builder.Property(e => e.Id)
                   .ValueGeneratedOnAdd();

            // SessionId with FK
            builder.Property(e => e.SessionId)
                   .IsRequired();

            builder.HasOne(e => e.InventorySession)
                   .WithMany(s => s.InventoryEntries)
                   .HasForeignKey(e => e.SessionId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Barcode
            builder.Property(e => e.Barcode)
                   .IsRequired()
                   .HasColumnType("nvarchar(max)");

            // Quantity with default 1
            builder.Property(e => e.Quantity)
                   .IsRequired()
                   .HasDefaultValue(1);

            // ScannedAt with default GETDATE()
            builder.Property(e => e.ScannedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            // Comment (nullable)
            builder.Property(e => e.Comment)
                   .HasColumnType("nvarchar(max)");
        }
    }

}

