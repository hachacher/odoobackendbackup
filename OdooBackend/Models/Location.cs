using System.ComponentModel.DataAnnotations.Schema;

namespace OdooBackend.Models
{
    public class Location
    {
        public int Id { get; set; }
        
        [Column("Location")]
        public string LocationName { get; set; } = string.Empty;
        public bool active { get; set; }
    }
}
