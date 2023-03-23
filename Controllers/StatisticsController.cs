using Dapper;
using FinanceAPI.Helpers;
using FinanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Globalization;

namespace FinanceAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly MySqlConnection _connection;
        private readonly DbHelper _dbHelper;
        private readonly TimeHelper _timeHelper;
        private readonly FilterHelper _filterHelper;
        private readonly List<Category> Categories;
        private readonly List<CategoryDto> CategoriesDto;
        private readonly List<Entry> Entries;
        private readonly List<EntryDto> EntriesDto;

        public StatisticsController(MySqlConnection connection, DbHelper dbHelper, TimeHelper timeHelper, FilterHelper filterHelper)
        {
            _connection = connection;
            _dbHelper = dbHelper;
            _timeHelper = timeHelper;
            _filterHelper = filterHelper;

            _connection.Open();
            Categories = _connection.Query<Category>("SELECT * FROM category").ToList();
            CategoriesDto = _connection.Query<CategoryDto>("SELECT * FROM category").ToList();
            Entries = _connection.Query<Entry>("SELECT * FROM entry").ToList();
            EntriesDto = _connection.Query<EntryDto>("SELECT * FROM entry").ToList();
            _connection.Close();
        }

        [HttpGet("TotalByParams/{category?}/{startTime?}/{endTime?}")]
        public ActionResult<TotalDto> GetTotalByCategory(string category = "All", string startTime = "2010-01-01 00:00:00", string endTime = "3000-01-01 00:00:00")
        {
            int total = 0;
            foreach (var entry in Entries)
            {
                if (category == "All" || entry.category_ID == _dbHelper.GetCategoryId(category))
                {
                    if (_timeHelper.isDateIn(startTime, endTime, entry))
                    {
                        total += entry.amount;
                    }
                }
            }
            return Ok(new TotalDto { total = total });
        }
        // average per day, per week for a certain category

        // 
        [HttpGet("CategoryStats/{category}/{startDate}/{endDate}")]
        public async Task<ActionResult<IEnumerable<CategoryExtraDto>>> CategoryStats(string category, string startDate, string endDate)
        {
            var categoriesExtra = await Task.Run(async () =>
            {
                _connection.Open();
                string? userId = HttpContext.Session.GetString("ID");
                if (userId != null)
                {
                    int id = int.Parse(userId);
                    Filter filter = new Filter { userid=userId, catName = category, startDate = startDate, endDate = endDate };
                    var categories = await _dbHelper.GetUserCategories(userId);
                    var entries = await _dbHelper.GetUserEntries(filter);
                    List<CategoryExtraDto> categoriesExtra = new List<CategoryExtraDto>();
                    foreach (CategoryDto category in categories)
                    {
                        categoriesExtra.Add(new CategoryExtraDto { name = category.name, numTransactions = 0, percentage = 0, total = 0 });
                    }
                    double total = 0;
                    foreach (EntryDto entry in entries)
                    {
                        total += entry.amount;
                        CategoryExtraDto catEx = categoriesExtra.FirstOrDefault(e => e.name == entry.category);
                        if (catEx != null)
                        {
                            catEx.total += entry.amount;
                            catEx.numTransactions++;
                        }
                    }

                    foreach (CategoryExtraDto catEx in categoriesExtra)
                    {
                        catEx.percentage = Math.Round((double)(catEx.total / total) * 100);
                    }
                    _connection.Close();
                    return categoriesExtra;
                }
                else
                {
                    return new List<CategoryExtraDto>();
                }

            });
            return Ok(categoriesExtra);
        }

        // route returns a monthly representation of all the entries
        [HttpGet("MonthlyStats/{category}/{startDate}/{endDate}")]
        public async Task<ActionResult<IEnumerable<MonthReportDto>>> MonthlyStats(string category, string startDate, string endDate)
        {
            _connection.Open();
            var result = await Task.Run(async () =>
            {
                List<MonthReportDto> months = new List<MonthReportDto>();
                string? userid = HttpContext.Session.GetString("ID");
                Filter filter = new Filter { userid=userid, catName = category, startDate = startDate, endDate =endDate};
                var entries = await _dbHelper.GetUserEntries(filter);
                DateTime first = DateTime.MaxValue;
                DateTime last = DateTime.MinValue;
                foreach (var entry in entries)
                {
                    DateTime date = _timeHelper.fromReadable(entry.time);
                    if (DateTime.Compare(date, first) < 0) first = date;
                    if (DateTime.Compare(date, last) > 0) last = date;
                    string month = _timeHelper.GenerateMonthRepresentation(date);
                    var current = months.FirstOrDefault(e => e.month == month);
                    if (current == null)
                    {
                        months.Add(new MonthReportDto { month = month, amount = entry.amount });
                    }
                    else
                    {
                        current.amount += entry.amount;
                    }
                }

                while (DateTime.Compare(first, last) < 0)
                {
                    string month = _timeHelper.GenerateMonthRepresentation(first);
                    var current = months.FirstOrDefault(e => e.month == month);
                    if (current == null)
                    {
                        months.Add(new MonthReportDto { month = month, amount = 0 });
                    }
                    first = first.AddMonths(1);
                }

                months.Sort((x, y) =>
                {
                    string[] monthx = x.month.Split('-');
                    string[] monthy = y.month.Split('-');
                    int xyear = int.Parse(monthx[1]);
                    int yyear = int.Parse(monthy[1]);
                    int xmon = int.Parse(monthx[0]);
                    int ymon = int.Parse(monthy[0]);
                    if (xyear == yyear)
                    {
                        return xmon - ymon;
                    }
                    else return xyear - yyear;
                });

                foreach (MonthReportDto month in months)
                {
                    month.month = _timeHelper.GenerateReadableMonthRepresentation(month.month);
                }

                return months;
            });

            return Ok(result);
        }
    }

}