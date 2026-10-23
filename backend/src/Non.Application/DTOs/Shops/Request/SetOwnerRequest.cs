using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Shops.Request;

public class SetOwnerRequest
{
    [StringLength(20)]
    public string? OwnerPhone { get; set; }
}