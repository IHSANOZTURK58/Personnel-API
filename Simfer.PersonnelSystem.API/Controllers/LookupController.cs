using Microsoft.AspNetCore.Mvc;
using Simfer.PersonnelSystem.API.Data;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class LookupController : ControllerBase
{
    private readonly AppDbContext _context;

    public LookupController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _context.Products.ToListAsync();
        return Ok(new { success = true, data = products });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        // 1. Sadece ID ve Name alanlarını seçerek JSON döngü krizini kökünden çözüyoruz.
        var categories = await _context.FaultCategories
            .Select(c => new
            {
                id = c.Id,
                name = c.Name
            })
            .ToListAsync();

        // 2. React Native'in doğrudan okuyabilmesi için kabuksuz, saf listeyi döndürüyoruz.
        return Ok(categories);
    }
}