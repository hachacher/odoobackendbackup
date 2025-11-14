using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OdooBackend.Configurations;
using OdooBackend.Models;

namespace OdooBackend
{ 
    public class OdooDBContext : DbContext
    {
        public OdooDBContext(DbContextOptions<OdooDBContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        //public DbSet<Item> Items => Set<Item>();
        public DbSet<InventorySession> InventorySessions => Set<InventorySession>();
        public DbSet<InventoryEntry> InventoryEntries => Set<InventoryEntry>();
        public DbSet<Location> Locations => Set<Location>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new InventorySessionConfiguration());
            // Also apply the other configuration
            modelBuilder.ApplyConfiguration(new InventoryEntryConfiguration());

            base.OnModelCreating(modelBuilder);
            // Optional: Fluent API if needed
        }
    }
}
