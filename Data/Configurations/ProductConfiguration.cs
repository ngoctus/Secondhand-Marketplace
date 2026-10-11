using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web2Hand.Models.Entities;

namespace Web2Hand.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200);
        // Lưu trạng thái dạng chữ ("Active", "Sold"...) để xem trong SQL cho dễ debug
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
    }
}
