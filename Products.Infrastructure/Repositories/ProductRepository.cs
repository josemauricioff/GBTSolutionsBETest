using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Application.Models;
using Products.Domain.Entities;
using Products.Domain.ValueObjects;
using Products.Infrastructure.Data;

namespace Products.Infrastructure.Repositories;

public sealed class ProductRepository(ProductDbContext database) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken) =>
        await database.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(
        PositiveNumber<int> id,
        CancellationToken cancellationToken) =>
        database.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

    public async Task<Product> AddAsync(
        Product product,
        CancellationToken cancellationToken)
    {
        database.Products.Add(product);

        await database.SaveChangesAsync(cancellationToken);

        return product;
    }

    public async Task<bool> UpdateAsync(
        PositiveNumber<int> id,
        Product product,
        CancellationToken cancellationToken) =>
        await database.Products
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Name, product.Name)
                    .SetProperty(item => item.Price, product.Price)
                    .SetProperty(item => item.Stock, product.Stock),
                cancellationToken) > 0;

    public async Task<bool> DeleteAsync(
        PositiveNumber<int> id,
        CancellationToken cancellationToken) =>
        await database.Products
            .Where(product => product.Id == id)
            .ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string name,
        CancellationToken cancellationToken) =>
        await database.Products
            .AsNoTracking()
            .Where(product =>
                EF.Functions
                    .Collate(product.Name, "Latin1_General_100_CI_AS")
                    .Contains(name))
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByStockRangeAsync(
        int minimum,
        int maximum,
        CancellationToken cancellationToken) =>
        await database.Products
            .FromSqlInterpolated($"""
                SELECT [Id], [Name], [Price], [Stock]
                FROM [Products]
                WHERE [Stock] >= {minimum} AND [Stock] <= {maximum}
                """)
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

    public async Task<StockChangeResult> IncreaseStockAsync(
        PositiveNumber<int> id,
        PositiveNumber<int> quantity,
        CancellationToken cancellationToken)
    {
        var amount = quantity.Value;

        var affectedRows =
            await database.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [Products]
                SET [Stock] = [Stock] + {amount}
                WHERE [Id] = {id.Value}
                  AND [Stock] <= {int.MaxValue - amount}
                """, cancellationToken);

        if (affectedRows > 0)
            return StockChangeResult.Success;

        return await ExistsAsync(id, cancellationToken)
            ? StockChangeResult.StockLimitExceeded
            : StockChangeResult.NotFound;
    }

    public async Task<StockChangeResult> DecreaseStockAsync(
        PositiveNumber<int> id,
        PositiveNumber<int> quantity,
        CancellationToken cancellationToken)
    {
        var amount = quantity.Value;

        var affectedRows =
            await database.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [Products]
                SET [Stock] = [Stock] - {amount}
                WHERE [Id] = {id.Value}
                  AND [Stock] >= {amount}
                """, cancellationToken);

        if (affectedRows > 0)
            return StockChangeResult.Success;

        return await ExistsAsync(id, cancellationToken)
            ? StockChangeResult.InsufficientStock
            : StockChangeResult.NotFound;
    }

    private Task<bool> ExistsAsync(
        PositiveNumber<int> id,
        CancellationToken cancellationToken) =>
        database.Products.AnyAsync(
            product => product.Id == id,
            cancellationToken);
}