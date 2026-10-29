using Products.Domain.Exceptions;
using Products.Domain.ValueObjects;

namespace Products.Domain.Entities;

public sealed class Product
{
    public const int MinimumIdentifier = 100000;
    public const int MaximumIdentifier = 999999;

    private Product() { }

    public PositiveNumber<int>? Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public PositiveNumber<decimal> Price { get; private set; } = null!;

    public NonNegativeNumber<int> Stock { get; private set; } = null!;

    public static PositiveNumber<int> CreateIdentifier(int value)
    {
        var identifier = new PositiveNumber<int>(value);

        if (identifier.Value < MinimumIdentifier ||
            identifier.Value > MaximumIdentifier)
        {
            throw new DomainValidationException(
                "Product identifier must have exactly six digits.");
        }

        return identifier;
    }

    public static Product Create(string name, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new DomainValidationException(
                "Name is required and must have at most 200 characters.");
        }

        var validatedPrice = new PositiveNumber<decimal>(price);
        var validatedStock = new NonNegativeNumber<int>(stock);

        if (validatedPrice.Value > 9999999999.99m ||
            decimal.Round(validatedPrice.Value, 2) != validatedPrice.Value)
        {
            throw new DomainValidationException(
                "Price must have at most two decimal places and not exceed 9999999999.99.");
        }

        return new Product
        {
            Name = name.Trim(),
            Price = validatedPrice,
            Stock = validatedStock
        };
    }
}