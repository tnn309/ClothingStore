using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ClothingStore.Models;
using ClothingStore.Data;
using Microsoft.Extensions.Logging;

namespace ClothingStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index()
        {
            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"];
            }

            // Lấy danh sách sản phẩm nổi bật từ database (ví dụ: 3 sản phẩm đầu tiên)
            var featuredProducts = await _context.Products.Take(3).ToListAsync();

            ViewBag.FeaturedProducts = featuredProducts.Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price, 
                ImageUrl = p.ImageUrl
            }).ToList();

            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            decimal total = cart.Sum(item => (item?.Price ?? 0m) * (item?.Quantity ?? 0));
            ViewBag.Total = total;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, string name, decimal price)
        {
            if (productId <= 0 || string.IsNullOrEmpty(name) || price <= 0)
            {
                TempData["Message"] = "Thông tin sản phẩm không hợp lệ!";
                return RedirectToAction("Index");
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                // Tăng số lượng nếu sản phẩm đã có
                existingItem.Quantity = (existingItem.Quantity ?? 0) + 1;
            }
            else
            {
                // Thêm sản phẩm mới với ProductId
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    Name = name,
                    Price = price,
                    Quantity = 1
                });
            }

            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));

            TempData["Message"] = $"{name} đã được thêm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        public IActionResult Cart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            decimal total = cart.Sum(item => (item?.Price ?? 0m) * (item?.Quantity ?? 0));
            ViewBag.Cart = cart;
            ViewBag.Total = total;

            _logger.LogInformation($"Cart in Cart: {JsonSerializer.Serialize(cart)}, Total: {total}");

            return View();
        }
    }
}
