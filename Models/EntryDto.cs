namespace FinanceAPI.Models
{
    public class EntryDto
    {
        public string category { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int amount { get; set; }
        public string time { get; set; }
    }
}
