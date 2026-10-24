using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Non.Domain.Entities;

namespace Non.Infrastructure.Persistence.Configurations;

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visits");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Note).HasMaxLength(1000);

        builder.HasOne(v => v.Shop)
            .WithMany()
            .HasForeignKey(v => v.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Agent)
            .WithMany()
            .HasForeignKey(v => v.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.ShopId, v.VisitedAt });
        builder.HasIndex(v => new { v.AgentId, v.VisitedAt });
    }
}