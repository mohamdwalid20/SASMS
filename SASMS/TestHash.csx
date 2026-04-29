using BCrypt.Net;

Console.WriteLine("=== Testing Admin Password Hash ===");
Console.WriteLine();

var password = "Admin@123456";
var storedHash = "$2a$12$exosm0CFgW7RAB9GfeIVH.SykedAUY33HdRkO1K8sJ4OEbbWAt2jO";

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Stored Hash: {storedHash}");
Console.WriteLine();

try
{
    bool isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
    Console.WriteLine($"✅ Hash Verification Result: {isValid}");
    
    if (isValid)
    {
        Console.WriteLine("✅ The hash is CORRECT for the password!");
    }
    else
    {
        Console.WriteLine("❌ The hash is WRONG for the password!");
        Console.WriteLine("Generating a new correct hash...");
        var newHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        Console.WriteLine($"New Hash: {newHash}");
        
        bool newValid = BCrypt.Net.BCrypt.Verify(password, newHash);
        Console.WriteLine($"New Hash Verification: {newValid}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error during verification: {ex.Message}");
}
