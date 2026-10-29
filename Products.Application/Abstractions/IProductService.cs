using Products.Application.Models;

namespace Products.Application.Abstractions;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductDto> CreateAsync(string name, decimal price, int stock, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, string name, decimal price, int stock, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductDto>> SearchAsync(string? name, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductDto>> GetByStockRangeAsync(int? minimum, int? maximum, CancellationToken cancellationToken);
    Task<StockChangeResult> IncreaseStockAsync(int id, int quantity, CancellationToken cancellationToken);
    Task<StockChangeResult> DecreaseStockAsync(int id, int quantity, CancellationToken cancellationToken);
}
