using Dapper;
using FinanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Cryptography;

namespace FinanceAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly MySqlConnection _connection;
        public UserController(MySqlConnection connection)
        {
            _connection = connection;
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private byte[] GenerateHash(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] passwordWithSalt = new byte[passwordBytes.Length + salt.Length];
                Buffer.BlockCopy(passwordBytes, 0, passwordWithSalt, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, passwordWithSalt, passwordBytes.Length, salt.Length);
                byte[] hash = sha256.ComputeHash(passwordWithSalt);
                return hash;
            }
        }

        private List<UserDto> GetAllUsers()
        {
            _connection.Open();
            var users = _connection.Query<UserDto>("SELECT * FROM user").ToList();
            _connection.Close();
            return users;
        }

        [HttpPost("register")]
        public async Task<ActionResult<Message>> Register(UserDto user)
        {
            _connection.Open();
            var status = await Task.Run(() =>
            {
                var users = GetAllUsers();
                if (users.FirstOrDefault(e => e.email == user.email) != null)
                {
                    return new Message { status = "failed", message = "email already registered" };
                }

                if (users.FirstOrDefault(e => e.username == user.username) != null)
                {
                    return new Message { status = "failed", message = "email already registered" };
                }
                // TODO logic for sending email

                byte[] salt = GenerateSalt();
                string hash = Convert.ToBase64String(GenerateHash(user.password, salt));
                _connection.Query($"INSERT INTO user (username, password, email) VALUES ({user.username}, {hash}, {user.email})");
                return new Message { status = "success", message = "user registered" };
            });
            return Ok(status);
        }

        [HttpGet("login")]
        public async Task<ActionResult<Message>> Login(UserDto user)
        {
            _connection.Open();
            var status = await Task.Run(() =>
            {
                var users = GetAllUsers();
                if (users.FirstOrDefault(e => e.email == user.email) != null)
                {
                    return new Message { status = "failed", message = "email already registered" };
                }

                if (users.FirstOrDefault(e => e.username == user.username) != null)
                {
                    return new Message { status = "failed", message = "email already registered" };
                }
                // TODO logic for sending email
                return new Message { status = "success", message = "user registered" };
            });
            return Ok(status);
        }
    }
}
