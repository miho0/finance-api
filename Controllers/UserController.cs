using Dapper;
using FinanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Cryptography;
using System.Text;

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

        private string GenerateHash(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] passwordWithSalt = new byte[passwordBytes.Length + salt.Length];
                Buffer.BlockCopy(passwordBytes, 0, passwordWithSalt, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, passwordWithSalt, passwordBytes.Length, salt.Length);
                byte[] hash = sha256.ComputeHash(passwordWithSalt);
                return Convert.ToBase64String(hash);
            }
        }
        private List<UserDto> GetAllUsers()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }
            var users = _connection.Query<UserDto>("SELECT * FROM user").ToList();
            if (_connection.State != System.Data.ConnectionState.Closed)
            {
                _connection.Close();
            }
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
                    return new Message { status = "failed", message = "username already taken" };
                }
                // TODO logic for sending email

                byte[] salt = GenerateSalt();
                string hash = GenerateHash(user.password, salt);
                _connection.Query("INSERT INTO user (username, password, email, salt) VALUES (@username, @hash, @email, @salt)", new { username = user.username, hash = hash, email = user.email, salt = salt });
                return new Message { status = "success", message = "user registered" };
            });
            return Ok(status);
        }

        [HttpGet("login/{username}/{password}")]
        public async Task<ActionResult<Message>> Login(string username, string password)
        {
            _connection.Open();
            var status = await Task.Run(() =>
            {
                List<User> user =  _connection.Query<User>($"SELECT * FROM user WHERE username = @username", new { username = username }).ToList();
                if (user.Count > 0)
                {
                    byte[] salt = user[0].salt;
                    string hash1 = user[0].password;
                    string hash2 = GenerateHash(password, salt);
                    if (hash1 == hash2)
                    {
                        HttpContext.Session.SetString("username", username);
                        return new Message { status = "success", message = "user logged in" };
                    }
                    else
                    {
                        return new Message { status = "failed", message = "wrong password" };
                    }
                } else
                {
                    return new Message { status = "failed", message = "user doesn't exist" };
                }

            });
            return Ok(status);
        }

        [HttpGet("currentUser")]
        public async Task<ActionResult<Message>> GetLoggedUser()
        {
            var status = await Task.Run(() =>
            {
                var username = HttpContext.Session.GetString("username");
                if (username == null)
                {
                    return new Message { status = "failed", message = "no user is logged in at this time" };
                }
                else
                {
                    return new Message { status = "success", message = username };
                }
            });
            return status;
        }
    }
}
