using DotnetApiTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotnetApiTemplate.Persistence.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Make)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Model)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Year)
            .IsRequired();

        builder.Property(c => c.Color)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(c => c.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(c => c.VIN)
            .HasMaxLength(17);

        builder.Property(c => c.Mileage)
            .IsRequired();

        builder.Property(c => c.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes for common queries
        builder.HasIndex(c => c.Make);
        builder.HasIndex(c => c.Model);
        builder.HasIndex(c => c.Year);
        builder.HasIndex(c => c.Price);
        builder.HasIndex(c => c.IsAvailable);
        builder.HasIndex(c => new { c.Make, c.Model });
    }
}
