using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Data.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
    }
}
