using Microsoft.AspNetCore.Mvc;
using FinSys2.Models;
using FinSys2.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting; // Добавлено

namespace FinSys2.Controllers
{
    public class HomeController : Controller
    {
        private readonly JsonDatabase<Transaction> _transactionDb;

        //внедряем IWebHostEnvironment через конструктор
        public HomeController(IWebHostEnvironment appEnvironment)
        {
            _transactionDb = new JsonDatabase<Transaction>("transactions.json", appEnvironment);
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.Balance = 0;
                ViewBag.TotalIncome = 0;
                ViewBag.TotalExpense = 0;
                ViewBag.ChartLabels = new List<string>();
                ViewBag.ChartValues = new List<decimal>();
                ViewBag.ExpenseLabels = new List<string>();
                ViewBag.ExpenseValues = new List<decimal>();
                ViewBag.IncomeLabels = new List<string>();
                ViewBag.IncomeValues = new List<decimal>();
                return View(new List<Transaction>());
            }

            //получение транзакций пользователя
            var userTransactions = _transactionDb.GetAll()
                .Where(t => t.UserId == userId)
                .ToList();

            //общие показатели для верхних карточек
            decimal income = userTransactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal expense = userTransactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            ViewBag.TotalIncome = income;
            ViewBag.TotalExpense = expense;
            ViewBag.Balance = income - expense;

            //логика графика баланса
            var sortedForLine = userTransactions.OrderBy(t => t.Date).ToList();
            var labels = new List<string>();
            var values = new List<decimal>();
            decimal runningBalance = 0;

            foreach (var trans in sortedForLine)
            {
                if (trans.Type == "Income") runningBalance += trans.Amount;
                else runningBalance -= trans.Amount;

                labels.Add(trans.Date.ToString("dd.MM HH:mm"));
                values.Add(runningBalance);
            }

            ViewBag.ChartLabels = labels;
            ViewBag.ChartValues = values;

            //расходы
            var expenseData = userTransactions
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(t => t.Amount) })
                .ToList();

            //доходы
            var incomeData = userTransactions
                .Where(t => t.Type == "Income")
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(t => t.Amount) })
                .ToList();

            ViewBag.ExpenseLabels = expenseData.Select(x => x.Category).ToList();
            ViewBag.ExpenseValues = expenseData.Select(x => x.Amount).ToList();
            ViewBag.IncomeLabels = incomeData.Select(x => x.Category).ToList();
            ViewBag.IncomeValues = incomeData.Select(x => x.Amount).ToList();

            return View(userTransactions.OrderByDescending(t => t.Date).ToList());
        }

        [HttpPost]
        public IActionResult AddTransaction(string type, decimal amount, string category, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            var transactions = _transactionDb.GetAll();
            transactions.Add(new Transaction
            {
                UserId = userId,
                Type = type,
                Amount = amount,
                Category = category,
                Comment = comment ?? "",
                Date = DateTime.Now
            });

            _transactionDb.SaveAll(transactions);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ClearAllData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            var all = _transactionDb.GetAll();
            var toKeep = all.Where(t => t.UserId != userId).ToList();
            _transactionDb.SaveAll(toKeep);
            return RedirectToAction("Index");
        }
    }
}