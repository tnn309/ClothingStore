using ClothingStore.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Ensure the database is created
                context.Database.EnsureCreated();

                // Kiểm tra xem đã có sản phẩm nào chưa
                if (context.Products.Any())
                {
                    return; // Database đã được seed
                }

                // Thêm dữ liệu mới với giá VNĐ và Category
                var products = new List<Product>
                {
                    new Product { Name = "Áo khoác da", Description = "Áo khoác da cao cấp", Price = 150000m, ImageUrl = "/images/Áo khoác da.jpg", Category = "áo" },
                    new Product { Name = "Áo khoác jeans", Description = "Áo khoác jeans năng động", Price = 200000m, ImageUrl = "/images/Áo khoác jeans.jpg", Category = "áo" },
                    new Product { Name = "Áo khoác mùa đông", Description = "Áo khoác ấm áp mùa đông", Price = 180000m, ImageUrl = "/images/Áo khoác mùa đông.jpg", Category = "áo" },
                    new Product { Name = "Áo len đỏ", Description = "Áo len đỏ nổi bật", Price = 450000m, ImageUrl = "/images/Áo len đỏ.jpg", Category = "áo" },
                    new Product { Name = "Áo len xám", Description = "Áo len xám ấm áp", Price = 500000m, ImageUrl = "/images/Áo len xám.jpg", Category = "áo" },
                    new Product { Name = "Áo len xanh", Description = "Áo len xanh thanh lịch", Price = 550000m, ImageUrl = "/images/Áo len xanh.jpg", Category = "áo" },
                    new Product { Name = "Áo polo", Description = "Áo thun polo lịch sự, phong cách", Price = 1200000m, ImageUrl = "/images/Áo polo.jpg", Category = "áo" },
                    new Product { Name = "Áo sơ mi jeans", Description = "Áo sơ mi jeans phong cách", Price = 800000m, ImageUrl = "/images/Áo sơ mi jeans.jpg", Category = "áo" },
                    new Product { Name = "Áo sơ mi kẻ", Description = "Áo sơ mi kẻ casual", Price = 1500000m, ImageUrl = "/images/Áo sơ mi kẻ.jpg", Category = "áo" },
                    new Product { Name = "Áo sơ mi trắng", Description = "Áo sơ mi trắng công sở", Price = 750000m, ImageUrl = "/images/Áo sơ mi trắng.jpg", Category = "áo" },
                    new Product { Name = "Áo thun đen", Description = "Áo thun cotton thoáng mát, thoải mái", Price = 900000m, ImageUrl = "/images/Áo thun đen.jpg", Category = "áo" },
                    new Product { Name = "Áo thun graphic", Description = "Áo thun in hình độc đáo", Price = 1100000m, ImageUrl = "/images/Áo thun graphic.jpg", Category = "áo" },
                    new Product { Name = "Giày chạy bộ", Description = "Giày chạy bộ chuyên dụng", Price = 120000m, ImageUrl = "/images/Giày chạy bộ.jpg", Category = "giày" },
                    new Product { Name = "Giày da nâu", Description = "Giày da nâu lịch sự", Price = 80000m, ImageUrl = "/images/Giày da nâu.jpg", Category = "giày" },
                    new Product { Name = "Giày sneaker trắng", Description = "Giày sneaker trắng thể thao", Price = 150000m, ImageUrl = "/images/Giày sneaker trắng.jpg", Category = "giày" },
                    new Product { Name = "Mũ len", Description = "Mũ len ấm áp", Price = 300000m, ImageUrl = "/images/Mũ len.jpg", Category = "phụ kiện" },
                    new Product { Name = "Mũ lưỡi trai", Description = "Mũ lưỡi trai thể thao", Price = 280000m, ImageUrl = "/images/Mũ lưỡi trai.jpg", Category = "phụ kiện" },
                    new Product { Name = "Mũ rộng vành", Description = "Mũ rộng vành chống nắng", Price = 350000m, ImageUrl = "/images/Mũ rộng vành.jpg", Category = "phụ kiện" },
                    new Product { Name = "Quần cargo", Description = "Quần cargo nhiều túi", Price = 400000m, ImageUrl = "/images/Quần cargo.jpg", Category = "quần" },
                    new Product { Name = "Quần kaki", Description = "Quần kaki thoải mái", Price = 380000m, ImageUrl = "/images/Quần kaki.jpg", Category = "quần" },
                    new Product { Name = "Quần rách gối", Description = "Quần jeans rách phong cách", Price = 420000m, ImageUrl = "/images/Quần rách.jpg", Category = "quần" },
                    new Product { Name = "Quần skinny", Description = "Quần jeans ôm sát, thời trang", Price = 320000m, ImageUrl = "/images/Quần skinny.jpg", Category = "quần" },
                    new Product { Name = "Quần slim fit", Description = "Quần jeans ôm vừa vặn", Price = 450000m, ImageUrl = "/images/Quần slim fit.jpg", Category = "quần" },
                    new Product { Name = "Quần tây", Description = "Quần tây đen lịch sự", Price = 380000m, ImageUrl = "/images/Quần tây.jpg", Category = "quần" },
                    new Product { Name = "Tất đen", Description = "Tất đen cơ bản", Price = 25000m, ImageUrl = "/images/Tất đen.jpg", Category = "phụ kiện" },
                    new Product { Name = "Tất họa tiết", Description = "Tất có họa tiết đẹp", Price = 35000m, ImageUrl = "/images/Tất họa tiết.jpg", Category = "phụ kiện" },
                    new Product { Name = "Tất trắng", Description = "Tất trắng cotton", Price = 25000m, ImageUrl = "/images/Tất trắng.jpg", Category = "phụ kiện" },
                    new Product { Name = "Thắt lưng cao cấp", Description = "Thắt lưng cao cấp nhất", Price = 2000000m, ImageUrl = "/images/Thắt lưng cao cấp.jpg", Category = "phụ kiện" },
                    new Product { Name = "Thắt lưng công sở", Description = "Thắt lưng công sở sang trọng", Price = 250000m, ImageUrl = "/images/Thắt lưng công sở.jpg", Category = "phụ kiện" },
                    new Product { Name = "Thắt lưng da", Description = "Thắt lưng da cao cấp", Price = 200000m, ImageUrl = "/images/Thắt lưng da.jpg", Category = "phụ kiện" },
                    new Product { Name = "Thắt lưng vải", Description = "Thắt lưng vải casual", Price = 120000m, ImageUrl = "/images/Thắt lưng vải.jpg", Category = "phụ kiện" },


                };

                context.Products.AddRange(products);
                context.SaveChanges();
            }
        }

        public static void ForceReseed(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Xóa tất cả sản phẩm
                if (context.Products.Any())
                {
                    context.Products.RemoveRange(context.Products);
                    context.SaveChanges();
                }

                // Gọi lại Initialize
                Initialize(serviceProvider);
            }
        }
    }
}
