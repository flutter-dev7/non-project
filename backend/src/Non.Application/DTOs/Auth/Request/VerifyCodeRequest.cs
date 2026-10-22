using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Auth.Request;

public class VerifyCodeRequest
{
    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Код должен состоять из 6 цифр.")]
    public string Code { get; set; } = string.Empty;
}