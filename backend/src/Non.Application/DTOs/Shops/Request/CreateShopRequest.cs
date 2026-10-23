using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Shops.Request;

public class CreateShopRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ContactName { get; set; }
}