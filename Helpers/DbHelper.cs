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
            return "";
        }

        public int GetCategoryId(string name)
        {
            var categories = _connection.Query<Category>("SELECT * FROM category").ToList();
            foreach (var category in categories)
            {
                if (category.name == name)
                {
                    return category.ID;
                }
            }
            return -1;
        }
    }
}
