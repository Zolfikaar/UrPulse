using UrPulse.Core.Models;
using UrPulse.Core.Services;
using Microsoft.EntityFrameworkCore;
using UrPulse.Core.Data;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UrPulseDbContext>(options =>
    options.UseSqlite("Data Source=UrPulseHealth.db"));
// Add services to the container.

builder.Services.AddOpenApi();

// Unified global settings provider (persists UrPulseSettings to appsettings.json)
builder.Services.AddSingleton<IUrPulseSettingsProvider, LocalJsonSettingsProvider>();

// PulseEngine as Singleton — stays alive for the process lifetime
builder.Services.AddSingleton<PulseEngine>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNuxtFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowNuxtFrontend");

// Ensure Engine starts its monitor timer immediately
app.Services.GetRequiredService<PulseEngine>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.MapPost("/api/pulse/heartbeat", (HeartbeatPulse pulse, PulseEngine engine) =>
{
    if (string.IsNullOrWhiteSpace(pulse.AppId))
    {
        return Results.BadRequest("AppId is required.");
    }

    pulse.Timestamp = DateTime.UtcNow;
    engine.ProcessPulse(pulse);

    return Results.Ok(new { Message = "Pulse received successfully." });
});

// Public client timing contract — no secrets. Monitored apps must follow these values.
app.MapGet("/api/pulse/client-config", (PulseEngine engine) =>
{
    return Results.Ok(new
    {
        heartbeatIntervalSeconds = engine.GetHeartbeatIntervalSeconds(),
        offlineThresholdSeconds = engine.GetOfflineThresholdSeconds(),
        escalationDelaySeconds = engine.GetEscalationDelaySeconds(),
        scanIntervalSeconds = engine.GetTimerIntervalSeconds()
    });
});

app.MapGet("/api/pulse/status", (PulseEngine engine) =>
{
    return Results.Ok(engine.GetAllStatuses());
});

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

// Unified global system + alerting configuration
app.MapGet("/api/settings/system", async (IUrPulseSettingsProvider settingsProvider, PulseEngine engine) =>
{
    var settings = await settingsProvider.GetSettingsAsync();

    // Prefer live engine values for runtime-tuned thresholds
    settings.GlobalHeartbeatIntervalSeconds = engine.GetHeartbeatIntervalSeconds();
    settings.GlobalOfflineThresholdSeconds = engine.GetOfflineThresholdSeconds();
    settings.GlobalScanIntervalSeconds = engine.GetTimerIntervalSeconds();
    settings.GlobalEscalationDelaySeconds = engine.GetEscalationDelaySeconds();

    return Results.Ok(settings);
});

app.MapPost("/api/settings/system", async (UrPulseSettings model, IUrPulseSettingsProvider settingsProvider, PulseEngine engine) =>
{
    if (model.GlobalHeartbeatIntervalSeconds < 5 || model.GlobalHeartbeatIntervalSeconds > 60)
    {
        return Results.BadRequest("GlobalHeartbeatIntervalSeconds must be between 5 and 60.");
    }

    if (model.GlobalOfflineThresholdSeconds < 5 || model.GlobalOfflineThresholdSeconds > 300)
    {
        return Results.BadRequest("GlobalOfflineThresholdSeconds must be between 5 and 300.");
    }

    if (model.GlobalScanIntervalSeconds < 1 || model.GlobalScanIntervalSeconds > 60)
    {
        return Results.BadRequest("GlobalScanIntervalSeconds must be between 1 and 60.");
    }

    if (model.GlobalEscalationDelaySeconds < 0 || model.GlobalEscalationDelaySeconds > 600)
    {
        return Results.BadRequest("GlobalEscalationDelaySeconds must be between 0 and 600.");
    }

    if (model.GlobalHeartbeatIntervalSeconds >= model.GlobalOfflineThresholdSeconds)
    {
        return Results.BadRequest("GlobalHeartbeatIntervalSeconds must be less than GlobalOfflineThresholdSeconds.");
    }

    model.Telegram ??= new TelegramSettings();
    model.Twilio ??= new TwilioSettings();

    var success = await settingsProvider.SaveSettingsAsync(model);
    if (!success)
    {
        return Results.Problem("Failed to persist UrPulseSettings to storage.");
    }

    engine.ApplyRuntimeTuning(
        model.GlobalHeartbeatIntervalSeconds,
        model.GlobalOfflineThresholdSeconds,
        model.GlobalScanIntervalSeconds,
        model.GlobalEscalationDelaySeconds);

    return Results.Ok(new
    {
        message = "Global system & alerting configuration saved.",
        settings = model
    });
});

app.Run();
