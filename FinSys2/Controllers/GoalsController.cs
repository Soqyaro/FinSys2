using Microsoft.AspNetCore.Mvc;
using FinSys2.Models;
using FinSys2.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting; // Обязательно

namespace FinSys2.Controllers
{
    public class GoalsController : Controller
    {
        private readonly JsonDatabase<Goal> _goalDb;
        private readonly JsonDatabase<Transaction> _transDb;
        private readonly CurrencyService _currencyService; //DI для CurrencyService тоже стоит сделать

        //DI для получения IWebHostEnvironment
        public GoalsController(IWebHostEnvironment appEnvironment, CurrencyService currencyService)
        {
            _goalDb = new JsonDatabase<Goal>("goals.json", appEnvironment);
            _transDb = new JsonDatabase<Transaction>("transactions.json", appEnvironment);
            _currencyService = currencyService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return View(new List<Goal>());

            var userTrans = _transDb.GetAll().Where(t => t.UserId == userId).ToList();
            decimal income = userTrans.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal expense = userTrans.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            ViewBag.FreeCash = income - expense;
            ViewBag.Rates = await _currencyService.GetExchangeRates();

            var userGoals = _goalDb.GetAll().Where(g => g.UserId == userId).ToList();
            return View(userGoals);
        }

        [HttpPost]
        public IActionResult AddGoal(string title, decimal targetAmount, decimal percentage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            var goals = _goalDb.GetAll();
            goals.Add(new Goal
            {
                UserId = userId,
                Title = title,
                TargetAmount = targetAmount,
                AllocatedPercentage = percentage
            });

            _goalDb.SaveAll(goals);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CompleteGoal(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var goals = _goalDb.GetAll();
            var goal = goals.FirstOrDefault(g => g.Id == id && g.UserId == userId);

            if (goal != null && !goal.IsCompleted)
            {
                goal.IsCompleted = true;
                _goalDb.SaveAll(goals);

                var transactions = _transDb.GetAll();
                transactions.Add(new Transaction
                {
                    UserId = userId,
                    Type = "Expense",
                    Amount = goal.TargetAmount,
                    Category = "Достижение цели",
                    Comment = $"Покупка: {goal.Title}",
                    Date = DateTime.Now
                });
                _transDb.SaveAll(transactions);
            }

            return RedirectToAction("Index");
        }
    }
}