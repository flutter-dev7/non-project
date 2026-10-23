namespace Non.Application.DTOs.Shops.Response;

public class ShopResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public decimal? DebtLimit { get; set; }
    public bool IsActive { get; set; }

    public Guid? OwnerUserId { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerName { get; set; }
}