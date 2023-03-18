namespace FinanceAPI.Models
{
    public class User
    {
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public byte[] salt { get; set; }
    }
}
