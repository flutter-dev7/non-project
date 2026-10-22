using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Products.Request;

public class CreateProductRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int? WeightInGrams { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }
}