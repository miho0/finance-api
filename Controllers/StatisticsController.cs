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
        private readonly List<Category> Categories;
        private readonly List<Entry> Entries;

        public StatisticsController(MySqlConnection connection, DbHelper dbHelper, TimeHelper timeHelper)
        {
            _connection = connection;
            _dbHelper = dbHelper;
            _timeHelper = timeHelper;

            _connection.Open();
            Categories = _connection.Query<Category>("SELECT * FROM category").ToList();
            Entries = _connection.Query<Entry>("SELECT * FROM entry").ToList();
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
    }
}