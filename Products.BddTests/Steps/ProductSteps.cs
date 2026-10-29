using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Data.SqlClient;
using Products.Infrastructure.Data;
using Products.Api.Models;
using Reqnroll;
using Xunit;

namespace Products.BddTests.Steps;

[Binding]
public sealed class ProductSteps
{
    private string databaseName = string.Empty;
    private string masterConnectionString = string.Empty;
    private bool databaseCreated;
    private string connectionString = string.Empty;
    private WebApplicationFactory<Program>? factory;
    private HttpClient client = null!;
    private HttpResponseMessage response = null!;
    private int productId;
    private List<Product>? searchResults;
    private HttpResponseMessage[] concurrentResponses = [];

    [BeforeScenario]
    public async Task StartScenario()
    {
        var testConnection = Environment.GetEnvironmentVariable("BDD_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(testConnection))
        {
            testConnection =
                @"Server=(localdb)\GBTSolutionsTest;Integrated Security=True;TrustServerCertificate=True";
        }

        databaseName = "bdd_" + Guid.NewGuid().ToString("N");

        var builder = new SqlConnectionStringBuilder(testConnection)
        {
            InitialCatalog = "master",
            Pooling = false
        };

        masterConnectionString = builder.ConnectionString;

        await using (var connection = new SqlConnection(masterConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}]", connection);
            await command.ExecuteNonQueryAsync();
            databaseCreated = true;
        }

        builder.InitialCatalog = databaseName;

        connectionString = builder.ConnectionString;

        var options = new DbContextOptionsBuilder<ProductDbContext>().UseSqlServer(connectionString).Options;

        await using (var db = new ProductDbContext(options))
            await db.Database.MigrateAsync();

        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => 
                host.ConfigureServices(services =>
                                        {
                                            services.RemoveAll<IDbContextOptionsConfiguration<ProductDbContext>>();
                                            services.RemoveAll<DbContextOptions<ProductDbContext>>();
                                            services.RemoveAll<ProductDbContext>();
                                            services.AddDbContext<ProductDbContext>(options => options.UseSqlServer(connectionString));
                                        }));

        client = factory.CreateClient();
    }

    [AfterScenario]
    public async Task CleanUpScenario()
    {
        response?.Dispose();

        foreach (var item in concurrentResponses) 
            item.Dispose();

        client?.Dispose();

        if (factory is not null) 
            await factory.DisposeAsync();

        if (!databaseCreated) 
            return;

        await using var connection = new SqlConnection(masterConnectionString);

        await connection.OpenAsync();

        await using var command = new SqlCommand($"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}];", connection);

        await command.ExecuteNonQueryAsync();
    }

    [Given("the product {string} has stock {int}")]
    public async Task GivenProductHasStock(string name, int stock)
    {
        var products = await client.GetFromJsonAsync<List<Product>>("/api/products") ?? throw new InvalidOperationException("Missing products response.");
        var product = Assert.Single(products, p => p.Name == name);
        Assert.Equal(stock, product.Stock);
        productId = product.Id;
    }

    [When("I retrieve product {int}")]
    public async Task RetrieveProduct(int id)
    {
        response = await client.GetAsync($"/api/products/{id}");
    }

    [When("I create a product named {string} with price {decimal} and stock {int}")]
    public async Task CreateProduct(string name, decimal price, int stock)
    {
        response = await client.PostAsJsonAsync("/api/products", new { name, price, stock });
        if (response.StatusCode == HttpStatusCode.Created)
            productId = (await response.Content.ReadFromJsonAsync<Product>())!.Id;
    }

    [When("I decrement the product stock by {int}")]
    public async Task DecrementStock(int quantity) => response = await client.PostAsync($"/api/products/{productId}/decrement-stock/{quantity}", null);

    [When("I add {int} units to the product stock")]
    public async Task AddStock(int quantity) => response = await client.PostAsync($"/api/products/{productId}/add-to-stock/{quantity}", null);

    [When("I search for products named {string}")]
    public async Task Search(string name) => response = await client.GetAsync($"/api/products/search?name={Uri.EscapeDataString(name)}");

    [When("I search for products with stock between {int} and {int}")]
    public async Task SearchStock(int min, int max) => response = await client.GetAsync($"/api/products/stock-level?min={min}&max={max}");

    [When("I update the product name to {string} with price {decimal} and stock {int}")]
    public async Task UpdateProduct(string name, decimal price, int stock) => response = await client.PutAsJsonAsync($"/api/products/{productId}", new { name, price, stock });

    [When("I delete the product")]
    public async Task DeleteProduct() => response = await client.DeleteAsync($"/api/products/{productId}");

    [Then("the response status should be {int}")]
    public void CheckStatus(int status) => Assert.Equal(status, (int)response.StatusCode);

    [Then("the product identifier should have 6 digits")]
    public void CheckId() => Assert.InRange(productId, 100000, 999999);

    [Then("the created product should have name {string} and stock {int}")]
    public async Task CheckProduct(string name, int stock)
    {
        var product = await client.GetFromJsonAsync<Product>($"/api/products/{productId}");
        Assert.NotNull(product); Assert.Equal(name, product.Name); Assert.Equal(stock, product.Stock);
    }

    [Then("the product stock should be {int}")]
    public async Task CheckStock(int stock)
    {
        var product = await client.GetFromJsonAsync<Product>($"/api/products/{productId}");
        Assert.NotNull(product); Assert.Equal(stock, product.Stock);
    }

    [Then("the search results should contain {string} with stock {int}")]
    public async Task CheckResult(string name, int stock)
    {
        var products = await GetSearchResultsAsync();
        Assert.Contains(products, p => p.Name == name && p.Stock == stock);
    }

    [Then("the search results should not contain {string}")]
    public async Task CheckMissingResult(string name)
    {
        var products = await GetSearchResultsAsync();
        Assert.DoesNotContain(products, p => p.Name == name);
    }

    private async Task<List<Product>> GetSearchResultsAsync()
    {
        return searchResults ??= await response.Content.ReadFromJsonAsync<List<Product>>()
         ?? throw new InvalidOperationException("Missing search results.");
    }

    [When("two API instances create {int} products concurrently")]
    public async Task CreateConcurrently(int count)
    {
        using var secondFactory = factory!.WithWebHostBuilder(_ => { });
        using var secondClient = secondFactory.CreateClient();
        var requests = Enumerable.Range(0, count).Select(index =>
         (index % 2 == 0 ? client : secondClient).PostAsJsonAsync("/api/products", new { name = "Concurrent product", price = 1m, stock = 0 }));
        concurrentResponses = await Task.WhenAll(requests);
    }

    [Then("all created products should have unique 6-digit identifiers")]
    public async Task CheckConcurrentIds()
    {
        Assert.All(concurrentResponses, item => Assert.Equal(HttpStatusCode.Created, item.StatusCode));
        var products = await Task.WhenAll(concurrentResponses.Select(item => item.Content.ReadFromJsonAsync<Product>()));
        Assert.All(products, product => { Assert.NotNull(product); Assert.InRange(product.Id, 100000, 999999); });
        Assert.Equal(products.Length, products.Select(product => product!.Id).Distinct().Count());
    }

    [When("two API instances each try to decrement stock by {int} concurrently")]
    public async Task DecrementConcurrently(int quantity)
    {
        using var secondFactory = factory!.WithWebHostBuilder(_ => { });
        using var secondClient = secondFactory.CreateClient();
        concurrentResponses = await Task.WhenAll(
         client.PostAsync($"/api/products/{productId}/decrement-stock/{quantity}", null),
         secondClient.PostAsync($"/api/products/{productId}/decrement-stock/{quantity}", null));
    }

    [Then("one request should succeed and one should report insufficient stock")]
    public void CheckConcurrentStockResults()
    {
        Assert.Single(concurrentResponses, item => item.StatusCode == HttpStatusCode.OK);
        Assert.Single(concurrentResponses, item => item.StatusCode == HttpStatusCode.Conflict);
    }

    [Then("the product should no longer exist")]
    public async Task CheckDeleted() => Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/products/{productId}")).StatusCode);
}




