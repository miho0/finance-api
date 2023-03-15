using FinanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data.Common;
using Dapper;
using System.Linq;
using FinanceAPI.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;

namespace FinanceAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EntryController : ControllerBase
    {
        private readonly MySqlConnection _connection;
        private readonly DbHelper _dbHelper;
        private readonly TimeHelper _timeHelper;
        private readonly FilterHelper _filterHelper;

        public EntryController(MySqlConnection connection, DbHelper dbHelper, TimeHelper timeHelper, FilterHelper filterHelper)
        {
            _connection = connection;
            _dbHelper = dbHelper;
            _timeHelper = timeHelper;
            _filterHelper = filterHelper;
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<EntryDto>>> GetEntries()
        {
            return await Task.Run(() =>
            {
                _connection.Open();
                var entries = _connection.Query<Entry>("SELECT * FROM entry").ToList();
                List<EntryDto> result = new List<EntryDto>();
                foreach (var entry in entries)
                {
                    result.Add(new EntryDto { category = _dbHelper.getCategoryName(entry.category_ID), name = entry.name, amount = entry.amount, description = entry.description, time = _timeHelper.formatForDisplay(entry.time) });
                }
                _connection.Close();
                return Ok(result);
            });
        }

        [HttpGet("GetFiltered/{startTime?}/{endTime?}")]
        public async Task<ActionResult<IEnumerable<EntryDto>>> GetEntriesFiltered(string startTime = "2010-01-01 00:00:00", string endTime = "3000-01-01 00:00:00")
        {
            return await Task.Run(() =>
            {
                _connection.Open();
                List<EntryDto> result = new List<EntryDto>();
                result = _filterHelper.FilterTransactions(_connection, startTime, endTime);
                _connection.Close();
                return Ok(result);
            });
        }

        [HttpGet("GetByCategory")]
        public async Task<ActionResult<IEnumerable<EntryDto>>> GetByCategory(string catName)
        {
            _connection.Open();
            var entries = await Task.Run(() =>
            {
                return _filterHelper.FilterByCategory(_connection, catName);
            });
            _connection.Close();
            return Ok(entries);
        }


        [HttpGet("GetByMonth")]
        public async Task<ActionResult<IEnumerable<MonthReportDto>>> GetByMonth(string catName)
        {
            _connection.Open();
            var entries = await Task.Run(() =>
            {
                List<MonthReportDto> months = new List<MonthReportDto>();
                var catEntries = _filterHelper.FilterByCategory(_connection, catName);
                DateTime first = DateTime.MaxValue;
                DateTime last = DateTime.MinValue;
                foreach (var entry in catEntries)
                {
                    DateTime date = _timeHelper.fromReadable(entry.time);
                    if (DateTime.Compare(date, first) < 0) first = date;
                    if (DateTime.Compare(date, last) > 0) last = date;
                    string month = _timeHelper.GenerateMonthRepresentation(date);
                    var current = months.FirstOrDefault(e => e.month == month);
                    if (current == null)
                    {
                        months.Add(new MonthReportDto { month = month, amount = entry.amount });
                    } else
                    {
                        current.amount += entry.amount;
                    }
                }

                while(DateTime.Compare(first, last) < 0)
                {
                    string month = _timeHelper.GenerateMonthRepresentation(first);
                    var current = months.FirstOrDefault(e => e.month == month);
                    if (current == null)
                    {
                        months.Add(new MonthReportDto { month=month, amount=0});
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

            return Ok(entries);
        }


        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<EntryDto>> GetEntryById(int id)
        {
            _connection.Open();
            var entry = await Task.Run(() => _connection.Query<Entry>($"SELECT * FROM entry where ID={id}").ToList());
            EntryDto result = new EntryDto { category = _dbHelper.getCategoryName(entry[0].category_ID), name = entry[0].name, amount = entry[0].amount, description = entry[0].description, time = _timeHelper.formatForDisplay(entry[0].time) };
            _connection.Close();
            if (entry == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost("Insert")]
        public async Task<ActionResult> InsertEntry(EntryDto entryDto)
        {
            _connection.Open();
            int id = _dbHelper.GetCategoryId(entryDto.category);
            string sqlQuery = $"INSERT INTO entry (category_ID, name, description, amount, time) VALUES ({id}, @name, @description, @amount, @time)";
            await Task.Run(() => _connection.Execute(sqlQuery, entryDto));
            _connection.Close();
            return Ok();
        }

        [HttpPatch("Update/{id}")]
        public async Task<ActionResult> UpdateEntry(EntryDto entryDto, int id)
        {
            _connection.Open();
            int catId = _dbHelper.GetCategoryId(entryDto.category);
            string sqlQuery = $"UPDATE entry SET category_id = {catId}, name = @name, description = @description, amount = @amount WHERE ID = {id}";
            await Task.Run(() => _connection.Execute(sqlQuery, entryDto));
            _connection.Close();
            return Ok();
        }

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> DeleteEntry(int id)
        {
            _connection.Open();
            string sqlQuery = $"DELETE FROM entry WHERE ID = {id}";
            await Task.Run(() => _connection.Execute(sqlQuery));
            _connection.Close();
            return Ok();
        }
    }
}
//[
//  {
//    "month": "10-22",
//    "amount": 1000
//  },
//  {
//    "month": "2-22",
//    "amount": 1000
//  },
//  {
//    "month": "9-22",
//    "amount": 1000
//  },
//  {
//    "month": "8-22",
//    "amount": 1000
//  },
//  {
//    "month": "2-23",
//    "amount": 10
//  }
//]

//[
//  {
//    "month": "2-22",
//    "amount": 1000
//  },
//  {
//    "month": "10-22",
//    "amount": 1000
//  },
//  {
//    "month": "9-22",
//    "amount": 1000
//  },
//  {
//    "month": "8-22",
//    "amount": 1000
//  },
//  {
//    "month": "2-23",
//    "amount": 10
//  }
//]