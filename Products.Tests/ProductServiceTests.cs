using Products.Application;
using Products.Application.Abstractions;
using Products.Application.Models;
using Products.Domain.Entities;
using Products.Domain.Exceptions;
using Products.Domain.ValueObjects;

namespace Products.Tests;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task CreateDoesNotPersistInvalidProduct()
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.CreateAsync("Lens", 0m, 0, default));

        Assert.Null(repository.SavedProduct);
    }

    [Fact]
    public async Task CreatePersistsNormalizedProductAndReturnsDto()
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository);

        var result = await service.CreateAsync(
            "  Lens  ",
            10m,
            3,
            default);

        Assert.NotNull(repository.SavedProduct);
        Assert.Equal("Lens", repository.SavedProduct.Name);
        Assert.Equal("Lens", result.Name);
        Assert.Equal(10m, result.Price);
        Assert.Equal(3, result.Stock);
        Assert.Equal(100000, result.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SearchRejectsMissingNameBeforeQueryingRepository(
        string? name)
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.SearchAsync(name, default));

        Assert.Null(repository.SearchTerm);
    }

    [Fact]
    public async Task SearchTrimsInputAndIncludesStockInResponse()
    {
        var repository = new FakeProductRepository();

        var result = await new ProductService(repository)
            .SearchAsync("  lens  ", default);

        Assert.Equal("lens", repository.SearchTerm);
        Assert.Equal(5, Assert.Single(result).Stock);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(0, null)]
    [InlineData(-1, 10)]
    [InlineData(10, 9)]
    public async Task StockRangeRejectsInvalidBounds(
        int? minimum,
        int? maximum)
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetByStockRangeAsync(
                minimum,
                maximum,
                default));

        Assert.False(repository.RangeWasQueried);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task StockChangesRejectNonPositiveQuantities(int quantity)
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.DecreaseStockAsync(
                100000,
                quantity,
                default));

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.IncreaseStockAsync(
                100000,
                quantity,
                default));

        Assert.Equal(0, repository.StockChangeCalls);
    }

    [Theory]
    [InlineData(StockChangeResult.Success)]
    [InlineData(StockChangeResult.NotFound)]
    [InlineData(StockChangeResult.InsufficientStock)]
    public async Task DecrementPreservesAtomicRepositoryOutcome(
        StockChangeResult expected)
    {
        var repository = new FakeProductRepository
        {
            StockResult = expected
        };

        var result = await new ProductService(repository)
            .DecreaseStockAsync(100000, 3, default);

        Assert.Equal(expected, result);
        Assert.Equal(1, repository.StockChangeCalls);
        Assert.Equal(3, repository.LastQuantity);
    }

    [Fact]
    public async Task IncrementPreservesOverflowOutcome()
    {
        var repository = new FakeProductRepository
        {
            StockResult = StockChangeResult.StockLimitExceeded
        };

        var result = await new ProductService(repository)
            .IncreaseStockAsync(100000, 1, default);

        Assert.Equal(StockChangeResult.StockLimitExceeded, result);
    }

    [Fact]
    public async Task MissingProductReturnsNull()
    {
        var repository = new FakeProductRepository();

        Assert.Null(
            await new ProductService(repository)
                .GetByIdAsync(999999, default));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(99999)]
    [InlineData(1000000)]
    public async Task ProductOperationsRejectInvalidIdentifiersBeforeRepositoryCalls(
        int id)
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetByIdAsync(id, default));

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UpdateAsync(id, "Lens", 1m, 0, default));

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.DeleteAsync(id, default));

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.IncreaseStockAsync(id, 1, default));

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.DecreaseStockAsync(id, 1, default));

        Assert.Null(repository.LastIdentifier);
        Assert.Null(repository.SavedProduct);
        Assert.Equal(0, repository.StockChangeCalls);
    }

    [Fact]
    public async Task ProductLookupPassesValidatedIdentifierToRepository()
    {
        var repository = new FakeProductRepository();

        await new ProductService(repository)
            .GetByIdAsync(123456, default);

        Assert.Equal(
            new PositiveNumber<int>(123456),
            repository.LastIdentifier);
    }

    [Fact]
    public async Task UpdateDoesNotPersistInvalidDetails()
    {
        var repository = new FakeProductRepository();

        await Assert.ThrowsAsync<DomainValidationException>(
            () => new ProductService(repository)
                .UpdateAsync(100000, "Lens", 1m, -1, default));

        Assert.Null(repository.SavedProduct);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public Product? SavedProduct { get; private set; }

        public PositiveNumber<int>? LastIdentifier { get; private set; }

        public string? SearchTerm { get; private set; }

        public bool RangeWasQueried { get; private set; }

        public int StockChangeCalls { get; private set; }

        public int LastQuantity { get; private set; }

        public StockChangeResult StockResult { get; init; } =
            StockChangeResult.Success;

        public Task<IReadOnlyList<Product>> GetAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Product>>(
                [WithPersistedIdentifier(Product.Create("Lens", 10m, 5))]);

        public Task<Product?> GetByIdAsync(
            PositiveNumber<int> id,
            CancellationToken cancellationToken)
        {
            LastIdentifier = id;

            return Task.FromResult<Product?>(null);
        }

        public Task<Product> AddAsync(
            Product product,
            CancellationToken cancellationToken)
        {
            SavedProduct = WithPersistedIdentifier(product);

            return Task.FromResult(product);
        }

        public Task<bool> UpdateAsync(
            PositiveNumber<int> id,
            Product product,
            CancellationToken cancellationToken)
        {
            LastIdentifier = id;
            SavedProduct = product;

            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(
            PositiveNumber<int> id,
            CancellationToken cancellationToken)
        {
            LastIdentifier = id;

            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<Product>> SearchAsync(
            string name,
            CancellationToken cancellationToken)
        {
            SearchTerm = name;

            return GetAllAsync(cancellationToken);
        }

        public Task<IReadOnlyList<Product>> GetByStockRangeAsync(
            int minimum,
            int maximum,
            CancellationToken cancellationToken)
        {
            RangeWasQueried = true;

            return GetAllAsync(cancellationToken);
        }

        public Task<StockChangeResult> IncreaseStockAsync(
            PositiveNumber<int> id,
            PositiveNumber<int> quantity,
            CancellationToken cancellationToken) =>
            RecordStockChange(id, quantity);

        public Task<StockChangeResult> DecreaseStockAsync(
            PositiveNumber<int> id,
            PositiveNumber<int> quantity,
            CancellationToken cancellationToken) =>
            RecordStockChange(id, quantity);

        private Task<StockChangeResult> RecordStockChange(
            PositiveNumber<int> id,
            PositiveNumber<int> quantity)
        {
            LastIdentifier = id;
            StockChangeCalls++;
            LastQuantity = quantity.Value;

            return Task.FromResult(StockResult);
        }

        private static Product WithPersistedIdentifier(Product product)
        {
            typeof(Product)
                .GetProperty(nameof(Product.Id))!
                .SetValue(product, Product.CreateIdentifier(100000));

            return product;
        }
    }
}