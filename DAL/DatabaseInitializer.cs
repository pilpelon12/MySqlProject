using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DAL
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(string connectionString)
        {
            // 1. יצירת מסד הנתונים בשרת במידה ואינו קיים
            await CreateDatabaseAsync(connectionString);

            // מחרוזת חיבור מלאה הכוללת את שם בסיס הנתונים שנוצר
            string fullConnString = connectionString + "Database=CSDepartmentDB;";

            // 2. יצירת הטבלאות
            await CreateTablesAsync(fullConnString);

            // 3. הזנת נתוני ברירת מחדל
            await SeedInitialDataAsync(fullConnString);
        }

        private static async Task CreateDatabaseAsync(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = "CREATE DATABASE IF NOT EXISTS CSDepartmentDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task CreateTablesAsync(string fullConnString)
        {
            using var connection = new MySqlConnection(fullConnString);
            await connection.OpenAsync();

            string createTablesSql = @"
                -- 1. טבלת תפקידים
                CREATE TABLE IF NOT EXISTS Roles (
                    RoleId INT AUTO_INCREMENT PRIMARY KEY,
                    RoleName VARCHAR(50) NOT NULL UNIQUE
                );

                -- 2. טבלת משתמשים
                CREATE TABLE IF NOT EXISTS Users (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(50) NOT NULL UNIQUE,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    PasswordHash VARCHAR(255) NOT NULL,
                    RoleId INT NOT NULL,
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
                );

                -- 3. טבלת נושאי לימוד במגמה
                CREATE TABLE IF NOT EXISTS Topics (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(100) NOT NULL,
                    Description TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                -- 4. טבלת חומרי למידה
                CREATE TABLE IF NOT EXISTS LearningMaterials (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(150) NOT NULL,
                    ContentOrUrl TEXT NOT NULL,
                    TopicId INT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (TopicId) REFERENCES Topics(Id) ON DELETE CASCADE
                );

                -- 5. טבלת תרגילים ומשימות
                CREATE TABLE IF NOT EXISTS Exercises (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(150) NOT NULL,
                    Instruction TEXT NOT NULL,
                    MaxScore INT DEFAULT 100,
                    DueDate DATETIME NOT NULL,
                    TopicId INT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (TopicId) REFERENCES Topics(Id) ON DELETE CASCADE
                );

                -- 6. טבלה מקשרת: הגשות תרגילים
                CREATE TABLE IF NOT EXISTS Submissions (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    StudentId INT NOT NULL,
                    ExerciseId INT NOT NULL,
                    SolutionCode TEXT NOT NULL,
                    Grade INT NULL,
                    Feedback TEXT NULL,
                    SubmittedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (StudentId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ExerciseId) REFERENCES Exercises(Id) ON DELETE CASCADE
                );

                -- 7. טבלת הודעות מגמה
                CREATE TABLE IF NOT EXISTS Announcements (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(150) NOT NULL,
                    Content TEXT NOT NULL,
                    AuthorId INT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (AuthorId) REFERENCES Users(Id) ON DELETE CASCADE
                );
            ";

            using var command = new MySqlCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task SeedInitialDataAsync(string fullConnString)
        {
            using var connection = new MySqlConnection(fullConnString);
            await connection.OpenAsync();

            // 1. הזנת תפקידים
            string checkRolesSql = "SELECT COUNT(*) FROM Roles;";
            using var checkRolesCmd = new MySqlCommand(checkRolesSql, connection);
            long rolesCount = (long)await checkRolesCmd.ExecuteScalarAsync();

            if (rolesCount == 0)
            {
                string insertRolesSql = "INSERT INTO Roles (RoleId, RoleName) VALUES (1, 'Admin'), (2, 'Teacher'), (3, 'Student');";
                using var insertRolesCmd = new MySqlCommand(insertRolesSql, connection);
                await insertRolesCmd.ExecuteNonQueryAsync();
            }

            // 2. הזנת משתמשים התחלתיים עם סיסמאות מגובבות
            await SeedUsersAsync(connection);

            // 3. הזנת נושא לימוד ראשוני
            string checkTopicsSql = "SELECT COUNT(*) FROM Topics;";
            using var checkTopicsCmd = new MySqlCommand(checkTopicsSql, connection);
            long topicsCount = (long)await checkTopicsCmd.ExecuteScalarAsync();

            if (topicsCount == 0)
            {
                string insertTopicSql = "INSERT INTO Topics (Title, Description) VALUES ('יסודות מדעי המחשב ב-C#', 'חומר לימוד ותרגילים בנושא משתנים, תנאים ולולאות');";
                using var insertTopicCmd = new MySqlCommand(insertTopicSql, connection);
                await insertTopicCmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task SeedUsersAsync(MySqlConnection connection)
        {
            string checkSql = "SELECT COUNT(*) FROM Users;";
            using var checkCmd = new MySqlCommand(checkSql, connection);
            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            if (count > 0) return;

            string insertSql = @"
        INSERT INTO Users (Username, Email, PasswordHash, RoleId) VALUES
        (@Username1, @Email1, @Hash1, (SELECT RoleId FROM Roles WHERE RoleName = 'Admin')),
        (@Username2, @Email2, @Hash2, (SELECT RoleId FROM Roles WHERE RoleName = 'Teacher')),
        (@Username3, @Email3, @Hash3, (SELECT RoleId FROM Roles WHERE RoleName = 'Student'));";

            using var insertCmd = new MySqlCommand(insertSql, connection);

            // 1. יצירת מנהל התחלתי
            insertCmd.Parameters.AddWithValue("@Username1", "admin");
            insertCmd.Parameters.AddWithValue("@Email1", "admin@cs-dept.edu");
            insertCmd.Parameters.AddWithValue("@Hash1", BCrypt.Net.BCrypt.HashPassword("admin"));

            // 2. יצירת מורה התחלתי
            insertCmd.Parameters.AddWithValue("@Username2", "teacher");
            insertCmd.Parameters.AddWithValue("@Email2", "teacher@cs-dept.edu");
            insertCmd.Parameters.AddWithValue("@Hash2", BCrypt.Net.BCrypt.HashPassword("teacher"));

            // 3. יצירת תלמיד התחלתי
            insertCmd.Parameters.AddWithValue("@Username3", "user");
            insertCmd.Parameters.AddWithValue("@Email3", "user@student.edu");
            insertCmd.Parameters.AddWithValue("@Hash3", BCrypt.Net.BCrypt.HashPassword("user"));

            await insertCmd.ExecuteNonQueryAsync();
        }
    }
}