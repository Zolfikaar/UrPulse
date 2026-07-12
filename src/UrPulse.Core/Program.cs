using UrPulse.Core.Models;
using UrPulse.Core.Services;
using Microsoft.EntityFrameworkCore;
using UrPulse.Core.Data;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UrPulseDbContext>(options =>
    options.UseSqlite("Data Source=UrPulseHealth.db"));
// Add services to the container.

builder.Services.AddOpenApi();

// 1. تسجيل الـ Secret Provider (محاكي الخزنة المعتمد على الـ JSON)
builder.Services.AddSingleton<ISecretProvider, LocalJsonSecretProvider>();

// 2. تسجيل الـ PulseEngine كـ Singleton ليبقى حياً طوال فترة تشغيل السيرفر
builder.Services.AddSingleton<PulseEngine>();

// للسماح بتطبيق Nuxt باللإتصال مع السيرفر و تجاوز ال CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNuxtFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3000/") // منفذ Nuxt الشهير
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowNuxtFrontend");

// تأكد من استدعاء الـ Engine عند إقلاع السيرفر ليبدأ التايمر بالعمل فوراً
app.Services.GetRequiredService<PulseEngine>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

// 1. نقطة استقبال النبضات من المشاريع (POST)
app.MapPost("/api/pulse/heartbeat", (HeartbeatPulse pulse, PulseEngine engine) =>
{
    if (string.IsNullOrWhiteSpace(pulse.AppId))
    {
        return Results.BadRequest("AppId is required.");
    }

    pulse.Timestamp = DateTime.UtcNow; // توثيق وقت الاستلام على السيرفر
    engine.ProcessPulse(pulse);

    return Results.Ok(new { Message = "Pulse received successfully." });
});

// 2. نقطة استعراض لوحة التحكم (GET) لرؤية حالة كل المشاريع الحالية
app.MapGet("/api/pulse/status", (PulseEngine engine) =>
{
    return Results.Ok(engine.GetAllStatuses());
});

// 3. Endpoint لجلب كامل السجل التاريخي لجميع التطبيقات (معاينة محدودة للوحة التحكم)
app.MapGet("/api/pulse/logs", async (UrPulseDbContext dbContext) =>
{
    try
    {
        var logs = await dbContext.HealthLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(20)
            .ToListAsync();

        return Results.Ok(logs);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to retrieve health logs: {ex.Message}");
    }
});

// 4. Endpoint للسجلات مع Pagination من جهة السيرفر (يجب تسجيله قبل {appId})
app.MapGet("/api/pulse/logs/paginated", async (int? page, int? pageSize, UrPulseDbContext dbContext) =>
{
    try
    {
        var currentPage = Math.Max(1, page ?? 1);
        var size = Math.Clamp(pageSize ?? 20, 1, 100);

        var totalCount = await dbContext.HealthLogs.CountAsync();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)size);

        var logs = await dbContext.HealthLogs
            .OrderByDescending(l => l.Timestamp)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync();

        return Results.Ok(new
        {
            totalCount,
            page = currentPage,
            pageSize = size,
            totalPages,
            logs
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to retrieve paginated health logs: {ex.Message}");
    }
});

// 5. Endpoint لجلب السجل التاريخي لتطبيق محدد (مثال: vector-kanban)
app.MapGet("/api/pulse/logs/{appId}", async (string appId, UrPulseDbContext dbContext) =>
{
    try
    {
        var appLogs = await dbContext.HealthLogs
            .Where(l => l.AppId == appId)
            .OrderByDescending(l => l.Timestamp)
            .Take(50)
            .ToListAsync();

        return Results.Ok(appLogs);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to retrieve logs for app {appId}: {ex.Message}");
    }
});

// 6. Endpoint لجلب إعدادات تطبيق معين من الخزنة (لتعبئة حقول الواجهة)
app.MapGet("/api/vault/settings/{appId}", async (string appId, ISecretProvider secretProvider) =>
{
    var settings = await secretProvider.GetAlertSettingsAsync(appId);
    if (settings == null)
    {
        // إذا لم تكن هناك إعدادات، نعيد كائن فارغ افتراضي لتسهيل التعامل في الفرونت إند
        return Results.Ok(new AlertSettings { EnableAlerts = false });
    }
    return Results.Ok(settings);
});

// 7. Endpoint لتحديث أو حفظ إعدادات تطبيق داخل الخزنة
app.MapPost("/api/vault/settings/{appId}", async (string appId, AlertSettings newSettings, ISecretProvider secretProvider) =>
{
    if (string.IsNullOrWhiteSpace(appId))
    {
        return Results.BadRequest("Invalid Application ID.");
    }

    var success = await secretProvider.SaveAlertSettingsAsync(appId, newSettings);

    if (success)
    {
        return Results.Ok(new { message = $"Vault settings updated successfully for '{appId}'." });
    }

    return Results.Problem("Failed to write settings to the secure distribution vault storage.");
});

// 8. Unified system settings — offline deadline + monitor scan interval
app.MapGet("/api/settings/system", (PulseEngine engine) =>
{
    return Results.Ok(new
    {
        thresholdSeconds = engine.GetOfflineThresholdSeconds(),
        intervalSeconds = engine.GetTimerIntervalSeconds()
    });
});

app.MapPost("/api/settings/system", (SystemSettingsModel model, PulseEngine engine) =>
{
    if (model.ThresholdSeconds < 5 || model.ThresholdSeconds > 300)
    {
        return Results.BadRequest("ThresholdSeconds must be between 5 and 300.");
    }

    if (model.IntervalSeconds < 1 || model.IntervalSeconds > 60)
    {
        return Results.BadRequest("IntervalSeconds must be between 1 and 60.");
    }

    engine.UpdateOfflineThresholdSeconds(model.ThresholdSeconds);
    engine.UpdateTimerIntervalSeconds(model.IntervalSeconds);

    return Results.Ok(new
    {
        message = "System settings updated successfully.",
        thresholdSeconds = model.ThresholdSeconds,
        intervalSeconds = model.IntervalSeconds
    });
});


app.Run();

public record SystemSettingsModel(int ThresholdSeconds, int IntervalSeconds);
