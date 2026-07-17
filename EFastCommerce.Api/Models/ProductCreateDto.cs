using System;
using System.ComponentModel.DataAnnotations;

namespace EFastCommerce.Api.Models
{
    public class ProductCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public string MeasurementUnit { get; set; } = "Piece"; // expects enum name

        public string? Size { get; set; }
    }
}
