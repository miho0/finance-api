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
                string? userId = HttpContext.Session.GetString("ID");

                if (userId != null)
                {
                    int id = int.Parse(userId);
                    var categories = _connection.Query<CategoryDto>("SELECT * FROM category WHERE ID = @id", new {id = id}).ToList();
                    _connection.Close();
                    return categories;
                } else
                {
                    _connection.Close();
                    return new List<CategoryDto>();
                }
            });

            return Ok(categories);
        }

        [HttpGet("GetExtra")]
        public async Task<ActionResult<IEnumerable<CategoryExtraDto>>> GetCategoriesExtra()
        {
            var categoriesExtra = await Task.Run(() =>
            {
                _connection.Open();
                string? userId = HttpContext.Session.GetString("ID");
                if (userId != null)
                {
                    int id = int.Parse(userId);
                    var categories = _connection.Query<Category>("SELECT * FROM category WHERE userid = @id", new {id = id}).ToList();
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
                } else
                {
                    return new List<CategoryExtraDto>();
                }

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

        [HttpGet("GetByName/{name}")]
        public async Task<ActionResult<CategoryDto>> GetCategoryByName(string name)
        {
            var category = await Task.Run(() =>
            {
                _connection.Open();
                string? userId = HttpContext.Session.GetString("ID");
                if (userId != null)
                {
                    var category = _connection.Query<CategoryDto>("SELECT * FROM category where userid = @userid and name = @name", new { userid = userId, name = name}).SingleOrDefault();
                    _connection.Close();
                    return category;
                } else
                {
                    return null;
                }
            });

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpPost("Insert")]
        public async Task<ActionResult<Message>> InsertCategory(CategoryDto categoryDto)
        {
            var response = await Task.Run(() =>
            {
            _connection.Open();
            string? userId = HttpContext.Session.GetString("ID");
                if (userId != null)
                {
                    int id = int.Parse(userId);
                    var category = _connection.Query<Category>("SELECT * FROM category WHERE userid = @id and name = @name", new {id = id, name = categoryDto.name}).ToList();
                    if (category.Count() > 0)
                    {
                        _connection.Close();
                        return new Message { status = "failed", message = $"you already have a category named {categoryDto.name}" };
                    }
                    _connection.Query("INSERT INTO category (userid, name, priority) VALUES (@userId, @name, @priority)", new {userId = id, name = categoryDto.name, priority = categoryDto.priority});
                    _connection.Close();
                    return new Message { status = "success", message = "category added successfully." };
                } else
                {
                    _connection.Close();
                    return new Message { status = "failed", message = "you are not logged in" };

                }
            });

            return Ok(response);
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

        [HttpDelete("Delete/{name}")]
        public async Task<ActionResult<Message>> DeleteCategory(string name)
        {
            var status = await Task.Run(() =>
            {
                string? userId = HttpContext.Session.GetString("ID");
                if (userId != null)
                {
                    int id = int.Parse(userId);
                    _connection.Query("DELETE FROM category WHERE name = @name and userid = @id", new { name = name, id = id });
                    _connection.Close();
                    return new Message { status = "success", message = "deleted succcessfully" };
                }
                else
                {
                    _connection.Close();
                    return new Message { status = "failed", message = "you are not logged in" };
                }
            });

            return Ok(status);
        }

    }
}
