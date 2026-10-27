namespace Non.Application.DTOs.Auth.Response;

public class ResetTokenResponse
{
    public string ResetToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}