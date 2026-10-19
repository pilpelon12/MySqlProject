using DAL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
