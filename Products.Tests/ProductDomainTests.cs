using Products.Domain.Entities;
using Products.Domain.Exceptions;
using Products.Domain.ValueObjects;

namespace Products.Tests;

public sealed class ProductDomainTests
{
    [Fact]
    public void CreateTrimsNameAndPreservesValidDetails()
    {
        var product = Product.Create("  Optical lens  ", 12.50m, 0);

        Assert.Equal("Optical lens", product.Name);
        Assert.Equal(12.50m, product.Price.Value);
        Assert.Equal(0, product.Stock.Value);
        Assert.Null(product.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsMissingName(string? name) =>
        Assert.Throws<DomainValidationException>(
            () => Product.Create(name!, 1m, 0));

    [Fact]
    public void CreateRejectsOverlongName() =>
        Assert.Throws<DomainValidationException>(
            () => Product.Create(new string('a', 201), 1m, 0));

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000")]
    public void CreateRejectsInvalidPrice(string value)
    {
        var price = decimal.Parse(
            value,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<DomainValidationException>(
            () => Product.Create("Lens", price, 0));
    }

    [Fact]
    public void CreateRejectsNegativeStock() =>
        Assert.Throws<DomainValidationException>(
            () => Product.Create("Lens", 1m, -1));

    [Fact]
    public void CreateAcceptsMaximumValues()
    {
        var product = Product.Create(
            new string('a', 200),
            9999999999.99m,
            int.MaxValue);

        Assert.Equal(int.MaxValue, product.Stock.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99999)]
    [InlineData(1000000)]
    [InlineData(int.MaxValue)]
    public void CreateIdentifierRejectsValuesOutsideTheSixDigitRange(
        int value) =>
        Assert.Throws<DomainValidationException>(
            () => Product.CreateIdentifier(value));

    [Theory]
    [InlineData(100000)]
    [InlineData(999999)]
    public void CreateIdentifierAcceptsSixDigitBoundaries(int value) =>
        Assert.Equal(value, Product.CreateIdentifier(value).Value);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PositiveQuantityRejectsNonPositiveValues(int quantity) =>
        Assert.Throws<DomainValidationException>(
            () => new PositiveNumber<int>(quantity));

    [Fact]
    public void PositiveQuantityAcceptsPositiveValue() =>
        Assert.Equal(5, new PositiveNumber<int>(5).Value);
}