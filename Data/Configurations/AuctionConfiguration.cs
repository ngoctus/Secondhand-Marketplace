using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web2Hand.Models.Entities;

namespace Web2Hand.Data.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
    }
}
