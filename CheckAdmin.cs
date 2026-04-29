using SASMS.Data;
using SASMS.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    Console.WriteLine("=== Checking Admin User in Database ===");
    
    var adminUser = await context.Users
        .Include(u => u.Admin)
        .FirstOrDefaultAsync(u => u.Email == "admin@sasms.edu");
    
    if (adminUser == null)
    {
        Console.WriteLine("❌ Admin user NOT FOUND in database!");
    }
    else
    {
        Console.WriteLine("✅ Admin user FOUND!");
        Console.WriteLine($"   ID: {adminUser.Id}");
        Console.WriteLine($"   Email: {adminUser.Email}");
        Console.WriteLine($"   Role: {adminUser.Role}");
        Console.WriteLine($"   IsActive: {adminUser.IsActive}");
        Console.WriteLine($"   Password Hash: {adminUser.Password}");
        Console.WriteLine();
        
        // Test password verification
        var passwordHasher = new PasswordHasher();
        var testPassword = "Admin@123456";
        
        Console.WriteLine($"Testing password: {testPassword}");
        bool isValid = passwordHasher.VerifyPassword(testPassword, adminUser.Password);
        Console.WriteLine($"Password verification result: {isValid}");
        
        if (!isValid)
        {
            Console.WriteLine();
            Console.WriteLine("❌ PASSWORD VERIFICATION FAILED!");
            Console.WriteLine("Generating new hash for testing...");
            var newHash = passwordHasher.HashPassword(testPassword);
            Console.WriteLine($"New hash: {newHash}");
            
            // Test the new hash
            bool newHashValid = passwordHasher.VerifyPassword(testPassword, newHash);
            Console.WriteLine($"New hash verification: {newHashValid}");
        }
        else
        {
            Console.WriteLine("✅ PASSWORD VERIFICATION SUCCESSFUL!");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("=== All Users in Database ===");
    var allUsers = await context.Users.ToListAsync();
    Console.WriteLine($"Total users: {allUsers.Count}");
    foreach (var user in allUsers)
    {
        Console.WriteLine($"  - {user.Email} (Role: {user.Role}, Active: {user.IsActive})");
    }
}

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
