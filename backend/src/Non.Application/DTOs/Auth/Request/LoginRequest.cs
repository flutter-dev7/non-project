using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Auth.Request;

public class LoginRequest
{
    [Required]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
}