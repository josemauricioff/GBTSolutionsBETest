namespace Products.Application.Models;

public enum StockChangeResult
{
    Success,
    NotFound,
    InsufficientStock,
    StockLimitExceeded
}
