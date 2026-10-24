using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Payments.Request;

public class ReversePaymentRequest
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}