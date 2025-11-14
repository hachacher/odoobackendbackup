using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdooBackend.Models
{ 
    public class InventoryEntry
    { 
        public long Id { get; set; }

        public long SessionId { get; set; }
         
        public string? Barcode { get; set; }

        public int Quantity { get; set; }

        public DateTime ScannedAt { get; set; }

        public string? Comment { get; set; } 

        // Navigation
        [ForeignKey(nameof(SessionId))]
        public InventorySession? InventorySession { get; set; } 
    }

}

