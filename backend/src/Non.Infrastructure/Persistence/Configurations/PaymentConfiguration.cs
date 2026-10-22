using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Non.Domain.Entities;

namespace Non.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Ignore(p => p.IsReversal);

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.ReversalReason).HasMaxLength(500);

        builder.HasOne(p => p.Shop)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ReceivedByUser)
            .WithMany()
            .HasForeignKey(p => p.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ReversalOfPayment)
            .WithMany()
            .HasForeignKey(p => p.ReversalOfPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ShopId);
    }
}