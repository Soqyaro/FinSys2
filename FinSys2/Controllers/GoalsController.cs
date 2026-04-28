using Microsoft.AspNetCore.Mvc;
using FinSys2.Models;
using FinSys2.Services;
using System.Security.Claims;

namespace FinSys2.Controllers
{
    public class GoalsController : Controller
    {
        private readonly JsonDatabase<Goal> goalDb;
        private readonly JsonDatabase<Transaction> transDb;
        private readonly CurrencyService currencyService = new CurrencyService();//динамический курс

        public GoalsController()
        {
            goalDb = new JsonDatabase<Goal>("goals.json");
            transDb = new JsonDatabase<Transaction>("transactions.json");
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return View(new List<Goal>());

            //свободные деньги пользователя
            var userTrans = transDb.GetAll().Where(t => t.UserId == userId).ToList();
            decimal income = userTrans.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal expense = userTrans.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            ViewBag.FreeCash = income - expense;

            //курс валют
            ViewBag.Rates = await currencyService.GetExchangeRates();

            var userGoals = goalDb.GetAll().Where(g => g.UserId == userId).ToList();
            return View(userGoals);
        }

        [HttpPost]
        public IActionResult AddGoal(string title, decimal targetAmount, decimal percentage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            var goals = goalDb.GetAll();
            goals.Add(new Goal
            {
                UserId = userId,
                Title = title,
                TargetAmount = targetAmount,
                AllocatedPercentage = percentage
            });

            goalDb.SaveAll(goals);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CompleteGoal(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var goals =goalDb.GetAll();
            var goal = goals.FirstOrDefault(g => g.Id == id && g.UserId == userId);

            if (goal != null && !goal.IsCompleted)
            {
                //цель выполнена навсегда
                goal.IsCompleted = true;
                goalDb.SaveAll(goals);

                //Списывание денег со счета
                var transactions = transDb.GetAll();
                transactions.Add(new Transaction
                {
                    UserId = userId,
                    Type = "Expense",
                    Amount = goal.TargetAmount,
                    Category = "Достижение цели",
                    Comment = $"Покупка: {goal.Title}",
                    Date = DateTime.Now
                });
                transDb.SaveAll(transactions);
            }

            return RedirectToAction("Index");
        }
    }
}