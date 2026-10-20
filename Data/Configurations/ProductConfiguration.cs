using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200);
        // Lưu trạng thái dạng chữ ("Active", "Sold"...) để xem trong SQL cho dễ debug
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
    }
}
