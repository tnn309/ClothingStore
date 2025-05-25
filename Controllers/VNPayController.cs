using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ClothingStore.Models;
using ClothingStore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace ClothingStore.Controllers
{
    [Authorize]
    public class VNPayController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<VNPayController> _logger;

        public VNPayController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<VNPayController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult PaymentVNPay()
        {
            var orderDataJson = HttpContext.Session.GetString("VNPayOrderData");
            if (string.IsNullOrEmpty(orderDataJson))
            {
                _logger.LogWarning("VNPayOrderData is empty in session.");
                return RedirectToAction("ViewCart", "Products");
            }

            try
            {
                var orderData = JsonSerializer.Deserialize<VNPayOrderData>(orderDataJson);
                if (orderData == null || orderData.Cart == null || !orderData.Cart.Any())
                {
                    return RedirectToAction("ViewCart", "Products");
                }

                // Tạo transaction reference giả lập
                var txnRef = DateTime.Now.Ticks.ToString();
                HttpContext.Session.SetString("VNPayTxnRef", txnRef);

                // Tạo ViewModel cho trang giả lập
                var viewModel = new VNPayPaymentViewModel
                {
                    TxnRef = txnRef,
                    Amount = orderData.Total,
                    OrderInfo = $"Thanh toán đơn hàng {txnRef}",
                    Cart = orderData.Cart,
                    ShippingInfo = orderData.ShippingInfo
                };

                _logger.LogInformation($"VNPay simulation page created for TxnRef: {txnRef}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay simulation page.");
                TempData["Error"] = "Có lỗi xảy ra khi tạo thanh toán VNPay!";
                return RedirectToAction("Payment", "Checkout");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(string cardNumber, string cardHolder, string expiryDate, string cvv, bool paymentSuccess = true)
        {
            var txnRef = HttpContext.Session.GetString("VNPayTxnRef");
            if (string.IsNullOrEmpty(txnRef))
            {
                TempData["Error"] = "Phiên giao dịch không hợp lệ!";
                return RedirectToAction("Index", "Home");
            }

            // Giả lập validation thông tin thẻ
            if (string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(cardHolder) ||
                string.IsNullOrEmpty(expiryDate) || string.IsNullOrEmpty(cvv))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin thẻ!";
                return RedirectToAction("PaymentVNPay");
            }

            // Giả lập kết quả thanh toán (có thể thành công hoặc thất bại)
            if (paymentSuccess)
            {
                var result = await ProcessSuccessfulPayment(txnRef);
                if (result)
                {
                    TempData["Message"] = "Thanh toán VNPay thành công! Đơn hàng đã được đặt.";
                }
                else
                {
                    TempData["Error"] = "Thanh toán thành công nhưng có lỗi khi lưu đơn hàng!";
                }
            }
            else
            {
                _logger.LogWarning($"VNPay payment simulation failed for TxnRef: {txnRef}");
                TempData["Error"] = "Thanh toán VNPay thất bại! Vui lòng thử lại.";
            }

            // Cleanup session
            HttpContext.Session.Remove("VNPayOrderData");
            HttpContext.Session.Remove("VNPayTxnRef");

            return RedirectToAction("Index", "Home");
        }

        private async Task<bool> ProcessSuccessfulPayment(string txnRef)
        {
            try
            {
                var orderDataJson = HttpContext.Session.GetString("VNPayOrderData");
                if (string.IsNullOrEmpty(orderDataJson))
                {
                    _logger.LogError("VNPayOrderData not found in session during payment confirmation.");
                    return false;
                }

                var orderData = JsonSerializer.Deserialize<VNPayOrderData>(orderDataJson);
                if (orderData == null)
                {
                    _logger.LogError("Failed to deserialize VNPayOrderData.");
                    return false;
                }

                var user = await _userManager.FindByIdAsync(orderData.UserId);
                if (user == null)
                {
                    _logger.LogError($"User not found with ID: {orderData.UserId}");
                    return false;
                }

                var order = new Order
                {
                    UserId = orderData.UserId,
                    OrderDate = DateTime.Now,
                    Total = orderData.Total,
                    FullName = orderData.ShippingInfo.FullName,
                    Address = orderData.ShippingInfo.Address,
                    Phone = orderData.ShippingInfo.Phone,
                    PaymentMethod = "VNPay",
                    Items = new List<OrderItem>()
                };

                _logger.LogInformation($"Processing VNPay order for UserId: {orderData.UserId}, Total: {orderData.Total}, TxnRef: {txnRef}");

                foreach (var cartItem in orderData.Cart)
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
                    _logger.LogWarning("No valid items in VNPay order after processing.");
                    return false;
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"VNPay order saved successfully, OrderId: {order.Id}");

                var userOrderStat = new UserOrderStats
                {
                    UserId = orderData.UserId,
                    UserName = orderData.UserName ?? "Unknown",
                    OrderId = order.Id,
                    OrderTotal = orderData.Total,
                    OrderDate = DateTime.Now
                };
                _context.UserOrderStats.Add(userOrderStat);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"UserOrderStats saved for VNPay OrderId: {order.Id}");

                // Clear cart and shipping info
                HttpContext.Session.Remove("Cart");
                HttpContext.Session.Remove("ShippingInfo");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing successful VNPay payment for TxnRef: {txnRef}");
                return false;
            }
        }
    }

    // Model để lưu dữ liệu VNPay
    public class VNPayOrderData
    {
        public List<CartItem> Cart { get; set; } = new List<CartItem>();
        public ShippingInfo ShippingInfo { get; set; } = new ShippingInfo();
        public decimal Total { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    // ViewModel cho trang thanh toán giả lập
    public class VNPayPaymentViewModel
    {
        public string TxnRef { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public List<CartItem> Cart { get; set; } = new List<CartItem>();
        public ShippingInfo ShippingInfo { get; set; } = new ShippingInfo();
    }
}
