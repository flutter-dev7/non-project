using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Auth.Request;

public class SendCodeRequest
{
    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
}