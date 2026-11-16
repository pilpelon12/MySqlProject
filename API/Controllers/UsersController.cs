using System.Collections.Generic;
using System.Threading.Tasks;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserDB _userDB;

        public UsersController(UserDB userDB)
        {
            _userDB = userDB;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            var users = await _userDB.GetAllUsersAsync();
            foreach (var u in users)
            {
                u.Password = string.Empty; // ריקון הסיסמה לפני שליחה ללקוח
            }
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _userDB.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("User not found.");

            user.Password = string.Empty; // ריקון הסיסמה לפני שליחה ללקוח
            return Ok(user);
        }

        // שלב 1 בהרשמה: בדיקה האם שם משתמש קיים
        [HttpGet("exists/{username}")]
        public async Task<ActionResult<bool>> UsernameExists(string username)
        {
            bool exists = await _userDB.UsernameExistsAsync(username);
            return Ok(exists);
        }

        // שלב 1 בהרשמה: בדיקה האם אימייל קיים
        [HttpGet("email-exists/{email}")]
        public async Task<ActionResult<bool>> EmailExists(string email)
        {
            bool exists = await _userDB.EmailExistsAsync(email);
            return Ok(exists);
        }

        // שלב 2 בהרשמה: הוספת משתמש חדש למערכת
        [HttpPost]
        public async Task<ActionResult<User>> AddUser([FromBody] User newUser)
        {
            if (await _userDB.UsernameExistsAsync(newUser.Username))
                return Conflict("Username is already taken."); // 409

            if (await _userDB.EmailExistsAsync(newUser.Email))
                return Conflict("Email is already taken."); // 409

            string role = string.IsNullOrEmpty(newUser.RoleName) ? "Student" : newUser.RoleName;

            int newId = await _userDB.AddUserAsync(newUser.Username, newUser.Email, newUser.Password, role);
            var createdUser = await _userDB.GetUserByIdAsync(newId);

            if (createdUser != null)
                createdUser.Password = string.Empty; // ריקון הסיסמה לפני שליחה חזרה

            return CreatedAtAction(nameof(GetUserById), new { id = newId }, createdUser);
        }

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] LoginRequest request)
        {
            var user = await _userDB.CheckLoginAsync(request.Username, request.Password);
            if (user == null)
                return Unauthorized("Invalid username or password."); // 401

            user.Password = string.Empty; // ריקון הגיבוב כדי שלא ייחשף ללקוח
            return Ok(user);
        }

        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordRequest request)
        {
            bool updated = await _userDB.UpdatePasswordAsync(id, request.NewPassword);
            if (!updated)
                return NotFound("User not found.");

            return NoContent(); // 204
        }
    }

    public record LoginRequest(string Username, string Password);
    public record UpdatePasswordRequest(string NewPassword);
}