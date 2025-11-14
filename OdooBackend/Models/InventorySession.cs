using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdooBackend.Models
{
    [Table("InventorySessions")]
    public class InventorySession
    {
        [Key]
        public long Id { get; set; }

        public int UserId { get; set; }

        public string? Location { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsPosted { get; set; }

        public string? SessionName { get; set; }

        // Navigation
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public ICollection<InventoryEntry>? InventoryEntries { get; set; }
    }
}

