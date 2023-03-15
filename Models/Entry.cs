namespace FinanceAPI.Models
{
    public class Entry
    {
        public int ID { get; set; }
        public int category_ID { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int amount { get; set; }
        public DateTime time { get; set; }
    }
}
