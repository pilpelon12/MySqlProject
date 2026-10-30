using System.Collections.Generic;
using System.Threading.Tasks;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    // כל בקשה ל- api/users תגיע למחלקה הזו
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserDB _userDB;

        // השרת מעביר לפה אוטומטית את UserDB בזכות הרישום שעשינו ב-Program.cs
        public UsersController(UserDB userDB)
        {
            _userDB = userDB;
        }

        // בקשת GET בסיסית: שליפת רשימת כל המשתמשים
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            var users = await _userDB.GetAllUsersAsync();
            return Ok(users); // מחזיר קוד 200 ואת רשימת המשתמשים
        }

        // בקשת GET עם פרמטר ב-URL (למשל api/users/5)
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _userDB.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("User not found."); // מחזיר 404 אם המשתמש לא קיים

            return Ok(user);
        }

        // בקשת POST: הוספת משתמש חדש למערכת
        [HttpPost]
        public async Task<ActionResult<User>> AddUser([FromBody] CreateUserRequest request)
        {
            // בדיקות קדם-הכנסה כדי לא לזרוק שגיאות שרת פנימיות
            if (await _userDB.UsernameExistsAsync(request.Username))
                return Conflict("Username is already taken."); // 409

            if (await _userDB.EmailExistsAsync(request.Email))
                return Conflict("Email is already taken."); // 409

            // הגדרת תפקיד ברירת מחדל אם לא סופק
            string role = string.IsNullOrEmpty(request.RoleName) ? "Student" : request.RoleName;

            // יצירה ושליפה חזרה
            int newId = await _userDB.AddUserAsync(request.Username, request.Email, request.Password, role);
            var createdUser = await _userDB.GetUserByIdAsync(newId);

            // מחזיר קוד 201 (Created) ומצביע על הנתיב של המשתמש החדש
            return CreatedAtAction(nameof(GetUserById), new { id = newId }, createdUser);
        }

        // בקשת POST: התחברות (למרות שאנחנו שולפים מידע, נהוג להשתמש ב-POST כדי להסתיר סיסמאות בגוף הבקשה)
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] LoginRequest request)
        {
            var user = await _userDB.CheckLoginAsync(request.Username, request.Password);
            if (user == null)
                return Unauthorized("Invalid username or password."); // 401 (שגיאת התחברות)

            return Ok(user);
        }

        // בקשת PUT: עדכון סיסמה למשתמש קיים
        [HttpPut("{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordRequest request)
        {
            bool updated = await _userDB.UpdatePasswordAsync(id, request.NewPassword);
            if (!updated)
                return NotFound("User not found.");

            return NoContent(); // 204 (הפעולה הצליחה, אין מידע להחזיר במסך)
        }
    }

    // אובייקטים קלים (Records) המשמשים רק כ"תבניות" לקבלת המידע מהמשתמש בגוף בקשת ה-HTTP
    public record CreateUserRequest(string Username, string Email, string Password, string? RoleName = "Student");
    public record LoginRequest(string Username, string Password);
    public record UpdatePasswordRequest(string NewPassword);
}