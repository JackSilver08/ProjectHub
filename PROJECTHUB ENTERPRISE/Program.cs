using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Hubs;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using PROJECTHUB_ENTERPRISE.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// =========================
// 1. Add services
// =========================

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

// ✅ API Controllers
builder.Services.AddControllers();

// ✅ Business Logic Services (DI)
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IWikiService, WikiService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserService, UserService>();


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
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
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
