using Microsoft.AspNetCore.Mvc;
using Products.Api.Models;
using Products.Application.Abstractions;
using Products.Application.Models;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> Get(int id, CancellationToken cancellationToken)
    {
        var product = await service.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await service.CreateAsync(request.Name!, request.Price!.Value, request.Stock!.Value, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductRequest request, CancellationToken cancellationToken) =>
        await service.UpdateAsync(id, request.Name!, request.Price!.Value, request.Stock!.Value, cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    public async Task<IActionResult> Decrement(int id, int quantity, CancellationToken cancellationToken) =>
        ToHttpResult(await service.DecreaseStockAsync(id, quantity, cancellationToken));

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    public async Task<IActionResult> Add(int id, int quantity, CancellationToken cancellationToken) =>
        ToHttpResult(await service.IncreaseStockAsync(id, quantity, cancellationToken));

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> Search([FromQuery] string? name, CancellationToken cancellationToken) =>
        Ok(await service.SearchAsync(name, cancellationToken));

    [HttpGet("stock-level")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> StockLevel([FromQuery] int? min, [FromQuery] int? max, CancellationToken cancellationToken) =>
        Ok(await service.GetByStockRangeAsync(min, max, cancellationToken));

    private IActionResult ToHttpResult(StockChangeResult result) => result switch
    {
        StockChangeResult.Success => Ok(),
        StockChangeResult.NotFound => NotFound(),
        StockChangeResult.InsufficientStock => Conflict(new ProblemDetails { Status = 409, Title = "Insufficient stock." }),
        StockChangeResult.StockLimitExceeded => Conflict(new ProblemDetails { Status = 409, Title = "Stock limit exceeded." }),
        _ => throw new InvalidOperationException("Unexpected stock change result.")
    };
}
