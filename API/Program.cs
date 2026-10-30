using DAL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// קריאת מחרוזת החיבור מתוך קובץ ההגדרות (appsettings.json או Secrets)
string? serverConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// מוסיפים למחרוזת את שם מסד הנתונים שיצרנו קודם
string dbConnectionString = $"{serverConnectionString}Database=CSDepartmentDB;";

// רישום מחלקת UserDB כ"סינגלטון" (מופע אחד שחי כל עוד השרת עובד)
builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

var app = builder.Build();

string connStr = builder.Configuration.GetConnectionString("DefaultConnection");

// בדיקה שהמחרוזת אכן קיימת ב-User Secrets
if (string.IsNullOrEmpty(connStr))
{
    throw new Exception("שגיאה: מחרוזת החיבור DefaultConnection לא נמצאה ב-User Secrets!");
}

// 2. הפעלת הקוד שבונה את בסיס הנתונים והטבלאות ב-DAL
await DatabaseInitializer.InitializeAsync(connStr);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
