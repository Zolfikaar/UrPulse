# Ur Pulse (Unified Health & Heartbeat Monitor)

**Ur Pulse** is a lightweight, high-performance, and cross-platform infrastructure monitoring system built under the **UrLabs** ecosystem. It is designed to act as a centralized "Dead Man's Switch" to track the availability, health, and resource utilization of multiple distributed applications and microservices in real-time.

---

## 🏗️ System Architecture & Framework

The system is engineered using **.NET 8/10** with strict adherence to clean architecture principles, separation of concerns, and non-blocking asynchronous I/O operations.

[ Ur Pulse Ecosystem ]
                              │
     ┌────────────────────────┼────────────────────────┐
     ▼                        ▼                        ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   UrPulse.Core   │    │  UrPulse.Client  │    │  UrPulse.Sample  │
│  (Central Engine)│    │   (Lightweight)  │    │  (Stress Tester) │
└──────────────────┘    └──────────────────┘    └──────────────────┘


### 1. **UrPulse.Core (The Central Brain)**
* Built as a high-throughput **Minimal API**.
* Manages runtime state tracking using thread-safe `ConcurrentDictionary` storage.
* Implements a background telemetry sweeper (`System.Threading.Timer`) evaluating status cadences every 5 seconds.
* Mitigates **Time Drift / Race Conditions** by baselining timestamps upon server ingress.

### 2. **UrPulse.Client (The SDK Pack)**
* A decoupled Class Library designed to be dropped into any client application as an isolated background worker.
* Performs non-blocking, asynchronous telemetry dispatching.
* Collects live infrastructure metrics (`Process.GetCurrentProcess()`) including accurate Private Memory (RAM) footprints and CPU Execution Time.

---

## 🚦 Lifecycle States & Multi-Channel Escalation

The engine evaluates systems into three definitive state categories:
* 🟢 **Healthy:** Telemetry is fluid, and hardware resource boundaries are respected.
* 🟡 **Degraded:** The service is alive, but self-reported health evaluation exceeds custom thresholds (e.g., RAM memory leaks).
* 🔴 **Offline:** Heartbeat cadence times out (Default: 15s).

### ⚡ Premium Cascading Escalation Policy
When a service enters a prolonged `Offline` state (exceeding 60 seconds), the core activates a dynamic escalation pipeline configuration:
1. **Local Audio Node:** Triggers immediate motherboard frequency alerts (`Console.Beep`).
2. **Telegram Notification Channel:** Dispatches rich markdown payloads containing precise environment metrics directly to the engineering group.
3. **VoIP Voice Telephony (Twilio Integration):** Initiates an automated cellular phone call via Twilio's API, executing dynamic TwiML text-to-speech protocols to verbally report the critical incident.

---

## ⚙️ Configuration & Customization

The monitoring payload is fully configurable on a per-app basis. Users can toggle alert systems and provide tailored alerting metadata:

```json
{
  "AppId": "vector-kanban",
  "ServiceName": "Auth-Service",
  "Status": "Healthy",
  "Alerts": {
    "EnableAlerts": true,
    "EscalationThresholdSeconds": 60,
    "EnableLoudAudioAlert": true,
    "EnableTelegramAlert": true,
    "TelegramBotToken": "YOUR_BOT_TOKEN",
    "TelegramChatId": "YOUR_CHAT_ID",
    "EnableVoiceCallAlert": true,
    "TwilioAccountSid": "YOUR_ACCOUNT_SID",
    "TwilioAuthToken": "YOUR_AUTH_TOKEN",
    "TwilioFromNumber": "+123456789",
    "TargetPhoneNumber": "+9647xxxxxxx",
    "CustomVoiceMessage": "Attention! System crisis detected on authorization nodes."
  }
}
🚀 Quick Start & Installation
To boot up the ecosystem locally for sanity checks or stress testing, execute the following commands in order:

Bash
# 1. Clone the repository
git clone [https://github.com/your-username/UrPulse.git](https://github.com/your-username/UrPulse.git)
cd UrPulse

# 2. Restore and Build the Solution
dotnet build

# 3. Run the Central Core Server
dotnet run --project src/UrPulse.Core

# 4. Open a separate terminal and run the Sample Stress App
dotnet run --project src/UrPulse.Sample
Once running, monitor active system registration state arrays in your browser via:
http://localhost:5252/api/pulse/status

Developed with passion under UrLabs Studio open specifications.