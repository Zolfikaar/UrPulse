using System.Diagnostics;
using System.Net.Http.Json;

namespace UrPulse.Client;

public class UrPulseClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _appId;
    private readonly string _serviceName;
    private readonly TimeSpan _interval;
    private Timer? _timer;
    private readonly Process _currentProcess;
    private readonly double _maxMemoryHealthyMb;

    public UrPulseClient(
        string serverUrl,
        string appId,
        string serviceName = "General",
        int intervalSeconds = 10,
        double maxMemoryHealthyMb = 100.0)
    {
        _httpClient = new HttpClient();
        _serverUrl = serverUrl.TrimEnd('/');
        _appId = appId;
        _serviceName = serviceName;
        _interval = TimeSpan.FromSeconds(intervalSeconds);
        _currentProcess = Process.GetCurrentProcess();
        _maxMemoryHealthyMb = maxMemoryHealthyMb;
    }

    public void Start()
    {
        if (_timer != null) return;
        _timer = new Timer(async _ => await SendHeartbeatAsync(), null, TimeSpan.Zero, _interval);
        Console.WriteLine($"[Ur Pulse Client] 🚀 Started monitoring for {_appId}:{_serviceName} (RAM Limit: {_maxMemoryHealthyMb}MB)...");
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
    }

    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
        _currentProcess.Dispose();
    }
}
