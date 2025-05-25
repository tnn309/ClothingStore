using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Models
{
    public class UserOrderStats
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public decimal OrderTotal { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }
    }
}