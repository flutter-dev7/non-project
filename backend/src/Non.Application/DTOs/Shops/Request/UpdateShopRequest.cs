namespace Non.Application.DTOs.Shops.Request;

public class UpdateShopRequest : CreateShopRequest
{
    public bool IsActive { get; set; } = true;
}