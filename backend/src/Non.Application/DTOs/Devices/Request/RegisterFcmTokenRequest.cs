using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Devices.Request;

public class RegisterFcmTokenRequest
{
    [Required]
    [StringLength(500, MinimumLength = 20)]
    public string Token { get; set; } = string.Empty;
}