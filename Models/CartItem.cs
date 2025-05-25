namespace ClothingStore.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal? Price { get; set; } // Sử dụng nullable decimal
        public int? Quantity { get; set; }  // Sử dụng nullable int

    }
}