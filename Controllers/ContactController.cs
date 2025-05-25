using Microsoft.AspNetCore.Mvc;
using ClothingStore.Data;
using ClothingStore.Models;

namespace ClothingStore.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(ContactForm contactForm)
        {
            if (ModelState.IsValid)
            {
                contactForm.SubmittedAt = DateTime.Now;
                _context.ContactForms.Add(contactForm); // Dòng 26 - Gây lỗi vì thiếu DbSet
                _context.SaveChanges();
                TempData["Message"] = "Tin nhắn của bạn đã được gửi thành công!";
                return RedirectToAction("Index");
            }
            return View(contactForm);
        }
    }
}