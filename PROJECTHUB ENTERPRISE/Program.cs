using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;

var builder = WebApplication.CreateBuilder(args);

// =========================
// 1. Add services
// =========================

// Razor Pages
builder.Services.AddRazorPages();

// PostgreSQL DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// =========================
// Authentication (Cookie)
// =========================
builder.Services.AddAuthentication("ProjectHubCookie")
    .AddCookie("ProjectHubCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // 🔐 Cookie settings (FIX warning & security)
        options.Cookie.Name = "ProjectHub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// =========================
// 2. Middleware pipeline
// =========================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ⚠️ Luôn redirect HTTPS
app.UseHttpsRedirection();

// Static files (CSS, JS)
app.UseStaticFiles();

app.UseRouting();

// ⚠️ BẮT BUỘC: Auth trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// Razor Pages
app.MapRazorPages();

// =========================
// 3. Run app
// =========================
app.Run();
