using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Products.Application.Abstractions;
using Products.Application.Models;
using Products.Domain.Entities;
using Products.Domain.ValueObjects;
using Xunit;

namespace Products.BddTests;

public sealed class HttpContractTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Testing");

                host.ConfigureServices(services =>
                {
                    services.RemoveAll<IProductRepository>();

                    services.AddSingleton<IProductRepository>(
                        new FakeProductRepository());
                });
            });

    [Fact]
    public async Task DomainValidationIsTranslatedToBadRequest()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Lens",
                price = 0m,
                stock = 1
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var details = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            400,
            details.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task MissingRequestFieldIsRejectedBeforeUseCase()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Lens",
                stock = 1
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProductListIncludesStock()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var products = await client.GetFromJsonAsync<List<ProductDto>>(
            "/api/products");

        Assert.NotNull(products);
        Assert.Equal(5, Assert.Single(products).Stock);
    }

    [Fact]
    public async Task MissingProductMapsToNotFound()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(99999)]
    [InlineData(1000000)]
    public async Task InvalidProductIdentifierMapsToBadRequest(int id)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/products/{id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InsufficientStockMapsToConflict()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            "/api/products/100000/decrement-stock/99",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public Task<IReadOnlyList<Product>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var product = Product.Create("Lens", 1m, 5);

            typeof(Product)
                .GetProperty(nameof(Product.Id))!
                .SetValue(product, Product.CreateIdentifier(100000));

            return Task.FromResult<IReadOnlyList<Product>>([product]);
        }

        public Task<Product?> GetByIdAsync(
            PositiveNumber<int> id,
            CancellationToken cancellationToken) =>
            Task.FromResult<Product?>(null);

        public Task<Product> AddAsync(
            Product product,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            PositiveNumber<int> id,
            Product product,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            PositiveNumber<int> id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Product>> SearchAsync(
            string name,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Product>> GetByStockRangeAsync(
            int minimum,
            int maximum,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StockChangeResult> IncreaseStockAsync(
            PositiveNumber<int> id,
            PositiveNumber<int> quantity,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StockChangeResult> DecreaseStockAsync(
            PositiveNumber<int> id,
            PositiveNumber<int> quantity,
            CancellationToken cancellationToken) =>
            Task.FromResult(StockChangeResult.InsufficientStock);
    }
}