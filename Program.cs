using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CandidApply.Data;
using CandidApply.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CandidApplyContextConnection") ?? throw new InvalidOperationException("Connection string 'CandidApplyContextConnection' not found.");

// Add services to the container.
builder.Services.AddControllersWithViews();

//DB Setting
IConfiguration configuration = builder.Configuration;
builder.Services.AddDbContext<ApplicationContext>(Options =>
    Options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection")
        )
    );

builder.Services.AddDbContext<UserContext>(Options =>
    Options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection")
       )
    );

//builder.Services.AddDefaultIdentity<IdentityUser>
builder.Services.AddIdentity<User, IdentityRole>
    (options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<UserContext>()
    .AddDefaultTokenProviders();

builder.Services.AddLogging(options => options.AddConsole());

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<ApplicationContext>();
builder.Services.AddRazorPages();

builder.Services.AddControllers(config =>
{
    var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
    config.Filters.Add(new AuthorizeFilter(policy));
});

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

app.UseAuthentication();
app.UseAuthorization();


app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

CreateDbIfNotExists(app);

app.Run();

static void CreateDbIfNotExists(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationContext>();
            DbInitializer.Initialize(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred creating the DB.");
        }
    }
}
