using CitizenRequest.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контекст базы данных
builder.Services.AddDbContext<CitizenRequestsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка аутентификации (ИСПРАВЛЕНО)
builder.Services.AddAuthentication("CitizenCookie")
    .AddCookie("CitizenCookie", options =>
    {
        options.LoginPath = "/Citizen/Login";
        options.Cookie.Name = "AuthCookie";
        options.AccessDeniedPath = "/Citizen/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
    });

// Регистрируем контроллеры с представлениями
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Настройка конвейера
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Citizen/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Порядок важен!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Citizen}/{action=Index}/{id?}");

app.Run();