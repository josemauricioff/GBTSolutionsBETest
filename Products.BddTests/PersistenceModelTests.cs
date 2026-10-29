using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Products.Domain.Entities;
using Products.Domain.ValueObjects;
using Products.Infrastructure.Data;
using Xunit;

namespace Products.BddTests;

public sealed class PersistenceModelTests
{
    private static ProductDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ProductDbContext>()
                .UseSqlServer(
                    "Server=localhost;Database=unused;Integrated Security=True;TrustServerCertificate=True")
                .Options);

    [Fact]
    public void MovedModelMatchesMigrationSnapshot()
    {
        using var database = CreateContext();

        Assert.False(database.Database.HasPendingModelChanges());
    }

    [Fact]
    public void MigrationScriptContainsSequenceConstraintsAndSeedData()
    {
        using var database = CreateContext();

        var script = database
            .GetService<IMigrator>()
            .GenerateScript();

        Assert.Contains(
            "CREATE SEQUENCE [dbo].[ProductIdSequence]",
            script);

        Assert.Contains("MAXVALUE 999999", script);
        Assert.Contains("NO CYCLE", script);
        Assert.Contains("PRIMARY KEY", script);
        Assert.Contains("CK_Products_Id", script);
        Assert.Contains("Optical lens", script);
    }

    [Fact]
    public void IdIsGeneratedByDatabaseSequence()
    {
        using var database = CreateContext();

        var idProperty = database.Model
            .FindEntityType(typeof(Product))!
            .FindProperty(nameof(Product.Id))!;

        Assert.Equal(
            "NEXT VALUE FOR [dbo].[ProductIdSequence]",
            idProperty.GetDefaultValueSql());
    }

    [Fact]
    public void IdentifierConverterPreservesRequiredIntegerColumnAndValidatesSixDigits()
    {
        using var database = CreateContext();

        var property = database.Model
            .FindEntityType(typeof(Product))!
            .FindProperty(nameof(Product.Id))!;

        var converter = property.GetTypeMapping().Converter!;

        Assert.Equal("int", property.GetColumnType());
        Assert.False(property.IsNullable);

        Assert.Equal(
            100000,
            converter.ConvertToProvider(
                Product.CreateIdentifier(100000)));

        Assert.Equal(
            Product.CreateIdentifier(999999),
            converter.ConvertFromProvider(999999));

        Assert.Throws<Products.Domain.Exceptions.DomainValidationException>(
            () => converter.ConvertFromProvider(99999));
    }

    [Fact]
    public void NewProductsUseTemporaryTrackingKeysWhileTheirIdentifiersRemainUnassigned()
    {
        using var database = CreateContext();

        var first = Product.Create("First lens", 1m, 0);
        var second = Product.Create("Second lens", 1m, 0);

        var firstIdentifier = database.Products
            .Add(first)
            .Property(product => product.Id);

        var secondIdentifier = database.Products
            .Add(second)
            .Property(product => product.Id);

        Assert.True(firstIdentifier.IsTemporary);
        Assert.True(secondIdentifier.IsTemporary);

        Assert.NotNull(firstIdentifier.CurrentValue);
        Assert.NotNull(secondIdentifier.CurrentValue);

        Assert.NotEqual(
            firstIdentifier.CurrentValue,
            secondIdentifier.CurrentValue);

        Assert.True(
            firstIdentifier.CurrentValue.Value > Product.MaximumIdentifier);

        Assert.True(
            secondIdentifier.CurrentValue.Value > Product.MaximumIdentifier);

        Assert.Null(first.Id);
        Assert.Null(second.Id);
    }

    [Fact]
    public void LookupByValueObjectIdentifierTranslatesToParameterizedSql()
    {
        using var database = CreateContext();

        var identifier = Product.CreateIdentifier(100000);

        var sql = database.Products
            .Where(product => product.Id == identifier)
            .ToQueryString();

        Assert.Contains(" int = 100000;", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void NumericValueObjectsRoundTripThroughExistingSqlColumnTypes()
    {
        using var database = CreateContext();

        var entity = database.Model.FindEntityType(typeof(Product))!;

        var price = entity.FindProperty(nameof(Product.Price))!;
        var stock = entity.FindProperty(nameof(Product.Stock))!;

        var priceConverter = price.GetTypeMapping().Converter!;
        var stockConverter = stock.GetTypeMapping().Converter!;

        Assert.Equal("decimal(12,2)", price.GetColumnType());
        Assert.Equal("int", stock.GetColumnType());

        Assert.False(price.IsNullable);
        Assert.False(stock.IsNullable);

        Assert.Equal(
            12.50m,
            priceConverter.ConvertToProvider(
                new PositiveNumber<decimal>(12.50m)));

        Assert.Equal(
            new PositiveNumber<decimal>(12.50m),
            priceConverter.ConvertFromProvider(12.50m));

        Assert.Equal(
            0,
            stockConverter.ConvertToProvider(
                new NonNegativeNumber<int>(0)));

        Assert.Equal(
            new NonNegativeNumber<int>(0),
            stockConverter.ConvertFromProvider(0));
    }
}