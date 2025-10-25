using ApiDemo.Api.Models;
using ApiDemo.Api.Models.Requests;
using ApiDemo.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ApiDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductRepository repository, ILogger<ProductsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAllAsync()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetByIdAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateAsync([FromBody] ProductCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Price = request.Price
        };

        var created = await _repository.CreateAsync(product);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] ProductUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = new Product
        {
            Id = id,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Price = request.Price
        };

        var updated = await _repository.UpdateAsync(product);
        if (!updated)
        {
            _logger.LogInformation("Attempted to update product {ProductId} but it was not found.", id);
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
        {
            _logger.LogInformation("Attempted to delete product {ProductId} but it was not found.", id);
            return NotFound();
        }

        return NoContent();
    }
}
