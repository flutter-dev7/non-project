using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Visits.Request;

public class CreateVisitRequest
{
    [Required]
    public Guid ShopId { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}