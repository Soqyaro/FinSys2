using Microsoft.AspNetCore.Mvc;
using FinSys2.Services;
using FinSys2.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting; //обязательно

namespace FinSys2.Controllers
{
    public class HistoryController : Controller
    {
        private readonly JsonDatabase<Transaction> _db;

        // Внедряем IWebHostEnvironment через конструктор
        public HistoryController(IWebHostEnvironment appEnvironment)
        {
            // Теперь передаем окружение в базу данных
            _db = new JsonDatabase<Transaction>("transactions.json", appEnvironment);
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return View(new List<Transaction>());

            // Используем наше приватное поле _db
            var items = _db.GetAll()
                .Where(t => t.UserId == userId)
                .OrderByDescending(x => x.Date)
                .ToList();

            return View(items);
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var allItems = _db.GetAll();

            var itemToRemove = allItems.FirstOrDefault(x => x.Id == id && x.UserId == userId);

            if (itemToRemove != null)
            {
                allItems.Remove(itemToRemove);
                _db.SaveAll(allItems);
            }

            return RedirectToAction("Index");
        }
    }
}