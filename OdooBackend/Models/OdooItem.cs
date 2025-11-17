using System;
namespace OdooBackend.Models
{
    public class OdooItem
    {
        public string? ItemNumber { get; set; }
        public string? Name { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public List<string>? Variants { get; set; }
    }
}

