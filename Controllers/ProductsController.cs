using Microsoft.AspNetCore.Mvc;
using SNovimGodom.Data;
using SNovimGodom.Models;
using SNovimGodom.Helpers;

namespace SNovimGodom.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Get(
        int page = 1,
        int pageSize = 10,
        string? category = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? minWeight = null,
        int? maxWeight = null)
    {
        var user = AuthHelper.GetUser(Request, _db);
        var isAdmin = user?.Role == "Admin";
        var query = _db.Products.AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(product => product.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(product => product.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(product => product.Brand == brand);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(product => product.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= maxPrice.Value);
        }

        if (minWeight.HasValue)
        {
            query = query.Where(product => product.Weight >= minWeight.Value);
        }

        if (maxWeight.HasValue)
        {
            query = query.Where(product => product.Weight <= maxWeight.Value);
        }

        var total = query.Count();

        var items = query
            .OrderBy(product => product.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new { items, total });
    }

    [HttpPost]
    public IActionResult Create(Product product)
    {
        var user = AuthHelper.GetUser(Request, _db);

        if (user == null || user.Role != "Admin")
            return Unauthorized();

        _db.Products.Add(product);
        _db.SaveChanges();

        return Ok(product);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Product product)
    {
        var user = AuthHelper.GetUser(Request, _db);
        if (user == null || user.Role != "Admin")
            return Unauthorized();

        var existing = _db.Products.Find(id);
        if (existing == null) return NotFound();

        existing.Name = product.Name;
        existing.Price = product.Price;
        existing.Description = product.Description;
        existing.Category = product.Category;
        existing.Brand = product.Brand;
        existing.ImageUrl = product.ImageUrl;
        existing.Weight = product.Weight;
        existing.IsActive = product.IsActive;

        _db.SaveChanges();

        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var user = AuthHelper.GetUser(Request, _db);
        if (user == null || user.Role != "Admin")
            return Unauthorized();

        var product = _db.Products.Find(id);
        if (product == null) return NotFound();

        _db.Products.Remove(product);
        _db.SaveChanges();

        return Ok();
    }
}
