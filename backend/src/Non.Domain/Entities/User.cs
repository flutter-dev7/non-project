using Non.Domain.Common;
using Non.Domain.Enums;

namespace Non.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public bool EmailConfirmed { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string? FcmToken { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}