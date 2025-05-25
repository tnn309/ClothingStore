using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ClothingStore.Models;
using ClothingStore.Data;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace ClothingStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index(string searchName, decimal? minPrice, decimal? maxPrice, string searchType)
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
                products = products.Where(p => p.Name.Contains(searchName));

            if (minPrice.HasValue)
            {
                if(minPrice < 0) minPrice = 0;
                else
                {
                    products = products.Where(p => p.Price >= minPrice.Value);
                }
            }

            if (maxPrice.HasValue)
            {
                if (minPrice > 100000000) minPrice = 100000000;
                else
                {
                    products = products.Where(p => p.Price <= maxPrice.Value);
                }
            }

            if (!string.IsNullOrEmpty(searchType))
                products = products.Where(p => p.Category == searchType);


            var cartJson = HttpContext.Session.GetString("Cart");
            int cartCount = string.IsNullOrEmpty(cartJson) ? 0 : JsonSerializer.Deserialize<List<CartItem>>(cartJson)?.Count ?? 0;
            ViewBag.CartCount = cartCount;

            ViewData["searchName"] = searchName;
            ViewData["minPrice"] = minPrice;
            ViewData["maxPrice"] = maxPrice;
            ViewData["searchType"] = searchType;

            return View(await products.ToListAsync());
        }

        [HttpPost]
        [Authorize]
        public IActionResult AddToCart(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
            var existingItem = cart.FirstOrDefault(c => c.ProductId == id);
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
                    ProductId = product.Id,  // Thêm ProductId
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1
                });
            }

            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));

            TempData["Message"] = $"{product.Name} đã được thêm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        public IActionResult ViewCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            decimal total = cart.Sum(item => (item?.Price ?? 0m) * (item?.Quantity ?? 0));
            ViewBag.Total = total;

            return View(cart);
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int index)
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return RedirectToAction("ViewCart");
            }

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            if (index >= 0 && index < cart.Count)
            {
                cart.RemoveAt(index);
                HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
                TempData["Message"] = "Sản phẩm đã được xóa khỏi giỏ hàng!";
            }

            return RedirectToAction("ViewCart");
        }
    }
}
