using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Repositories;
using sql.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = "wwwroot"
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 本機私密設定檔不進 Git。
// Gmail、資料庫或其他敏感設定優先放這裡，避免直接寫進版本庫。
builder.Configuration.AddJsonFile("appsettings.LocalSecrets.json", optional: true, reloadOnChange: true);

// ASP.NET Core DataProtection 預設會把金鑰寫到使用者目錄。
// 這台環境對預設路徑沒有權限，因此改存到專案可控資料夾，避免 Session / Cookie 啟動失敗。
var dataProtectionKeyDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeyDirectory);

// 這裡是 ASP.NET Core 的服務註冊區。
// 可以把它想成先把系統會用到的工具準備好，之後 Controller / Service 需要時框架會自動注入。
builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyDirectory));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Smart Community Reservation API",
        Version = "v1",
        Description = "作品集版本的預約、排隊、帳號與管理後台 API。"
    });
});
builder.Services.Configure<PasswordResetEmailOptions>(
    builder.Configuration.GetSection("PasswordResetEmail"));

// 讓 Service 也能讀到目前這次請求的 HttpContext。
builder.Services.AddHttpContextAccessor();

// 舊資料存取核心目前先保留，避免一次改太多造成風險。
builder.Services.AddScoped<DBmanager>();

// Repository 是新加的資料層邊界。
builder.Services.AddScoped<EquipmentRepository>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<AdminActionLogRepository>();
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
builder.Services.AddScoped<AdminActionLogService>();
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
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"系統發生錯誤\"}");
        }
        else
        {
            context.Response.Redirect("/Home/Error");
        }
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Community Reservation API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
