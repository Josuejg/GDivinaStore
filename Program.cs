using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
var builder = WebApplication.CreateBuilder(args);

// MVC + RuntimeCompilation (opcional para hot-reload de vistas en desarrollo)
builder.Services.AddControllersWithViews();




builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<GraciaDivina.Models.AccesoDatos>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Auth/Login";
        options.AccessDeniedPath = "/Admin/Auth/Denied";
        options.Cookie.Name = "GD_ADMIN";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();
// Cultura Costa Rica para moneda, fechas y números
var cr = new CultureInfo("es-CR");
CultureInfo.DefaultThreadCurrentCulture = cr;
CultureInfo.DefaultThreadCurrentUICulture = cr;

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cr),
    SupportedCultures = new[] { cr },
    SupportedUICultures = new[] { cr }
});




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

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
