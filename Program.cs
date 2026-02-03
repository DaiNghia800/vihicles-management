using Fastkart.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Public_Transport.Helpers;
using Public_Transport.Middleware;
using Public_Transport.Models.EF;
using Public_Transport.Models.Entities;
using Public_Transport.Services;
using Public_Transport.Services.IServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews(options =>
{
    // Tắt implicit required validation cho non-nullable value types
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// ✅ FIX: Configure Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always; // ← Bắt buộc HTTPS
});

// Configure session
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ← Bắt buộc HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Configure authentication cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";  // ← ĐÃ ĐỔI: từ "/Account/Login" thành "/login"
        options.AccessDeniedPath = "/access-denied";  // ← ĐÃ ĐỔI: từ "/Account/AccessDenied" thành "/access-denied"
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ← Bắt buộc HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddCookie("External", options =>
    {
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddGoogle(googleOptions =>
    {
        googleOptions.SignInScheme = "External";
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        googleOptions.CallbackPath = "/signin-google";
        googleOptions.Scope.Clear();
        googleOptions.Scope.Add("openid");
        googleOptions.Scope.Add("profile");
        googleOptions.Scope.Add("email");
        googleOptions.SaveTokens = true;
    })
    .AddFacebook(facebookOptions =>
    {
        facebookOptions.SignInScheme = "External";
        facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
        facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

        facebookOptions.CallbackPath = "/signin-facebook";
        facebookOptions.Scope.Clear();
        facebookOptions.Scope.Add("email");
        facebookOptions.Scope.Add("public_profile");

        facebookOptions.SaveTokens = true;

        facebookOptions.Fields.Clear();
        facebookOptions.Fields.Add("name");
        facebookOptions.Fields.Add("email");
        facebookOptions.Fields.Add("picture");
        facebookOptions.Fields.Add("first_name");
        facebookOptions.Fields.Add("last_name");
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NoCustomer", policy =>

    {
        // 2. Yêu cầu người dùng phải đăng nhập
        policy.RequireAuthenticatedUser();
        // 3. Yêu cầu người dùng KHÔNG CÓ vai trò "Customer"
        policy.RequireAssertion(context =>
            !context.User.IsInRole(WebConstants.ROLE_CUSTOMER));
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped);
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // ← Bắt buộc HTTPS
}

app.UseHttpsRedirection(); // ← Redirect HTTP sang HTTPS
app.UseStaticFiles();

app.UseCookiePolicy(); // ← Áp dụng Cookie Policy

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
