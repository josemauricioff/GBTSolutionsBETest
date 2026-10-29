using Products.Application.Abstractions;
using Products.Application.Models;
using Products.Domain.Entities;
using Products.Domain.Exceptions;
using Products.Domain.ValueObjects;

namespace Products.Application;

public sealed class ProductService(IProductRepository repository) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken) =>
        Map(await repository.GetAllAsync(cancellationToken));

    public async Task<ProductDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(
            Product.CreateIdentifier(id),
            cancellationToken);

        return product is null ? null : Map(product);
    }

    public async Task<ProductDto> CreateAsync(
        string name,
        decimal price,
        int stock,
        CancellationToken cancellationToken)
    {
        var product = Product.Create(name, price, stock);

        return Map(await repository.AddAsync(product, cancellationToken));
    }

    public Task<bool> UpdateAsync(
        int id,
        string name,
        decimal price,
        int stock,
        CancellationToken cancellationToken) =>
        repository.UpdateAsync(
            Product.CreateIdentifier(id),
            Product.Create(name, price, stock),
            cancellationToken);

    public Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken) =>
        repository.DeleteAsync(
            Product.CreateIdentifier(id),
            cancellationToken);

    public async Task<IReadOnlyList<ProductDto>> SearchAsync(
        string? name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Name is required.");

        return Map(
            await repository.SearchAsync(
                name.Trim(),
                cancellationToken));
    }

    public async Task<IReadOnlyList<ProductDto>> GetByStockRangeAsync(
        int? minimum,
        int? maximum,
        CancellationToken cancellationToken)
    {
        if (!minimum.HasValue ||
            !maximum.HasValue ||
            minimum < 0 ||
            maximum < minimum)
        {
            throw new DomainValidationException(
                "Provide a non-negative min and a max greater than or equal to min.");
        }

        return Map(
            await repository.GetByStockRangeAsync(
                minimum.Value,
                maximum.Value,
                cancellationToken));
    }

    public Task<StockChangeResult> IncreaseStockAsync(
        int id,
        int quantity,
        CancellationToken cancellationToken) =>
        repository.IncreaseStockAsync(
            Product.CreateIdentifier(id),
            new PositiveNumber<int>(quantity),
            cancellationToken);

    public Task<StockChangeResult> DecreaseStockAsync(
        int id,
        int quantity,
        CancellationToken cancellationToken) =>
        repository.DecreaseStockAsync(
            Product.CreateIdentifier(id),
            new PositiveNumber<int>(quantity),
            cancellationToken);

    private static ProductDto Map(Product product) =>
        new(
            product.Id?.Value
                ?? throw new InvalidOperationException(
                    "Product identifier has not been assigned by persistence."),
            product.Name,
            product.Price.Value,
            product.Stock.Value);

    private static IReadOnlyList<ProductDto> Map(
        IReadOnlyList<Product> products) =>
        products.Select(Map).ToArray();
}