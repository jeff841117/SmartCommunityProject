using Microsoft.AspNetCore.Identity;

namespace SmartCommunity.Data
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleMgr = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 建立角色
            if (!await roleMgr.RoleExistsAsync("Admin"))
                await roleMgr.CreateAsync(new IdentityRole("Admin"));

            // 建立帳號
            var adminEmail = "admin";
            var admin = await userMgr.FindByNameAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser { UserName = adminEmail };
                await userMgr.CreateAsync(admin, "Admin#12345");
            }

            // 加入角色
            if (!await userMgr.IsInRoleAsync(admin, "Admin"))
                await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }
}