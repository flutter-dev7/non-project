using System.ComponentModel.DataAnnotations;
using Non.Domain.Enums;

namespace Non.Application.DTOs.Users.Request;

public class UpdateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(254)]
    public string? Email { get; set; }

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
}