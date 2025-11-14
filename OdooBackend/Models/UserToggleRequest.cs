using System;
using System.ComponentModel.DataAnnotations;

namespace OdooBackend.Models
{
    public class UserToggleRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        public string Location { get; set; } = string.Empty;
    }
}
