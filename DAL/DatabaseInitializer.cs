using System;
using System.Collections.Generic;
using System.Text;

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

            // 3. הזנת נתוני ברירת מחדל (תפקידים ורכז מגמה ראשוני)
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

            // שאילתה ליצירת 7 הטבלאות מנורמלות לפי הסדר הנכון (בגלל מפתחות זרים)
            string createTablesSql = @"
                -- 1. טבלת תפקידים
                CREATE TABLE IF NOT EXISTS Roles (
                    RoleId INT AUTO_INCREMENT PRIMARY KEY,
                    RoleName VARCHAR(50) NOT NULL UNIQUE
                );

                -- 2. טבלת משתמשים
                CREATE TABLE IF NOT EXISTS Users (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    FullName VARCHAR(100) NOT NULL,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    PasswordHash VARCHAR(255) NOT NULL,
                    RoleId INT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE RESTRICT
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

                -- 6. טבלה מקשרת: הגשות תרגילים (בין תלמיד לתרגיל)
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

            // 1. הזנת תפקידים בסיסיים
            string checkRolesSql = "SELECT COUNT(*) FROM Roles;";
            using var checkRolesCmd = new MySqlCommand(checkRolesSql, connection);
            long rolesCount = (long)await checkRolesCmd.ExecuteScalarAsync();

            if (rolesCount == 0)
            {
                string insertRolesSql = "INSERT INTO Roles (RoleId, RoleName) VALUES (1, 'Admin'), (2, 'Student');";
                using var insertRolesCmd = new MySqlCommand(insertRolesSql, connection);
                await insertRolesCmd.ExecuteNonQueryAsync();
            }

            // 2. הזנת נושא לימוד ראשוני לדוגמה
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
    }
}
