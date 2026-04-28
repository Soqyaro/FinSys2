using Microsoft.AspNetCore.Mvc;
using FinSys2.Services;
using FinSys2.Models;
using System.Security.Claims;

namespace FinSys2.Controllers
{
    public class HistoryController : Controller
    {
        private readonly JsonDatabase<Transaction> db = new JsonDatabase<Transaction>("transactions.json");

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return View(new List<Transaction>());

            //самые свежие операции в начало списка
            var items = db.GetAll()
                .Where(t => t.UserId == userId)
                .OrderByDescending(x => x.Date)
                .ToList();

            return View(items);
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var allItems = db.GetAll();

            //Удаляем только если ifd совпал И это запись текущего пользователя
            var itemToRemove = allItems.FirstOrDefault(x => x.Id == id && x.UserId == userId);

            if (itemToRemove != null)
            {
                allItems.Remove(itemToRemove);
                db.SaveAll(allItems);
            }

            return RedirectToAction("Index");
        }
    }
}