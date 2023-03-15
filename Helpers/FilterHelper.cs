using Dapper;
using FinanceAPI.Models;
using MySqlConnector;

namespace FinanceAPI.Helpers
{
    public class FilterHelper
    {
        private readonly TimeHelper _timeHelper;
        private readonly DbHelper _dbHelper;
        public FilterHelper (TimeHelper th, DbHelper dh)
        {
            _timeHelper = th;
            _dbHelper = dh;
        }
        public List<EntryDto> FilterTransactions(MySqlConnection _connection, string start, string end)
        {
            var entries = _connection.Query<Entry>("SELECT * FROM entry").ToList();
            List<EntryDto> result = new List<EntryDto>();
            foreach (var entry in entries)
            {
                if (_timeHelper.isDateIn(start, end, entry))
                {
                    result.Add(new EntryDto { category = _dbHelper.getCategoryName(entry.category_ID), name = entry.name, amount = entry.amount, description = entry.description, time = _timeHelper.formatForDisplay(entry.time) });
                }
            }
            return result;
        }

        public List<EntryDto> FilterByCategory(MySqlConnection _connection, string catName)
        {
            List<Entry> entries = _connection.Query<Entry>($"SELECT * FROM entry INNER JOIN category ON entry.Category_id=category.ID where category.name='{catName}'").ToList();
            List<EntryDto> result = new List<EntryDto>();
            foreach (Entry entry in entries)
            {
                result.Add(new EntryDto { category = _dbHelper.getCategoryName(entry.category_ID), name = entry.name, amount = entry.amount, description = entry.description, time = _timeHelper.formatForDisplay(entry.time) });
            }
            return result;
        }

    }
}
