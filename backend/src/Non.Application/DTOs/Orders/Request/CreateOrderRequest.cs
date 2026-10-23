using System.ComponentModel.DataAnnotations;
using Non.Domain.Enums;

namespace Non.Application.DTOs.Orders.Request;

public class CreateOrderRequest
{
    public Guid? Id { get; set; }

    public Guid? ShopId { get; set; }

    public DateOnly DeliveryDate { get; set; }

    [EnumDataType(typeof(PaymentType))]
    public PaymentType PaymentType { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Добавьте хотя бы одну позицию.")]
    [MaxLength(100)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}