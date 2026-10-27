using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Auth.Request;

public class ResetPasswordRequest
{
    [Required]
    [StringLength(200, MinimumLength = 20)]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}