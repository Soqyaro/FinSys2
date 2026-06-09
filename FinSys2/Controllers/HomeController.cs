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

        // Добавляем параметры в метод (они могут быть null, если фильтр не выбран)
        public IActionResult Index(int? month, int? year)
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

            var userTransactions = _transactionDb.GetAll()
                .Where(t => t.UserId == userId)
                .ToList();

            // 1. ОБЩИЕ ПОКАЗАТЕЛИ И ЛИНЕЙНЫЙ ГРАФИК (за всё время)
            decimal income = userTransactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal expense = userTransactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            ViewBag.TotalIncome = income;
            ViewBag.TotalExpense = expense;
            ViewBag.Balance = income - expense;

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

            // 2. ФИЛЬТРАЦИЯ ТОЛЬКО ДЛЯ КРУГОВЫХ ДИАГРАММ
            var pieChartData = userTransactions.AsEnumerable();

            if (month.HasValue && month.Value > 0)
                pieChartData = pieChartData.Where(t => t.Date.Month == month.Value);

            if (year.HasValue && year.Value > 0)
                pieChartData = pieChartData.Where(t => t.Date.Year == year.Value);

            // Расходы (с учетом фильтра)
            var expenseData = pieChartData
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(t => t.Amount) })
                .ToList();

            // Доходы (с учетом фильтра)
            var incomeData = pieChartData
                .Where(t => t.Type == "Income")
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(t => t.Amount) })
                .ToList();

            ViewBag.ExpenseLabels = expenseData.Select(x => x.Category).ToList();
            ViewBag.ExpenseValues = expenseData.Select(x => x.Amount).ToList();
            ViewBag.IncomeLabels = incomeData.Select(x => x.Category).ToList();
            ViewBag.IncomeValues = incomeData.Select(x => x.Amount).ToList();

            // Сохраняем выбранные значения в ViewBag, чтобы селекты не сбрасывались
            ViewBag.SelectedMonth = month ?? 0;
            ViewBag.SelectedYear = year ?? 0;

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