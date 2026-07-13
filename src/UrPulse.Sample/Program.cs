using UrPulse.Client;

Console.WriteLine("=== Ur Labs: Ur Pulse Heartbeat Test ===");

using var pulseClient = new UrPulseClient(
    serverUrl: "http://localhost:5252",
    appId: "vector-kanban",
    serviceName: "Auth-Service",
    intervalSeconds: 10 // fallback only — Core GET /api/pulse/client-config wins
);

pulseClient.Start();

Console.WriteLine("Press any key to simulate a CRASH and test server-side escalation...");
Console.ReadKey();

pulseClient.Stop();
Console.WriteLine("Sample stopped. Timing is controlled by UrPulse Core settings:");
Console.WriteLine("  1) Offline after GlobalOfflineThresholdSeconds of silence");
Console.WriteLine("  2) Escalation (Telegram/Twilio/beep) after GlobalEscalationDelaySeconds offline");

Console.ReadKey();
