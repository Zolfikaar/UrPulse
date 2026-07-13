using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace UrPulse.Client;

public class UrPulseClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _appId;
    private readonly string _serviceName;
    private TimeSpan _interval;
    private Timer? _timer;
    private Timer? _configTimer;
    private readonly Process _currentProcess;
    private readonly double _maxMemoryHealthyMb;
    private readonly bool _syncIntervalFromCore;

    /// <param name="intervalSeconds">
    /// Fallback only. When <paramref name="syncIntervalFromCore"/> is true (default),
    /// Core's GlobalHeartbeatIntervalSeconds from GET /api/pulse/client-config wins.
    /// </param>
    public UrPulseClient(
        string serverUrl,
        string appId,
        string serviceName = "General",
        int intervalSeconds = 10,
        double maxMemoryHealthyMb = 100.0,
        bool syncIntervalFromCore = true)
    {
        _httpClient = new HttpClient();
        _serverUrl = serverUrl.TrimEnd('/');
        _appId = appId;
        _serviceName = serviceName;
        _interval = TimeSpan.FromSeconds(Math.Clamp(intervalSeconds, 5, 60));
        _currentProcess = Process.GetCurrentProcess();
        _maxMemoryHealthyMb = maxMemoryHealthyMb;
        _syncIntervalFromCore = syncIntervalFromCore;
    }

    public void Start()
    {
        if (_timer != null) return;

        if (_syncIntervalFromCore)
        {
            SyncIntervalFromCoreAsync().GetAwaiter().GetResult();
            _configTimer = new Timer(async _ => await SyncIntervalFromCoreAsync(), null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        }

        _timer = new Timer(async _ => await SendHeartbeatAsync(), null, TimeSpan.Zero, _interval);
        Console.WriteLine($"[Ur Pulse Client] 🚀 Started {_appId}:{_serviceName} every {_interval.TotalSeconds:0}s (RAM Limit: {_maxMemoryHealthyMb}MB)...");
    }

    private async Task SyncIntervalFromCoreAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/pulse/client-config");
            if (!response.IsSuccessStatusCode) return;

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (!doc.RootElement.TryGetProperty("heartbeatIntervalSeconds", out var prop)) return;

            var seconds = Math.Clamp(prop.GetInt32(), 5, 60);
            var next = TimeSpan.FromSeconds(seconds);
            if (next == _interval) return;

            _interval = next;
            _timer?.Change(TimeSpan.Zero, _interval);
            Console.WriteLine($"[Ur Pulse Client] ⚙️ Heartbeat interval synced from Core: {seconds}s");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ur Pulse Client] ⚠️ Could not sync client-config: {ex.Message}");
        }
    }

    private string EvaluateStatus(double currentMemoryMb)
    {
        if (currentMemoryMb > _maxMemoryHealthyMb)
        {
            return "Degraded";
        }

        return "Healthy";
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            _currentProcess.Refresh();

            double memoryUsedMb = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
            TimeSpan cpuTime = _currentProcess.TotalProcessorTime;
            string currentStatus = EvaluateStatus(memoryUsedMb);

            var pulse = new
            {
                AppId = _appId,
                ServiceName = _serviceName,
                Status = currentStatus,
                Metadata = new Dictionary<string, string>
                {
                    { "MachineName", Environment.MachineName },
                    { "OS", Environment.OSVersion.ToString() },
                    { "MemoryUsage_MB", memoryUsedMb.ToString("F2") },
                    { "MemoryLimit_MB", _maxMemoryHealthyMb.ToString("F2") },
                    { "TotalCpuTime_Sec", cpuTime.TotalSeconds.ToString("F2") },
                    { "DotNetVersion", Environment.Version.ToString() }
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/pulse/heartbeat", pulse);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Ur Pulse Client] ⚠️ Failed to send heartbeat. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ur Pulse Client] ❌ Error sending heartbeat: {ex.Message}");
        }
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        _timer = null;
        _configTimer?.Dispose();
        _configTimer = null;
    }

    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
        _currentProcess.Dispose();
    }
}
