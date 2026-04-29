using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<DBmanager>();
// 添加背景服務來定期檢查過期預約
builder.Services.AddHostedService<ExpiredReservationCheckerService>();
// 在生產環境中禁用詳細錯誤
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });
}

// 添加 Session 服務
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session 過期時間
    options.Cookie.HttpOnly = true; // 防止 XSS 攻擊
    options.Cookie.IsEssential = true; // 即使用戶不同意也要使用
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // 記錄錯誤但不顯示詳細信息給用戶
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "發生未處理的異常");

        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            // AJAX 請求返回 JSON 錯誤
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"系統發生錯誤\"}");
        }
        else
        {
            // 頁面請求重定向到錯誤頁面
            context.Response.Redirect("/Home/Error");
        }
    }
});
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
// 使用 Session 中間件（必須在 UseRouting 之後，UseEndpoints 之前）
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
