using Dapper;
using FinanceAPI.Models;
using MySqlConnector;

namespace FinanceAPI.Helpers
{
    public class DbHelper
    {
        private readonly MySqlConnection _connection;

        public DbHelper(MySqlConnection connection)
        {
            _connection = connection;
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
    }
}
