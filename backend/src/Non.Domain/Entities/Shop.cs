using Non.Domain.Common;

namespace Non.Domain.Entities;

public class Shop : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? ContactName { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public decimal? DebtLimit { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}