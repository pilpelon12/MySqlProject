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

            // 2. הזנת משתמשים
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
                ('admin_coordinator', 'admin@cs-dept.edu', 'Admin123!', 1),
                ('teacher_dan', 'dan@cs-dept.edu', 'Teach123!', 2),
                ('teacher_rona', 'rona@cs-dept.edu', 'Teach123!', 2),
                ('student_noam', 'noam@student.edu', 'Stud123!', 3),
                ('student_maya', 'maya@student.edu', 'Stud123!', 3),
                ('student_itay', 'itay@student.edu', 'Stud123!', 3),
                ('student_shira', 'shira@student.edu', 'Stud123!', 3),
                ('student_omer', 'omer@student.edu', 'Stud123!', 3),
                ('student_yael', 'yael@student.edu', 'Stud123!', 3),
                ('student_guy', 'guy@student.edu', 'Stud123!', 3);";

            using var insertCmd = new MySqlCommand(insertSql, connection);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }
}