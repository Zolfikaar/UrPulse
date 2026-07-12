using UrPulse.Client;

Console.WriteLine("=== Ur Labs: Ur Pulse Heartbeat Test ===");

using var pulseClient = new UrPulseClient(
    serverUrl: "http://localhost:5252",
    appId: "vector-kanban",
    serviceName: "Auth-Service",
    intervalSeconds: 5
);

pulseClient.Start();

Console.WriteLine("Press any key to simulate a CRASH and test server-side escalation...");
Console.ReadKey();

pulseClient.Stop();
Console.WriteLine("Sample stopped. Server countdown started. Alert settings are loaded from UrPulse.Core/appsettings.json.");
Console.WriteLine("After ~15s the service goes Offline; after the configured escalation threshold, alerts fire.");

Console.ReadKey();
