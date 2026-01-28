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

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromHours(3);
    options.Cookie.Name = "public-transport.session";
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromHours(3);
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = false;
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

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
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
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AdminAccessMiddleware>();
app.UseSession();
app.MapControllerRoute(
      name: "Admin",
      pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
