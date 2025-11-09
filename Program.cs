using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http; // SameSiteMode

var builder = WebApplication.CreateBuilder(args);

// ===== MVC =====
builder.Services.AddControllersWithViews();

// ===== Infra =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// AccesoDatos: usar Scoped (mejor que Singleton para conexiones por request)
builder.Services.AddScoped<GraciaDivina.Models.AccesoDatos>();

// ===== Auth (Cookies) =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Auth/Login";
        options.AccessDeniedPath = "/Admin/Auth/Denied";
        options.Cookie.Name = "GD_ADMIN";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;   // evita bloqueos del navegador
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ===== Cultura es-CR =====
var cr = new CultureInfo("es-CR");
CultureInfo.DefaultThreadCurrentCulture = cr;
CultureInfo.DefaultThreadCurrentUICulture = cr;

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cr),
    SupportedCultures = new[] { cr },
    SupportedUICultures = new[] { cr }
});

// ===== Pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ===== Rutas =====
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
