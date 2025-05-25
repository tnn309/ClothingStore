using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClothingStore.Data;
using Microsoft.AspNetCore.Authorization;
using ClothingStore.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClothingStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<CheckoutController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize]
        public IActionResult Index()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            decimal total = cart.Sum(item => (item?.Price ?? 0m) * (item?.Quantity ?? 0));
            ViewBag.Total = total;
            _logger.LogInformation($"Cart loaded in Index: {JsonSerializer.Serialize(cart)}");
            return View(cart);
        }

        [Authorize]
        public IActionResult Address()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson) || (JsonSerializer.Deserialize<List<CartItem>>(cartJson)?.Count ?? 0) == 0)
            {
                return RedirectToAction("ViewCart", "Products");
            }
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult Address(string fullName, string address, string phone)
        {
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(address) || string.IsNullOrEmpty(phone))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tin!";
                return View();
            }

            var shippingInfo = new ShippingInfo { FullName = fullName, Address = address, Phone = phone };
            HttpContext.Session.SetString("ShippingInfo", JsonSerializer.Serialize(shippingInfo));
            _logger.LogInformation($"Shipping info saved: {JsonSerializer.Serialize(shippingInfo)}");
            return RedirectToAction("Payment");
        }

        [Authorize]
        public IActionResult Payment()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            if (cart.Count == 0)
            {
                return RedirectToAction("ViewCart", "Products");
            }

            // Lọc các CartItem hợp lệ
            var validCart = cart.Where(item =>
                item != null &&
                item.Price.HasValue &&
                item.Quantity.HasValue &&
                item.ProductId > 0).ToList();

            if (validCart.Count == 0)
            {
                return RedirectToAction("ViewCart", "Products");
            }

            decimal total = validCart.Sum(item => (item.Price ?? 0m) * (item.Quantity ?? 0));
            ViewBag.Cart = validCart;
            ViewBag.Total = total;

            var shippingInfoJson = HttpContext.Session.GetString("ShippingInfo");
            var shippingInfo = string.IsNullOrEmpty(shippingInfoJson)
                ? new ShippingInfo()
                : JsonSerializer.Deserialize<ShippingInfo>(shippingInfoJson) ?? new ShippingInfo();

            ViewBag.FullName = shippingInfo.FullName ?? "";
            ViewBag.Address = shippingInfo.Address ?? "";
            ViewBag.Phone = shippingInfo.Phone ?? "";

            _logger.LogInformation($"Payment page loaded - Valid Cart: {JsonSerializer.Serialize(validCart)}, ShippingInfo: {JsonSerializer.Serialize(shippingInfo)}");
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Payment(string paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod))
            {
                _logger.LogWarning("Payment method is empty.");
                ViewBag.Error = "Vui lòng chọn phương thức thanh toán!";
                return View();
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                _logger.LogWarning("Cart JSON is empty in session.");
                return RedirectToAction("ViewCart", "Products");
            }

            List<CartItem> cart;
            try
            {
                cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing cart JSON.");
                return RedirectToAction("ViewCart", "Products");
            }

            if (cart.Count == 0)
            {
                _logger.LogWarning("Cart is empty during payment.");
                return RedirectToAction("ViewCart", "Products");
            }

            // Lọc các CartItem hợp lệ
            var validCart = cart.Where(item =>
                item != null &&
                item.Price.HasValue &&
                item.Quantity.HasValue &&
                item.ProductId > 0).ToList();

            if (validCart.Count == 0)
            {
                ViewBag.Error = "Không có sản phẩm hợp lệ trong giỏ hàng!";
                _logger.LogWarning("No valid items in cart.");
                return View();
            }

            var shippingInfoJson = HttpContext.Session.GetString("ShippingInfo");
            if (string.IsNullOrEmpty(shippingInfoJson))
            {
                _logger.LogWarning("ShippingInfo JSON is empty in session.");
                return RedirectToAction("Address");
            }

            ShippingInfo shippingInfo;
            try
            {
                shippingInfo = JsonSerializer.Deserialize<ShippingInfo>(shippingInfoJson) ?? new ShippingInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing shippingInfo JSON.");
                return RedirectToAction("Address");
            }

            if (string.IsNullOrEmpty(shippingInfo.FullName) || string.IsNullOrEmpty(shippingInfo.Address) || string.IsNullOrEmpty(shippingInfo.Phone))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tin giao hàng!";
                _logger.LogWarning("Incomplete shipping info.");
                return View();
            }

            decimal total = validCart.Sum(item => (item.Price ?? 0m) * (item.Quantity ?? 0));
            _logger.LogInformation($"Total calculated: {total}");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found during checkout.");
                return RedirectToAction("Login", "Account");
            }

            if (paymentMethod == "COD")
            {
                return await ProcessCODPayment(validCart, shippingInfo, total, user);
            }
            else if (paymentMethod == "VNPay")
            {
                // Lưu thông tin tạm thời cho VNPay
                var orderData = new
                {
                    Cart = validCart,
                    ShippingInfo = shippingInfo,
                    Total = total,
                    UserId = user.Id,
                    UserName = user.UserName
                };
                HttpContext.Session.SetString("VNPayOrderData", JsonSerializer.Serialize(orderData));

                return RedirectToAction("PaymentVNPay", "VNPay");
            }

            ViewBag.Error = "Phương thức thanh toán không hợp lệ!";
            return View();
        }

        private async Task<IActionResult> ProcessCODPayment(List<CartItem> validCart, ShippingInfo shippingInfo, decimal total, IdentityUser user)
        {
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Total = total,
                FullName = shippingInfo.FullName,
                Address = shippingInfo.Address,
                Phone = shippingInfo.Phone,
                PaymentMethod = "COD",
                Items = new List<OrderItem>()
            };

            _logger.LogInformation($"Processing COD order for UserId: {user.Id}, Total: {total}");

            foreach (var cartItem in validCart)
            {
                try
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product == null)
                    {
                        _logger.LogWarning($"Product with ID {cartItem.ProductId} not found, skipping.");
                        continue;
                    }

                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Name = cartItem.Name ?? product.Name,
                        Price = cartItem.Price ?? product.Price,
                        Quantity = cartItem.Quantity ?? 1
                    };
                    order.Items.Add(orderItem);
                    _logger.LogInformation($"Added order item: ProductId {cartItem.ProductId}, Name: {orderItem.Name}, Price: {orderItem.Price}, Quantity: {orderItem.Quantity}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing cart item with ProductId: {cartItem.ProductId}");
                    continue;
                }
            }

            if (!order.Items.Any())
            {
                ViewBag.Error = "Không có sản phẩm hợp lệ để đặt hàng!";
                _logger.LogWarning("No valid items in order after processing.");
                return View();
            }

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Order saved successfully, OrderId: {order.Id}");

                var userOrderStat = new UserOrderStats
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? "Unknown",
                    OrderId = order.Id,
                    OrderTotal = total,
                    OrderDate = DateTime.Now
                };
                _context.UserOrderStats.Add(userOrderStat);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"UserOrderStats saved for OrderId: {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order or UserOrderStats.");
                ViewBag.Error = "Đã xảy ra lỗi khi lưu đơn hàng. Vui lòng thử lại!";
                return View();
            }

            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("ShippingInfo");
            TempData["Message"] = "Đặt hàng thành công bằng COD!";
            return RedirectToAction("Index", "Home");
        }
    }
}
