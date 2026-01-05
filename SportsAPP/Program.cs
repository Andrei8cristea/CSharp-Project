using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;
using SportsAPP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Support both SQL Server (local) and PostgreSQL (production)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Detect database provider based on connection string
    if (connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase) || 
        connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        // PostgreSQL for production (Render)
        options.UseNpgsql(connectionString);
    }
    else
    {
        // SQL Server for local development
        options.UseSqlServer(connectionString);
    }
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with ApplicationUser and Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add Memory Cache for rate limiting
builder.Services.AddMemoryCache();

// Register content moderation and rate limiting services
builder.Services.AddHttpClient<GroqApiClient>();
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
builder.Services.AddSingleton<IRateLimitService, RateLimitService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed data and apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Apply migrations automatically in production (Render)
        if (!app.Environment.IsDevelopment())
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
        }
        
        // Initialize roles and seed data
        SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// HTTPS redirection - disabled in production (Render handles SSL termination)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
