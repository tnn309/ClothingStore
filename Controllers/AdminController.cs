using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ClothingStore.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using ClothingStore.Models;

namespace ClothingStore.Controllers
{
    public class AdminController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Login()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return RedirectToAction("Overview");
            }

            // Luôn tạo hoặc cập nhật admin mặc định
            var defaultAdminEmail = "hungtanne@gmail.com";
            var defaultAdminPassword = "Tann30092004$";

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var role = new IdentityRole("Admin");
                await _roleManager.CreateAsync(role);
            }

            var adminUser = await _userManager.FindByEmailAsync(defaultAdminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = defaultAdminEmail, Email = defaultAdminEmail, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(adminUser, defaultAdminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                // Cập nhật mật khẩu nếu cần
                var removeResult = await _userManager.RemovePasswordAsync(adminUser);
                if (removeResult.Succeeded)
                {
                    var addResult = await _userManager.AddPasswordAsync(adminUser, defaultAdminPassword);
                }
                // Đảm bảo vai trò "Admin" được gán
                if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
                // Đảm bảo email được xác thực
                if (!adminUser.EmailConfirmed)
                {
                    adminUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(adminUser);
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ email và mật khẩu!";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại trong hệ thống!";
                return View();
            }

            // Kiểm tra mật khẩu
            var passwordCheck = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordCheck)
            {
                ViewBag.Error = "Mật khẩu không chính xác!";
                return View();
            }

            // Kiểm tra quyền admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (!isAdmin)
            {
                ViewBag.Error = "Tài khoản này không có quyền admin!";
                return View();
            }

            // Kiểm tra tài khoản có bị khóa không
            if (await _userManager.IsLockedOutAsync(user))
            {
                ViewBag.Error = "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên!";
                return View();
            }

            // Đăng nhập thành công
            await _signInManager.SignInAsync(user, isPersistent: true);
            return RedirectToAction("Overview");
        }

        public async Task<IActionResult> Overview()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            var totalUsers = await _userManager.Users.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.SumAsync(o => o.Total);
            var totalProducts = await _context.Products.CountAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProducts = totalProducts;

            // Dữ liệu cho biểu đồ doanh thu
            var revenueData = _context.Orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.Total) })
                .OrderBy(g => g.Date)
                .ToList();
            ViewBag.RevenueLabels = JsonSerializer.Serialize(revenueData.Select(r => r.Date.ToString("dd/MM/yyyy")));
            ViewBag.RevenueValues = JsonSerializer.Serialize(revenueData.Select(r => r.Total));

            return View();
        }

        // Alias cho Overview để tương thích
        public async Task<IActionResult> Index()
        {
            return await Overview();
        }

        public async Task<IActionResult> Users()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            // Lấy tất cả users (bao gồm cả admin)
            var allUsers = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();

            // Lấy thống kê đơn hàng cho từng user
            var userStats = new List<UserStatsViewModel>();

            foreach (var user in allUsers)
            {
                // Kiểm tra role của user
                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains("Admin");

                var orders = await _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .ToListAsync();

                var totalOrders = orders.Count;
                var totalSpent = orders.Sum(o => o.Total);
                var lastOrderDate = orders.Any() ? orders.Max(o => o.OrderDate) : (DateTime?)null;

                userStats.Add(new UserStatsViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? "Unknown",
                    Email = user.Email ?? "No Email",
                    PhoneNumber = user.PhoneNumber ?? "Chưa cập nhật",
                    PasswordHash = user.PasswordHash ?? "No Password",
                    EmailConfirmed = user.EmailConfirmed,
                    IsAdmin = isAdmin,
                    RegisterDate = DateTime.Now,
                    TotalOrders = totalOrders,
                    TotalSpent = totalSpent,
                    LastOrderDate = lastOrderDate,
                    AccountStatus = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now ? "Bị khóa" : "Hoạt động"
                });
            }

            // Sắp xếp: Admin trước, sau đó theo tổng chi tiêu
            var sortedUsers = userStats
                .OrderByDescending(u => u.IsAdmin)
                .ThenByDescending(u => u.TotalSpent)
                .ThenBy(u => u.UserName)
                .ToList();

            return View(sortedUsers);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var orders = await _context.Orders
                .Where(o => o.UserId == id)
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var viewModel = new UserDetailsViewModel
            {
                User = user,
                UserRoles = roles.ToList(),
                Orders = orders,
                TotalOrders = orders.Count,
                TotalSpent = orders.Sum(o => o.Total),
                IsAdmin = roles.Contains("Admin"),
                AccountStatus = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now ? "Bị khóa" : "Hoạt động"
            };

            return View(viewModel);
        }

        // API để lấy mật khẩu hash (chỉ dành cho admin)
        [HttpGet]
        public async Task<IActionResult> GetUserPassword(string userId)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            return Json(new
            {
                success = true,
                passwordHash = user.PasswordHash,
                message = "Đây là mật khẩu đã được mã hóa (hash), không thể giải mã ngược."
            });
        }

        // Khóa/mở khóa tài khoản user
        [HttpPost]
        public async Task<IActionResult> ToggleUserLock(string userId)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            // Không cho phép khóa admin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return Json(new { success = false, message = "Không thể khóa tài khoản admin" });
            }

            var isCurrentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now;

            if (isCurrentlyLocked)
            {
                // Mở khóa
                await _userManager.SetLockoutEndDateAsync(user, null);
                return Json(new { success = true, message = "Đã mở khóa tài khoản", isLocked = false });
            }
            else
            {
                // Khóa tài khoản (khóa 100 năm)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
                return Json(new { success = true, message = "Đã khóa tài khoản", isLocked = true });
            }
        }

        public async Task<IActionResult> UserStats()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            // Lấy TẤT CẢ users từ Identity
            var allUsers = await _userManager.Users.ToListAsync();

            // Tạo danh sách thống kê cho tất cả users
            var allUserStats = new List<dynamic>();

            foreach (var user in allUsers)
            {
                // Kiểm tra role
                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains("Admin");

                // Lấy đơn hàng của user này
                var userOrders = await _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .ToListAsync();

                var orderCount = userOrders.Count;
                var totalAmount = userOrders.Sum(o => o.Total);

                allUserStats.Add(new
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? "Unknown",
                    Email = user.Email ?? "No Email",
                    OrderCount = orderCount,
                    TotalAmount = totalAmount,
                    IsAdmin = isAdmin,
                    HasOrders = orderCount > 0
                });
            }

            ViewBag.AllUserStats = allUserStats.OrderByDescending(u => ((dynamic)u).TotalAmount).ToList();

            // Tính toán thống kê tổng quan
            var totalUsers = allUserStats.Count;
            var usersWithOrders = allUserStats.Count(u => ((dynamic)u).HasOrders);
            var usersWithoutOrders = allUserStats.Count(u => !((dynamic)u).HasOrders && !((dynamic)u).IsAdmin);
            var adminUsers = allUserStats.Count(u => ((dynamic)u).IsAdmin);

            ViewBag.TotalUsers = totalUsers;
            ViewBag.UsersWithOrders = usersWithOrders;
            ViewBag.UsersWithoutOrders = usersWithoutOrders;
            ViewBag.AdminUsers = adminUsers;

            // Dữ liệu cho biểu đồ
            var chartData = allUserStats
                .Where(u => ((dynamic)u).OrderCount > 0)
                .Take(10)
                .ToList();

            ViewBag.ChartLabels = JsonSerializer.Serialize(chartData.Select(u => ((dynamic)u).UserName));
            ViewBag.ChartValues = JsonSerializer.Serialize(chartData.Select(u => ((dynamic)u).OrderCount));

            return View();
        }

        public async Task<IActionResult> ProductStats()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            var products = await _context.Products.ToListAsync();

            // Thống kê theo category
            var categoryStats = products
                .GroupBy(p => p.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(p => p.Price),
                    AveragePrice = g.Average(p => p.Price),
                    Products = g.Select(p => new {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price
                    }).ToList()
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            ViewBag.CategoryStats = categoryStats;
            ViewBag.Products = products.Take(20).ToList();

            // Dữ liệu cho biểu đồ category
            ViewBag.CategoryLabels = JsonSerializer.Serialize(categoryStats.Select(c => c.Category));
            ViewBag.CategoryValues = JsonSerializer.Serialize(categoryStats.Select(c => c.Count));

            // Thống kê sản phẩm bán chạy (từ OrderItems) - FIX
            var bestSellingProducts = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(10)
                .ToListAsync();

            var bestSellingDetails = new List<object>();
            foreach (var item in bestSellingProducts)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    bestSellingDetails.Add(new
                    {
                        ProductName = product.Name,
                        Category = product.Category,
                        TotalSold = item.TotalSold,
                        TotalRevenue = item.TotalRevenue
                    });
                }
            }

            ViewBag.BestSellingProducts = bestSellingDetails;

            return View();
        }

        public async Task<IActionResult> Products()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            var products = await _context.Products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            ViewBag.Categories = GetProductCategories();
            return View(new Product { Name = "", Price = 0m, Category = "" });
        }

        [HttpPost]
        public IActionResult AddProduct(Product product)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
                TempData["Message"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = GetProductCategories();
            return View(product);
        }

        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = GetProductCategories();
            return View(product);
        }

        [HttpPost]
        public IActionResult EditProduct(Product product)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                _context.Products.Update(product);
                _context.SaveChanges();
                TempData["Message"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = GetProductCategories();
            return View(product);
        }

        [HttpGet]
        public IActionResult DeleteProduct(int id)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        public IActionResult DeleteProductConfirmed(int id)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
                TempData["Message"] = "Xóa sản phẩm thành công!";
            }
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> ReseedData()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            try
            {
                // Cập nhật category cho các sản phẩm hiện có thay vì xóa
                var existingProducts = await _context.Products.ToListAsync();

                foreach (var product in existingProducts)
                {
                    // Gán category dựa trên tên sản phẩm
                    if (product.Name.Contains("Áo") || product.Name.Contains("Shirt") || product.Name.Contains("Len"))
                    {
                        product.Category = "Áo";
                    }
                    else if (product.Name.Contains("Quần") || product.Name.Contains("Jeans") || product.Name.Contains("Kaki") || product.Name.Contains("Tây") || product.Name.Contains("Cargo"))
                    {
                        product.Category = "Quần";
                    }
                    else if (product.Name.Contains("Giày") || product.Name.Contains("Sneaker"))
                    {
                        product.Category = "Giày";
                    }
                    else if (product.Name.Contains("Mũ") || product.Name.Contains("Tất") || product.Name.Contains("Thắt Lưng"))
                    {
                        product.Category = "Phụ kiện";
                    }
                    else
                    {
                        product.Category = "Khác";
                    }
                }

                // Chỉ thêm sản phẩm mới nếu chưa có đủ
                if (existingProducts.Count < 20)
                {
                    var newProducts = new List<Product>
                    {
                        new Product { Name = "Áo Thun Cotton Nam", Description = "Áo thun cotton thoáng mát, thoải mái", Price = 150000m, ImageUrl = "/images/be_blazer.jpg", Category = "Áo" },
                        new Product { Name = "Áo Thun Polo Nam", Description = "Áo thun polo lịch sự, phong cách", Price = 200000m, ImageUrl = "/images/be_cardigan.jpg", Category = "Áo" },
                        new Product { Name = "Quần Jeans Slim Fit", Description = "Quần jeans ôm vừa vặn", Price = 450000m, ImageUrl = "/images/be_shorts.jpg", Category = "Quần" },
                        new Product { Name = "Giày Sneaker Trắng", Description = "Giày sneaker trắng thể thao", Price = 750000m, ImageUrl = "/images/đỏ_shorts.jpg", Category = "Giày" },
                        new Product { Name = "Mũ Lưỡi Trai", Description = "Mũ lưỡi trai thể thao", Price = 120000m, ImageUrl = "/images/nam_cotton_xanh dương.jpg", Category = "Phụ kiện" }
                    };

                    // Chỉ thêm những sản phẩm chưa có
                    foreach (var newProduct in newProducts)
                    {
                        if (!existingProducts.Any(p => p.Name == newProduct.Name))
                        {
                            _context.Products.Add(newProduct);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã cập nhật category cho sản phẩm thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật dữ liệu: {ex.Message}";
            }

            return RedirectToAction("Products");
        }

        private List<string> GetProductCategories()
        {
            return new List<string> { "Áo", "Quần", "Giày", "Phụ kiện" };
        }

        public IActionResult AddAdmin()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddAdmin(string email, string password)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập email và mật khẩu!";
                return View();
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var role = new IdentityRole("Admin");
                var roleResult = await _roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                {
                    ViewBag.Error = "Không thể tạo vai trò Admin!";
                    return View();
                }
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email, Email = email };
                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    ViewBag.Error = "Không thể tạo người dùng!";
                    return View();
                }
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!roleResult.Succeeded)
                {
                    ViewBag.Error = "Không thể gán vai trò Admin!";
                    return View();
                }
            }

            ViewBag.Success = "Đã thêm admin mới thành công!";
            return View();
        }

        public IActionResult UserOrders(string userId)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Items)
                .ToList();
            ViewBag.UserName = _context.Users.FirstOrDefault(u => u.Id == userId)?.UserName ?? "Unknown";
            ViewBag.UserId = userId;
            return View(orders);
        }

        public async Task<IActionResult> Orders()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            var orders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("ChooseRole", "Auth");
        }

        [HttpPost]
        public async Task<IActionResult> AutoCategorizeProducts()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login");
            }

            try
            {
                var products = await _context.Products.Where(p => p.Category == "Khác" || string.IsNullOrEmpty(p.Category)).ToListAsync();

                foreach (var product in products)
                {
                    // Tự động phân loại dựa trên tên
                    var name = product.Name.ToLower();

                    if (name.Contains("áo") || name.Contains("shirt") || name.Contains("len") || name.Contains("blazer") || name.Contains("cardigan"))
                    {
                        product.Category = "Áo";
                    }
                    else if (name.Contains("quần") || name.Contains("jeans") || name.Contains("kaki") || name.Contains("tây") || name.Contains("cargo") || name.Contains("shorts"))
                    {
                        product.Category = "Quần";
                    }
                    else if (name.Contains("giày") || name.Contains("sneaker"))
                    {
                        product.Category = "Giày";
                    }
                    else if (name.Contains("mũ") || name.Contains("tất") || name.Contains("thắt lưng") || name.Contains("belt"))
                    {
                        product.Category = "Phụ kiện";
                    }
                    else
                    {
                        product.Category = "Khác";
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = $"Đã tự động phân loại {products.Count} sản phẩm!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi phân loại: {ex.Message}";
            }

            return RedirectToAction("Products");
        }
    }
}
