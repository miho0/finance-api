using FinanceAPI.Models;
using System.Globalization;

namespace FinanceAPI.Helpers
{
    public class TimeHelper
    {
        private  string[] months = {
          "January",
          "February",
          "March",
          "April",
          "May",
          "June",
          "July",
          "August",
          "September",
          "October",
          "November",
          "December"
        };
        public DateTime fromJs(string input)
        {
            return DateTime.ParseExact(input, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public DateTime fromReadable(string input)
        {
            DateTime result;
            DateTime.TryParseExact(input, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            return result;
        }

        public string formatForDisplay(DateTime date)
        {
            return date.ToString();
        }

        public bool isDateIn(string start, string end, Entry entry)
        {
            return (DateTime.Compare(fromJs(start), entry.time) <= 0 && DateTime.Compare(fromJs(end), entry.time) >= 0);
        }

        public string GenerateMonthRepresentation(DateTime date)
        {
            int m = date.Month;
            int y = date.Year;
            return m.ToString() + "-" + y.ToString().Substring(2);
        }

        public string GenerateReadableMonthRepresentation(string monthRepresentation)
        {
            string[] repSplit = monthRepresentation.Split('-'); 
            string result = months[int.Parse(repSplit[0])-1];
            if (repSplit[0] == "1")
            {
                result += " " + repSplit[1];
            }
            return result;
        }
    }
}
