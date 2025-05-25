using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.IO;

namespace ClothingStore.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public IActionResult ChooseRole()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChooseRole(string role)
        {
            if (role == "user")
            {
                return RedirectToAction("Login", "Account");
            }
            else if (role == "admin")
            {
                return RedirectToAction("Login", "Admin");
            }
            return View();
        }
    }
}