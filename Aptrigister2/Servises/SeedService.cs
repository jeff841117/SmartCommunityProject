using Aptrigister2.Models;
using Microsoft.AspNetCore.Identity;
using Aptrigister2.Data;

namespace Aptrigister2.Servises
{
    public class SeedService
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();

            try
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.EnsureCreatedAsync();

                logger.LogInformation("Database  applied successfully.");
                await SeedService.AddRoleAsync(roleManager, "Admin");
                await SeedService.AddRoleAsync(roleManager, "User");

                logger.LogInformation("Checking Admin user...");
                var adminEmail = "admin@community.com";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var adminUser = new Users
                    {
                        FullName = "System Administrator",
                        UserName = adminEmail, 
                        NormalizedUserName=adminEmail.ToUpper(),
                        Email = adminEmail,
                        NormalizedEmail=adminEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        
                    };
                    var result = await userManager.CreateAsync(adminUser, "Admin/12345");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Admin user created and assigned to Admin role.");
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }         
                }
                else
                {
                    logger.LogInformation("Admin user already exists.");
                }
            

            }
            catch (Exception ex) {
                logger.LogError(ex, "An error occurred while applying database ");
                
            }
      
        }

        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
