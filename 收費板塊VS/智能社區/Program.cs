using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartCommunity.Data;
using SmartCommunity.Models;

var builder = WebApplication.CreateBuilder(args);

// ====== 註冊服務 ======
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// DbContext
builder.Services.AddDbContext<SmartCommunityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartCommunity")));

// Identity：使用預設的 IdentityUser（帳號系統）
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        // 如需簡單密碼，放寬規則（開發用；正式環境請用強密碼）
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 3;
    })
    .AddEntityFrameworkStores<SmartCommunityContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// ====== 建立 app ======
var app = builder.Build();

// ====== Middleware ======
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

// ====== 路由 ======
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// （可選）讓 /Bills/Create 也能開到 Admin 的新增頁，避免 404
app.MapControllerRoute(
    name: "bills-create-alias",
    pattern: "Bills/Create",
    defaults: new { controller = "AdminBills", action = "Create" });

// ====== 啟動時建立角色與預設管理者（並可同步建立住戶資料） ======
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<SmartCommunityContext>();
        await context.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // 從 appsettings.json 讀取設定
        var adminConfig = builder.Configuration.GetSection("Seed:Admin");
        var adminEmail = adminConfig["Email"] ?? "admin@local";
        var adminPass = adminConfig["Password"] ?? "Admin!23456";

        // 1) 角色
        const string AdminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdminRole));
            if (!roleResult.Succeeded)
                logger.LogError("建立角色 {Role} 失敗：{Err}", AdminRole,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        // 2) 管理者帳號
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail, // 這裡用 Email 當帳號；若要顯示姓名可另外擴充或用住戶表
                Email = adminEmail,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(adminUser, adminPass);
            if (!createResult.Succeeded)
            {
                logger.LogError("建立管理者失敗：{Err}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }

        // 3) 加入 Admin 角色
        if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, AdminRole))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, AdminRole);
            if (!addRoleResult.Succeeded)
            {
                logger.LogError("管理者加入角色失敗：{Err}",
                    string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
            }
        }

        // 4) （可選）同步建立一筆住戶資料，關聯到 Admin 帳號
        //    注意：若你的 DbContext 中有 Identity 的 Users 與你自訂 Users 同名衝突，
        //    建議把自訂的 Users DbSet 改名為 Residents，這裡也請相應修改 Set<T>()。
        if (adminUser != null)
        {
            // 用 Set<T>() 取代 context.Users，避免與 Identity 的 Users 衝突
            var residents = context.Set<SmartCommunity.Models.User>();
            var existsResident = await residents.AnyAsync(r => r.AspNetUserId == adminUser.Id);
            if (!existsResident)
            {
                residents.Add(new SmartCommunity.Models.User
                {
                    AspNetUserId = adminUser.Id,
                    UserName = "系統管理員",
                    RoomNumber = "N/A",
                    Email = adminEmail,
                    Phone = null
                });
                await context.SaveChangesAsync();
            }
        }
    }
    catch (Exception ex)
    {
        var logger2 = services.GetRequiredService<ILogger<Program>>();
        logger2.LogError(ex, "初始化角色/管理者/住戶資料時發生錯誤");
    }
}

app.Run();