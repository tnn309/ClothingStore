using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; } // Sử dụng required thay vì CS8618

        [StringLength(500)]
        public string? Description { get; set; } // Cho phép null

        [Required]
        public required decimal Price { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; } // Cho phép null

        [StringLength(50)]
        public required string Category { get; set; } = "Khác"; // Giá trị mặc định để không ảnh hưởng SeedData

    }
}
