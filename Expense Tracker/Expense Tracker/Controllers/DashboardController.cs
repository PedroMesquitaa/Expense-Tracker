using Despesa_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Despesa_Tracker.Controllers
{
    public class DashboardController : Controller
    {

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            //Last 7 Days
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            //Total Receita
            int TotalReceita = SelectedTransactions
                .Where(i => i.Category.Type == "Receita")
                .Sum(j => j.Amount);
            ViewBag.TotalReceita = TotalReceita.ToString("C0");

            //Total Despesa
            int TotalDespesa = SelectedTransactions
                .Where(i => i.Category.Type == "Despesa")
                .Sum(j => j.Amount);
            ViewBag.TotalDespesa = TotalDespesa.ToString("C0");

            //Balance
            int Balance = TotalReceita - TotalDespesa;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //Doughnut Chart - Despesa By Category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Despesa")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            //Spline Chart - Receita vs Despesa

            //Receita
            List<SplineChartData> ReceitaSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Receita")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    Receita = k.Sum(l => l.Amount)
                })
                .ToList();

            //Despesa
            List<SplineChartData> DespesaSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Despesa")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    Despesa = k.Sum(l => l.Amount)
                })
                .ToList();

            //Combine Receita & Despesa
            string[] Last7Days = Enumerable.Range(0, 7)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join Receita in ReceitaSummary on day equals Receita.day into dayReceitaJoined
                                      from Receita in dayReceitaJoined.DefaultIfEmpty()
                                      join Despesa in DespesaSummary on day equals Despesa.day into DespesaJoined
                                      from Despesa in DespesaJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          Receita = Receita == null ? 0 : Receita.Receita,
                                          Despesa = Despesa == null ? 0 : Despesa.Despesa,
                                      };
            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();


            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int Receita;
        public int Despesa;

    }
}