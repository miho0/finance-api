using Dapper;
using FinanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using MySqlConnector;
using System.Reflection.Metadata.Ecma335;

namespace FinanceAPI.Helpers
{
    public class DbHelper
    {
        private readonly MySqlConnection _connection;
        private readonly TimeHelper _timeHelper;

        public DbHelper(MySqlConnection connection, TimeHelper timeHelper)
        {
            _connection = connection;
            _timeHelper = timeHelper;
        }

        public string getCategoryName(int id)
        {
            var categories = _connection.Query<Category>("SELECT * FROM category").ToList();
            foreach (var category in categories)
            {
                if (category.ID == id)
                {
                    return category.name;
                }
            }
            _connection.Close();
            return "";
        }

        public int GetCategoryId(string name)
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }
            var categories = _connection.Query<Category>("SELECT * FROM category").ToList();
            foreach (var category in categories)
            {
                if (category.name == name)
                {
                    _connection.Close();
                    return category.ID;
                }
            }
            _connection.Close();
            return -1;
        }

        // returns the categories belonging to a user according to filters if one is registered
        public Task<List<CategoryDto>> GetUserCategories(string? userid)
        {
            return Task.Run(() =>
            {
                _connection.Open();
                if (userid != null)
                {
                    _connection.Close();
                    return _connection.Query<CategoryDto>("SELECT * FROM category WHERE userid = @userid", new { userid = int.Parse(userid) }).ToList();
                }
                else
                {
                    _connection.Close();
                    return new List<CategoryDto>();
                }
            });
        }

        // returns the entries belonging to a user if one is registered

        public async Task<List<EntryDto>> GetUserEntries(Filter filter)
        {
            var result = await Task.Run(() =>
            {
                _connection.Open();
                if (filter.userid != null)
                {
                    object obj = new
                    {
                        id = int.Parse(filter.userid),
                        catName = filter.catName,
                        startDate = filter.startDate,
                        endDate = filter.endDate,
                    };

                    string sql = "SELECT * FROM entry JOIN category ON entry.Category_ID=category.ID WHERE userid = @id";
                    if (filter.catName != "none")
                    {
                        sql += " and category.name = @catName";
                    }
                    if (filter.startDate != "none")
                    {
                        sql += " and time > @startDate";
                    }
                    if (filter.endDate != "none")
                    {
                        sql += " and time < @endDate";
                    }

                    List<EntryDto> result = new List<EntryDto>();
                    var entries = _connection.Query<Entry>(sql, obj).ToList();

                    foreach (var entry in entries)
                    {
                        result.Add(new EntryDto { category = getCategoryName(entry.category_ID), name = entry.name, amount = entry.amount, description = entry.description, time = _timeHelper.formatForDisplay(entry.time) });
                    }
                    _connection.Close();
                    return result;
                }
                else
                {
                    _connection.Close();
                    return new List<EntryDto>();
                }
            });
            return result;
        }

    }
}

// SELECT * FROM entry JOIN category ON entry.Category_ID=category.ID WHERE userid = 3 and category.name = 'Groceries' and time > '2022-03-22' and time < '2022-10-04'