using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Repositories;
using sql.Services;

var builder = WebApplication.CreateBuilder(args);

// ?????????????????????? Git?
// ?? Gmail ????????????????
builder.Configuration.AddJsonFile("appsettings.LocalSecrets.json", optional: true, reloadOnChange: true);

// 這裡是 ASP.NET Core 的服務註冊區。
// 可以把它想成先把系統會用到的工具準備好，
// 之後 Controller / Service 需要時，框架會自動注入。
builder.Services.AddControllersWithViews();
builder.Services.Configure<PasswordResetEmailOptions>(
    builder.Configuration.GetSection("PasswordResetEmail"));

// 讓 Service 也能讀到目前這次請求的 HttpContext。
// 我們後面用它來集中取得目前登入者資訊，而不是每個 Controller 自己讀 Session。
builder.Services.AddHttpContextAccessor();

// 舊資料存取核心目前先保留，避免一次改太多造成風險。
builder.Services.AddScoped<DBmanager>();

// Repository 是新加的資料層邊界。
// 目的不是一次取代 DBmanager，而是把各模組的資料操作慢慢搬出來。
builder.Services.AddScoped<EquipmentRepository>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<ReservationRepository>();
builder.Services.AddScoped<QueueRepository>();

// 目前登入者資訊統一由這個 Service 整理。
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<EquipmentStateNotifier>();
builder.Services.AddScoped<QueueProcessingCoordinator>();
builder.Services.AddScoped<PasswordResetEmailBridge>();

// Service 層負責業務流程，Repository 層負責資料存取。
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<QueueService>();
builder.Services.AddScoped<FutureReservationPlanningService>();

// 背景服務：固定時間檢查是否有過期預約需要自動結束。
builder.Services.AddHostedService<ExpiredReservationCheckerService>();

if (!builder.Environment.IsDevelopment())
{
    // 正式環境不要直接把太細的驗證錯誤暴露給使用者。
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });
}

// Session 可以想成「目前登入者的臨時身分卡」。
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 閒置 30 分鐘後過期
    options.Cookie.HttpOnly = true; // 防止前端 JavaScript 直接讀 Cookie
    options.Cookie.IsEssential = true; // 登入功能屬必要功能
});

var app = builder.Build();

// 下面開始設定請求處理管線。
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 全域例外處理：避免錯誤直接炸到使用者畫面上。
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "發生未處理的異常");

        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            // AJAX / API 請求回 JSON，方便前端程式處理。
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"系統發生錯誤\"}");
        }
        else
        {
            // 一般頁面請求導到錯誤頁面。
            context.Response.Redirect("/Home/Error");
        }
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Session 要放在路由之後，Controller 執行時才讀得到登入資訊。
app.UseSession();

// 預設先進登入頁。
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
