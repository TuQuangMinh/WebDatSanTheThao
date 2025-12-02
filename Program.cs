using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using web.Models;
using web.Repository;
using web.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 3;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Register repositories
builder.Services.AddScoped<ISanRepository, EFSanRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IBookingSlotRepository, BookingSlotRepository>();

// Configure EmailService
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<EmailService>();

// Add session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Khởi tạo vai trò và tài khoản admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting role and admin user initialization...");

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Tạo vai trò Admin và User nếu chưa tồn tại
        string[] roleNames = { "Admin", "User" };

        foreach (var roleName in roleNames)
        {
            logger.LogInformation("Checking if role {RoleName} exists...", roleName);
            var roleExist = await roleManager.RoleExistsAsync(roleName);

            if (!roleExist)
            {
                logger.LogInformation("Creating role {RoleName}...", roleName);
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));

                if (result.Succeeded)
                {
                    logger.LogInformation("Role {RoleName} created successfully", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role {RoleName}. Errors: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Role {RoleName} already exists", roleName);
            }
        }

        // Tạo tài khoản Admin mặc định
        var adminEmail = "admin@example.com";
        logger.LogInformation("Checking if admin user exists with email: {AdminEmail}", adminEmail);

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            logger.LogInformation("Creating admin user...");

            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Name = "Administrator",
                EmailConfirmed = true,
                StreetAddress = "Admin Street",
                City = "Admin City",
                State = "Admin State",
                PostalCode = "00000"
            };

            var createUser = await userManager.CreateAsync(user, "Admin@123");

            if (createUser.Succeeded)
            {
                logger.LogInformation("Admin user created successfully");

                var addToRoleResult = await userManager.AddToRoleAsync(user, "Admin");
                if (addToRoleResult.Succeeded)
                {
                    logger.LogInformation("Admin role assigned to admin user successfully");
                }
                else
                {
                    logger.LogError("Failed to assign Admin role to admin user. Errors: {Errors}",
                        string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("Failed to create admin user. Errors: {Errors}",
                    string.Join(", ", createUser.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Admin user already exists");

            // Kiểm tra xem admin user đã có role Admin chưa
            var isInRole = await userManager.IsInRoleAsync(adminUser, "Admin");
            if (!isInRole)
            {
                logger.LogInformation("Adding Admin role to existing admin user...");
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");

                if (addToRoleResult.Succeeded)
                {
                    logger.LogInformation("Admin role assigned to existing admin user successfully");
                }
                else
                {
                    logger.LogError("Failed to assign Admin role to existing admin user. Errors: {Errors}",
                        string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        logger.LogInformation("Role and admin user initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database: {Message}", ex.Message);

        // Log inner exception details if available
        if (ex.InnerException != null)
        {
            logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages before controller routes
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=San}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "error",
    pattern: "Home/Error/{statusCode?}",
    defaults: new { controller = "Home", action = "Error" });

app.Run();