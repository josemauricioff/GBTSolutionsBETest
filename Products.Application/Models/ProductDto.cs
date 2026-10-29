namespace Products.Application.Models;

public sealed record ProductDto(int Id, string Name, decimal Price, int Stock);
