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
