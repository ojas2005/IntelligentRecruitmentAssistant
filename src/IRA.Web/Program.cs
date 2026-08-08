using IRA.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ----- Typed API client pointing at the ASP.NET Core Web API backend -----
// Forwards the signed-in user's JWT (stored in the auth cookie) on every backend call.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5180/";
builder.Services.AddHttpClient<RecruitmentApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddControllersWithViews();

// ----- Cookie authentication: the portal signs users in after a JWT login against the API -----
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

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
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
