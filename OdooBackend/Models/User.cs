using System;
namespace OdooBackend.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string? UserName { get; set; }  // Renamed from 'User' to avoid conflicts with reserved keywords
        public string? Location { get; set; }

        public int Type { get; set; }  // 1 = Manager

        public string? Password { get; set; }
        
        public bool Inactive { get; set; }

        // Optional: Navigation property
        public ICollection<InventorySession>? InventorySessions { get; set; }
    }

}

