using Microsoft.EntityFrameworkCore;
using Products.Domain.Entities;
using Products.Domain.ValueObjects;

namespace Products.Infrastructure.Data;

public sealed class ProductDbContext(
    DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasSequence<int>("ProductIdSequence", "dbo")
            .StartsAt(100003)
            .IncrementsBy(1)
            .HasMin(100000)
            .HasMax(999999)
            .IsCyclic(false);

        model.Entity<Product>(product =>
        {
            product.ToTable("Products", table =>
            {
                table.HasCheckConstraint(
                    "CK_Products_Id",
                    "[Id] BETWEEN 100000 AND 999999");

                table.HasCheckConstraint(
                    "CK_Products_Name",
                    "LEN(LTRIM(RTRIM([Name]))) > 0");

                table.HasCheckConstraint(
                    "CK_Products_Price",
                    "[Price] > 0");

                table.HasCheckConstraint(
                    "CK_Products_Stock",
                    "[Stock] >= 0");
            });

            product.HasKey(item => item.Id);

            product.Property(item => item.Id)
                .HasConversion(
                    number => number!.Value,
                    value => Product.CreateIdentifier(value))
                .HasDefaultValueSql(
                    "NEXT VALUE FOR [dbo].[ProductIdSequence]")
                .ValueGeneratedOnAdd()
                .HasValueGenerator<TemporaryProductIdValueGenerator>()
                .IsRequired();

            product.Property(item => item.Name)
                .HasMaxLength(200)
                .IsRequired();

            product.Property(item => item.Price)
                .HasConversion(
                    number => number.Value,
                    value => new PositiveNumber<decimal>(value))
                .HasPrecision(12, 2)
                .IsRequired();

            product.Property(item => item.Stock)
                .HasConversion(
                    number => number.Value,
                    value => new NonNegativeNumber<int>(value))
                .IsRequired();

            product.HasData(
                new
                {
                    Id = Product.CreateIdentifier(100000),
                    Name = "Optical lens",
                    Price = new PositiveNumber<decimal>(89.90m),
                    Stock = new NonNegativeNumber<int>(25)
                },
                new
                {
                    Id = Product.CreateIdentifier(100001),
                    Name = "Lens cleaner",
                    Price = new PositiveNumber<decimal>(12.50m),
                    Stock = new NonNegativeNumber<int>(50)
                },
                new
                {
                    Id = Product.CreateIdentifier(100002),
                    Name = "Frame",
                    Price = new PositiveNumber<decimal>(125m),
                    Stock = new NonNegativeNumber<int>(10)
                });
        });
    }
}