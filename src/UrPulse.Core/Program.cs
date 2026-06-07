using UrPulse.Core.Models;
using UrPulse.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<UrPulse.Core.Services.PulseEngine>();
var app = builder.Build();

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

app.Run();

