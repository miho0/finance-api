using Dapper;
using FinanceAPI.Helpers;
using FinanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace FinanceAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly MySqlConnection _connection;
        private readonly FilterHelper _filterHelper;
        private readonly DbHelper _dbHelper;

        public CategoryController(MySqlConnection connection, FilterHelper filterHelper, DbHelper dbHelper)
        {
            _connection = connection;
            _filterHelper = filterHelper;
            _dbHelper = dbHelper;
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await Task.Run(() =>
            {
                _connection.Open();
                var categories = _connection.Query<CategoryDto>("SELECT * FROM category").ToList();
                _connection.Close();
                return categories;
            });

            return Ok(categories);
        }

        [HttpGet("GetExtra")]
        public async Task<ActionResult<IEnumerable<CategoryExtraDto>>> GetCategoriesExtra()
        {
            var categoriesExtra = await Task.Run(() =>
            {
                _connection.Open();
                var categories = _connection.Query<Category>("SELECT * FROM category").ToList();
                var dbEntries = _connection.Query<Entry>("SELECT * FROM entry").ToList();
                var entries = new List<EntryDto>();
                foreach (var entry in dbEntries)
                {
                    entries.Add(new EntryDto { category = _dbHelper.getCategoryName(entry.category_ID), name = entry.name, amount = entry.amount });
                }
                List<CategoryExtraDto> categoriesExtra = new List<CategoryExtraDto>();
                foreach (Category category in categories)
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
            });
            return Ok(categoriesExtra);
        }

        [HttpGet("GetExtraFiltered")]
        public async Task<ActionResult<IEnumerable<CategoryExtraDto>>> GetCategoriesExtra(string startTime = "2010-01-01 00:00:00", string endTime = "3000-01-01 00:00:00")
        {
            var categoriesExtra = await Task.Run(() =>
            {
                _connection.Open();
                var categories = _connection.Query<Category>("SELECT * FROM category").ToList();
                var entries = _filterHelper.FilterTransactions(_connection, startTime, endTime);
                List<CategoryExtraDto> categoriesExtra = new List<CategoryExtraDto>();
                foreach (Category category in categories)
                {
                    categoriesExtra.Add(new CategoryExtraDto { name=category.name, numTransactions=0, percentage=0, total=0});
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

                foreach(CategoryExtraDto catEx in categoriesExtra)
                {
                    catEx.percentage = Math.Round((double) (catEx.total / total) * 100);
                }
                _connection.Close();
                return categoriesExtra;
                });
            return Ok(categoriesExtra);
        }

        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategoryById(int id)
        {
            var category = await Task.Run(() =>
            {
                _connection.Open();
                var category = _connection.Query<CategoryDto>($"SELECT * FROM category where id={id}").SingleOrDefault();
                _connection.Close();
                return category;
            });

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpPost("Insert")]
        public async Task<ActionResult> InsertCategory(CategoryDto categoryDto)
        {
            await Task.Run(() =>
            {
                _connection.Open();
                string sqlQuery = "INSERT INTO category (name, priority) VALUES (@name, @priority)";
                _connection.Execute(sqlQuery, categoryDto);
                _connection.Close();
            });

            return Ok();
        }

        [HttpPatch("Update/{id}")]
        public async Task<ActionResult> UpdateCategory(CategoryDto categoryDto, int id)
        {
            await Task.Run(() =>
            {
                _connection.Open();
                string sqlQuery = $"UPDATE category SET name = @name, priority = @priority WHERE ID = {id}";
                _connection.Execute(sqlQuery, categoryDto);
                _connection.Close();
            });

            return Ok();
        }

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            await Task.Run(() =>
            {
                _connection.Open();
                string sqlQuery = $"DELETE FROM category WHERE ID = {id}";
                _connection.Execute(sqlQuery);
                _connection.Close();
            });

            return Ok();
        }

    }
}
