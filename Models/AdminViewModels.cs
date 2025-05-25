using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Models
{
    // ViewModels cho Admin
    public class UserStatsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime RegisterDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
    }

    public class UserDetailsViewModel
    {
        public IdentityUser User { get; set; } = new IdentityUser();
        public List<string> UserRoles { get; set; } = new List<string>();
        public List<Order> Orders { get; set; } = new List<Order>();
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public bool IsAdmin { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        public IdentityUser User { get; set; } = new IdentityUser();
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public List<Order> Orders { get; set; } = new List<Order>();
    }

    public class EditProfileViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
