using System;
using System.Text.Json.Serialization;

namespace Model
{
    public class User : BaseEntity // ירושה: מקבל אוטומטית את המאפיין Id
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [JsonIgnore]
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty; // נשלף מטבלת Roles
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public User() { }

        // בנאי ליצירת משתמש חדש (לפני שנשמר במסד, לכן אין לו עדיין Id)
        public User(string username, string email, string password)
        {
            Username = username;
            Email = email;
            Password = password;
            IsActive = true;
        }

        // בנאי לשליפת משתמש קיים ממסד הנתונים (כולל הכל)
        // base(id) שולח את המזהה למחלקת האב BaseEntity
        public User(int id, string username, string email, string password, int roleId, string roleName, bool isActive, DateTime createdAt)
            : base(id)
        {
            Username = username;
            Email = email;
            Password = password;
            RoleId = roleId;
            RoleName = roleName;
            IsActive = isActive;
            CreatedAt = createdAt;
        }
    }
}
