using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ProductsDb"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Ноутбук", Price = 25000, Category = "Електроніка" },
            new Product { Name = "Мишка", Price = 500, Category = "Аксесуари" },
            new Product { Name = "Клавіатура", Price = 1500, Category = "Аксесуари" }
        );
        db.SaveChanges();
    }
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.Run();

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
}

public class ProductController : Controller
{
    private readonly AppDbContext _db;

    public ProductController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var products = await _db.Products.ToListAsync();
        return Json(products);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var html = @"
            <!DOCTYPE html>
            <html>
            <head><meta charset='utf-8'><title>Додати товар</title></head>
            <body style='font-family: Arial; margin: 30px;'>
                <h2>Додати новий товар</h2>
                <form action='/Product/Create' method='post'>
                    <label>Назва:</label><br/>
                    <input type='text' name='Name' required /><br/><br/>
                    <label>Ціна:</label><br/>
                    <input type='number' step='0.01' name='Price' required /><br/><br/>
                    <label>Категорія:</label><br/>
                    <input type='text' name='Category' required /><br/><br/>
                    <button type='submit'>Зберегти</button>
                </form>
            </body>
            </html>";

        return Content(html, "text/html");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return Json(product);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Json(new List<Product>());
        }

        var results = await _db.Products
            .Where(p => p.Name.Contains(keyword) || p.Category.Contains(keyword))
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { Message = "Товар не знайдено." });
        }

        return Json(product);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { Message = "Товар не знайдено." });
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return Json(product);
    }
}