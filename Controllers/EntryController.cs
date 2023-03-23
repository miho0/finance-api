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

        [HttpGet("Get/{catName}/{startTime}/{endTime}")]
        public async Task<ActionResult<IEnumerable<EntryDto>>> GetEntriesFiltered(string catName, string startTime, string endTime)
        {
            var result = await Task.Run(() =>
            {
                string? userId = HttpContext.Session.GetString("ID");
                Filter filter = new Filter{userid=userId, catName=catName, startDate=startTime, endDate=endTime};  
                return _dbHelper.GetUserEntries(filter);
            });
            return Ok(result);
        }

        [HttpPost("Insert")]
        public async Task<ActionResult> InsertEntry(EntryDto entryDto)
        {
            await Task.Run(() =>
            {
                _connection.Open();
                int id = _dbHelper.GetCategoryId(entryDto.category);
                string? userid = HttpContext.Session.GetString("ID");
                if (userid != null)
                {
                    _connection.Query("INSERT INTO entry (category_ID, name, description, amount, time) VALUES (@id, @name, @description, @amount, @time)");
                }
                _connection.Close();
            });
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