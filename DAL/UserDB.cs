using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    public class UserDB
    {
        private readonly string _connectionString;

        // שאילתת בסיס שחוזרת על עצמה: שולפת משתמש ומחברת את טבלת התפקידים כדי לקבל את שם התפקיד
        private const string BaseSelectQuery = @"
            SELECT u.Id, u.Username, u.Email, u.PasswordHash, u.RoleId, r.RoleName, u.IsActive, u.CreatedAt
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.RoleId";

        // בנאי שמקבל את מחרוזת החיבור מהשרת 
        public UserDB(string connectionString)
        {
            _connectionString = connectionString;
        }

        // פונקציית עזר פרטית: לוקחת שורה אחת ממסד הנתונים וממירה אותה לאובייקט User
        private User ReadUser(IDataReader reader)
        {
            return new User(
                reader.GetInt32(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("Username")),
                reader.GetString(reader.GetOrdinal("Email")),
                reader.GetString(reader.GetOrdinal("PasswordHash")),
                reader.GetInt32(reader.GetOrdinal("RoleId")),
                reader.GetString(reader.GetOrdinal("RoleName")),
                reader.GetBoolean(reader.GetOrdinal("IsActive")),
                reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            );
        }

        // הוספת משתמש. הפקודה LAST_INSERT_ID מחזירה את ה-Id שהמספור האוטומטי נתן לו
        public async Task<int> AddUserAsync(string username, string email, string password, string roleName = "Student")
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Users (Username, Email, PasswordHash, RoleId)
                VALUES (@Username, @Email, @Password, (SELECT RoleId FROM Roles WHERE RoleName = @RoleName));
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);
            cmd.Parameters.AddWithValue("@RoleName", roleName);

            object? result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // בדיקה אם שם משתמש כבר קיים במערכת (מחזיר True/False)
        public async Task<bool> UsernameExistsAsync(string username)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Username", username);

            long count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        // בדיקה אם אימייל כבר קיים
        public async Task<bool> EmailExistsAsync(string email)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Users WHERE Email = @Email;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Email", email);

            long count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        // התחברות: בודק אם המשתמש קיים ופעיל (IsActive = TRUE)
        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = $"{BaseSelectQuery} WHERE u.Username = @Username AND u.IsActive = TRUE;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Username", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                User user = ReadUser(reader);
                // אנו משווים את הסיסמה בקוד (C#) ולא בשאילתה, כי MySQL לא מבדיל כברירת מחדל בין אותיות גדולות לקטנות
                if (user.Password == password)
                {
                    return user;
                }
            }
            return null; // אם לא נמצא או שהסיסמה שגויה
        }

        // שליפת משתמש ספציפי לפי מזהה
        public async Task<User?> GetUserByIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = $"{BaseSelectQuery} WHERE u.Id = @Id;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadUser(reader);
            }
            return null;
        }

        // שליפת כל המשתמשים כרשימה
        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = $"{BaseSelectQuery} ORDER BY u.Id;";
            using var cmd = new MySqlCommand(sql, connection);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) // הלולאה עוברת שורה שורה
            {
                users.Add(ReadUser(reader));
            }
            return users;
        }

        // עדכון סיסמה
        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "UPDATE Users SET PasswordHash = @Password WHERE Id = @Id;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Password", newPassword);
            cmd.Parameters.AddWithValue("@Id", userId);

            // מחזיר את כמות השורות שהשתנו. אם זה גדול מ-0, זה אומר שהעדכון הצליח
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
