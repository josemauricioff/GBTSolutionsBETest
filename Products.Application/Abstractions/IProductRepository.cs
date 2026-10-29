using Products.Application.Models;
using Products.Domain.Entities;
using Products.Domain.ValueObjects;

namespace Products.Application.Abstractions;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(
        PositiveNumber<int> id,
        CancellationToken cancellationToken);

    Task<Product> AddAsync(
        Product product,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        PositiveNumber<int> id,
        Product product,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        PositiveNumber<int> id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> SearchAsync(
        string name,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByStockRangeAsync(
        int minimum,
        int maximum,
        CancellationToken cancellationToken);

    Task<StockChangeResult> IncreaseStockAsync(
        PositiveNumber<int> id,
        PositiveNumber<int> quantity,
        CancellationToken cancellationToken);

    Task<StockChangeResult> DecreaseStockAsync(
        PositiveNumber<int> id,
        PositiveNumber<int> quantity,
        CancellationToken cancellationToken);
}