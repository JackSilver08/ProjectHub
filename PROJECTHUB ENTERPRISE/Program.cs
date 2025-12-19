using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Hubs;

var builder = WebApplication.CreateBuilder(args);

// =========================
// 1. Add services
// =========================

builder.Services.AddRazorPages();

// ✅ API Controllers
builder.Services.AddControllers();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddAuthentication("ProjectHubCookie")
    .AddCookie("ProjectHubCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "ProjectHub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var app = builder.Build();

// =========================
// 2. Middleware pipeline
// =========================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.MapHub<ProjectHub>("/hubs/project");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Razor Pages
app.MapRazorPages();

// ✅ API
app.MapControllers();

app.Run();
