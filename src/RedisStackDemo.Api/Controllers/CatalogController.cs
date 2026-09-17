using Microsoft.AspNetCore.Mvc;
using RedisStackDemo.Api.Models;
using RedisStackDemo.Api.Services;

namespace RedisStackDemo.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly IProductCatalogService _catalog;

    public CatalogController(IProductCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] Product product)
    {
        var saved = await _catalog.UpsertAsync(product);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var product = await _catalog.GetAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _catalog.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Full-text + faceted search over the JSON catalog, e.g.
    /// GET /api/catalog/search?text=wireless&category=electronics&maxPrice=100
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? text,
        [FromQuery] string? category,
        [FromQuery] double? maxPrice,
        [FromQuery] int limit = 20)
    {
        var results = await _catalog.SearchAsync(text, category, maxPrice, limit);
        return Ok(results);
    }
}
