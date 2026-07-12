# Ur Pulse

**UrLabs · Centralized Dead Man's Switch & Infrastructure Heartbeat Monitor**

Ur Pulse is a production-oriented, cross-platform monitoring system for distributed applications and microservices. It provides a centralized registry of service liveness, self-reported health degradation, asynchronous multi-channel escalation, and durable historical uptime logging—backed by a server-side secret engine so monitored clients never handle alert credentials.

The design prioritizes **separation of concerns**, **telemetry-only client transport**, **server-authoritative timestamps**, **out-of-band secret resolution**, and **non-blocking persistence and notification dispatch** so the central engine remains responsive under alert and ingest load.

---

## 1. Project Overview & Philosophy

Distributed systems fail silently: a process may hang without crashing, a host may lose network egress, or a deployment may stop emitting logs while remaining unreachable. Ur Pulse addresses this class of failure with a **Dead Man's Switch** model—each monitored service periodically asserts its presence. Absence of assertion within a bounded window is treated as an **Offline** condition and may trigger escalation.

Beyond binary liveness, Ur Pulse supports **self-reported degradation**. Client applications evaluate local resource constraints (e.g., private working-set memory ceilings) and may report a `Degraded` evaluation in the telemetry envelope while still emitting heartbeats. This separates *"the process is alive but under resource pressure"* from *"the process has stopped reporting entirely."*

Core principles:

| Principle | Implementation |
|-----------|----------------|
| **Telemetry-only Client SDK** | The Client transmits identity, status, and hardware metadata only. It does **not** carry, embed, or transfer Telegram/Twilio tokens or alert policy. |
| **Out-of-band secret resolution** | The Core asynchronously resolves per-`AppId` alert configuration from a centralized simulated vault (`ISecretProvider` → `UrVaultSimulation` in `appsettings.json`). |
| **Server-authoritative time** | Heartbeat receipt time is stamped at ingress with `DateTime.UtcNow`, eliminating client clock skew from liveness calculations. |
| **Decoupled SDK** | Monitoring logic ships as a standalone client library; the central engine has no dependency on consumer applications. |
| **Non-blocking escalation & logging** | Audio, Telegram, Twilio, and EF Core health-log writes execute on background tasks; the health sweeper never awaits external I/O or DbContext contention on the timer thread. |
| **Durable uptime history** | State transitions (`Healthy`, `Offline`) are ingested into a local SQLite store via a thread-safe scoped EF Core pipeline for historical metrics. |

**Solution layout:** `UrPulse.slnx` / `UrPulse.sln` — three projects under `src/`, targeting **.NET 10**.

---

## 2. Architectural Layout

```
┌─────────────────────────────────────────────────────────────────┐
│                     Monitored Applications                       │
│   (any .NET host referencing UrPulse.Client)                   │
│   Telemetry only: AppId, ServiceName, Status, Metadata         │
│   — no tokens, no AlertSettings on the wire —                  │
└────────────────────────────┬────────────────────────────────────┘
                             │ POST /api/pulse/heartbeat
                             │ (JSON HeartbeatPulse; credentials omitted)
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  UrPulse.Core — Minimal API Host (Central Engine)               │
│  · PulseEngine (singleton)                                      │
│  · ConcurrentDictionary state store                             │
│  · Background health sweeper (5 s cadence)                      │
│  · ISecretProvider / LocalJsonSecretProvider (UrVaultSimulation)│
│  · Escalation orchestration (Telegram, Twilio, local audio)     │
│  · EF Core + SQLite health-log pipeline (IServiceScopeFactory)  │
└───────────────┬─────────────────────────────┬───────────────────┘
                │                             │
                │ GET /api/pulse/status       │ GET /api/pulse/logs[/{appId}]
                │                             │ UrPulseHealth.db
                ▼                             ▼
┌───────────────────────────┐   ┌─────────────────────────────────┐
│ Operators / Dashboards    │   │ Historical uptime & metrics     │
└───────────────────────────┘   └─────────────────────────────────┘
```

### UrPulse.Core — Central Engine

| Responsibility | Detail |
|----------------|--------|
| **Ingress** | Accepts telemetry-only heartbeat payloads via `POST /api/pulse/heartbeat`. |
| **State registry** | Maintains the authoritative in-memory view of registered services in a `ConcurrentDictionary`. |
| **Liveness adjudication** | Evaluates silence duration against a fixed 15-second offline threshold on a recurring timer. |
| **Secret engine emulation** | Resolves Twilio/Telegram/audio policy **asynchronously and out-of-band** via `ISecretProvider` keyed by `AppId`. Credentials never arrive from clients. |
| **Escalation** | Invokes configured alert channels after sustained offline duration exceeds the vault-defined escalation threshold. |
| **Historical logging** | Persists `Healthy` and `Offline` transitions to SQLite through a background, scoped EF Core write path. |
| **Observability** | Exposes live status and historical logs via status/logs Minimal API endpoints. |

Built as an ASP.NET Core **Minimal API** with OpenAPI support in Development. Runtime state remains in-process for low-latency adjudication; durability for uptime analytics is provided by the SQLite persistence layer (`UrPulseHealth.db`).

### UrPulse.Client — Telemetry-Only SDK

| Responsibility | Detail |
|----------------|--------|
| **Background heartbeat emitter** | `System.Threading.Timer`-driven dispatch at a configurable interval (default 10 s). |
| **Local health evaluation** | Samples `Process.GetCurrentProcess()` for private memory and CPU time; derives `Healthy` vs. `Degraded` for the telemetry envelope. |
| **Telemetry envelope** | Constructs a JSON payload containing **only** `AppId`, `ServiceName`, `Status`, and `Metadata`. |
| **Transport** | Asynchronous `HttpClient.PostAsJsonAsync` to the Core ingress endpoint. |

The Client is a **class library with zero credential surface**. Constructor parameters are limited to server URL, application identity, interval, and local memory ceiling. Consumer applications instantiate `UrPulseClient`, call `Start()`, and optionally `Stop()`/`Dispose()` on shutdown. This boundary ensures monitored services remain deployable artifacts that cannot leak or misuse alert secrets—even if a client process is compromised or its traffic is inspected.

### UrPulse.Sample — Reference Integration

| Responsibility | Detail |
|----------------|--------|
| **End-to-end validation** | Demonstrates telemetry-only SDK wiring against a local Core instance. |
| **Crash simulation** | Starts heartbeats, waits for operator input, then stops the client to provoke offline detection and Core-side escalation. |
| **Vault-backed alerts** | Escalation credentials are **not** supplied by the Sample; they are loaded by the Core from `UrVaultSimulation` in `appsettings.json`. |

The Sample is a console executable—not a production component. It exists to validate the heartbeat → silence → vault-resolved escalation → historical log pipeline during development and architecture review.

---

## 3. Centralized Secret Engine Emulation

### Threat-model rationale

Embedding Telegram bot tokens, Twilio auth tokens, or phone numbers inside client heartbeat payloads creates an unacceptable attack surface: every monitored microservice becomes a secret carrier, network captures expose credentials, and rotating vault material requires redeploying every producer. Ur Pulse eliminates this class of risk by making the Client SDK **100% telemetry-only and secure**—it does not carry or transfer tokens.

### Abstraction: `ISecretProvider`

The Core depends on a narrow async contract:

```csharp
public interface ISecretProvider
{
    Task<AlertSettings?> GetAlertSettingsAsync(string appId);
}
```

`PulseEngine` never reads secrets from the inbound `HeartbeatPulse`. On escalation eligibility and alert dispatch, it asynchronously pulls configuration for the affected `AppId` from the registered provider.

### Implementation: `LocalJsonSecretProvider`

`LocalJsonSecretProvider` emulates a centralized secret engine by binding per-application `AlertSettings` from configuration:

```
UrVaultSimulation:Apps:{appId}
```

If the section is absent, the provider returns `null` and escalation is suppressed for that application. This simulates a vault lookup path that can later be replaced with HashiCorp Vault, Azure Key Vault, or an internal secrets service **without changing** `PulseEngine` or the Client SDK—only the `ISecretProvider` implementation is swapped at DI registration:

```csharp
builder.Services.AddSingleton<ISecretProvider, LocalJsonSecretProvider>();
```

### Out-of-band resolution flow

1. Client POSTs telemetry (`AppId`, `ServiceName`, `Status`, `Metadata`).
2. Core stamps ingress time and updates the in-memory registry.
3. Sweeper detects sustained silence → marks `Offline`.
4. Escalation path calls `await _secretProvider.GetAlertSettingsAsync(pulse.AppId)`.
5. Core applies vault-resolved thresholds and channel credentials (Telegram, Twilio, local audio).

Secrets remain server-local, asynchronously resolved, and never transit the monitoring control plane between client and Core.

---

## 4. Historical Data Logging (EF Core + SQLite)

### Purpose

Operational heartbeats are ephemeral by nature; architecture review and uptime analytics require a durable trail of state transitions. The Core implements an **asynchronous, thread-safe background logging pipeline** that ingests `Healthy` and `Offline` transitions into a local SQLite database (`UrPulseHealth.db`) for historical uptime metrics.

### Data model: `HealthLog`

| Column | Type | Role |
|--------|------|------|
| `Id` | `Guid` | Primary key. |
| `AppId` | `string` | Logical application identity. |
| `ServiceName` | `string` | Component within the application. |
| `Status` | `string` | Persisted transition: `Healthy` or `Offline`. |
| `Timestamp` | `DateTime` | UTC write time. |
| `HardwareMetricsJson` | `string` | Serialized client metadata (RAM, CPU, host, runtime) for forensic flexibility. |

`UrPulseDbContext` exposes `DbSet<HealthLog> HealthLogs` and is registered with SQLite:

```csharp
builder.Services.AddDbContext<UrPulseDbContext>(options =>
    options.UseSqlite("Data Source=UrPulseHealth.db"));
```

### Thread-safe scoped writes via `IServiceScopeFactory`

`PulseEngine` is a **singleton**; `UrPulseDbContext` is **scoped**. Direct injection of a DbContext into the engine would violate EF Core lifetime rules and risk concurrent use across timer callbacks and ingress threads. The engine therefore depends on `IServiceScopeFactory` and creates an isolated scope per write:

```csharp
using var scope = _scopeFactory.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<UrPulseDbContext>();
// Add HealthLog → SaveChangesAsync
```

Each log operation is fire-and-forget (`Task.Run`) from:

- **`ProcessPulse`** — successful ingress → status `Healthy`
- **`CheckAppHealth`** — silence threshold crossed → status `Offline`

Failures are caught and logged to the console; they do not interrupt the sweeper or heartbeat ingress path. This preserves the non-blocking contract of the Dead Man's Switch while accumulating a queryable uptime history.

### Historical query surface

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/pulse/logs` | Latest 100 health-log rows (descending by timestamp). |
| `GET` | `/api/pulse/logs/{appId}` | Latest 50 rows for a specific application. |

---

## 5. Core Telemetry Logic

### Server ingress timestamping (race condition & time-drift mitigation)

Client devices may exhibit clock drift, NTP misconfiguration, or delayed network delivery. Ur Pulse does **not** rely on client-supplied timestamps for liveness decisions.

On every heartbeat receipt, the Core overwrites `pulse.Timestamp` with `DateTime.UtcNow` at server ingress:

```csharp
// Program.cs — ingress handler
pulse.Timestamp = DateTime.UtcNow;
engine.ProcessPulse(pulse);
```

`PulseEngine.ProcessPulse` applies a second authoritative stamp before dictionary insertion, ensuring the stored record reflects server receive time regardless of deserialization order or client payload content.

**Effect:** Offline detection compares `DateTime.UtcNow` (sweeper) against server-stamped `pulse.Timestamp` (last successful ingress). Client clock skew cannot false-positive or false-negative liveness results.

### Thread-safe state store

Registered services are held in a `ConcurrentDictionary<string, HeartbeatPulse>`:

```csharp
private readonly ConcurrentDictionary<string, HeartbeatPulse> _activePulses = new();
```

Concurrent heartbeat POSTs from multiple services—or concurrent POSTs for the same service—safely upsert without explicit locking. The background sweeper iterates `_activePulses` while ingress handlers mutate entries; `ConcurrentDictionary` provides the memory consistency required for this read/write overlap.

### Recovery semantics

When a previously `Offline` service resumes heartbeats, `ProcessPulse`:

1. Restores the registry entry to an active (`Healthy`) ingress state.
2. Clears the offline episode for subsequent escalation eligibility.
3. Enqueues a background `Healthy` health-log write.

Escalation is **single-shot per offline episode**—the `EscalationTriggered` flag prevents repeated Telegram/Twilio invocations on subsequent 5-second sweeper ticks.

### Background health sweeper

A `System.Threading.Timer` invokes `CheckAppHealth` every **5 seconds** (initial fire: immediate). Each sweep:

1. Computes `now = DateTime.UtcNow`.
2. For each registered pulse, evaluates silence: `(now - pulse.Timestamp) > 15 seconds`.
3. Transitions non-`Offline` records to `Offline`, sets `OfflineSince = now`, and enqueues an `Offline` health-log write.
4. Asynchronously resolves vault alert settings and evaluates escalation eligibility for sustained offline duration.

The 5-second sweep interval and 15-second silence threshold are independent parameters—the sweep frequency determines detection latency; the threshold determines when silence constitutes failure.

---

## 6. Cascading Escalation Pipeline (The 3 States)

Ur Pulse classifies each registered service into one of three operational states. State assignment is **split between client evaluation and server adjudication** by design. Alert credentials are resolved exclusively by the Core secret provider.

### Healthy — Active monitoring & hardware metrics ingestion

**Source:** Successful Core ingress after client heartbeat.

On each accepted pulse, the engine stamps server time, updates the in-memory registry, and asynchronously persists a `Healthy` `HealthLog` (including serialized hardware metadata). Healthy does **not** imply the server independently validates hardware metrics—it trusts continued heartbeat arrival while retaining client-supplied metadata for dashboards and historical analysis.

### Degraded — Self-reported threshold violations

**Source:** Client-side evaluation in `UrPulseClient.EvaluateStatus`.

When private memory **exceeds** `_maxMemoryHealthyMb`, the client sets `Status = "Degraded"` in the telemetry envelope while continuing to emit heartbeats. Metadata includes both `MemoryUsage_MB` and `MemoryLimit_MB`. Heartbeats still reset the ingress timestamp; degradation signals resource pressure without triggering the silence-based offline pipeline. Escalation remains reserved for heartbeat cessation.

### Offline — 15-second heartbeat silence detection

**Source:** Server-side adjudication in `PulseEngine.CheckAppHealth`.

If `(DateTime.UtcNow - pulse.Timestamp) > 15 seconds` and the record is not already `Offline`, the engine:

1. Sets `Status = "Offline"`.
2. Records `OfflineSince = DateTime.UtcNow`.
3. Resets `EscalationTriggered = false`.
4. Enqueues an `Offline` health-log write.

Silence implies the Dead Man's Switch has tripped—the service stopped asserting liveness regardless of its last client-evaluated status.

### Vault-driven escalation policy

After entering `Offline`, the engine asynchronously loads `AlertSettings` from `ISecretProvider`. When **all** of the following hold:

- Vault settings exist and `EnableAlerts == true`
- `(now - OfflineSince) >= EscalationThresholdSeconds` (default: **60**, from vault)
- `pulse.EscalationTriggered == false`

…the engine sets `EscalationTriggered = true` and invokes `TriggerCriticalEscalation`, which again resolves secrets from the vault before dispatching channels. The default timeline from last heartbeat to full escalation:

```
T+0s    Last heartbeat received (server-stamped)
T+15s   Offline detected (silence threshold) + Offline health log
T+75s   Critical escalation fires (15s silence + 60s vault threshold)
```

#### Escalation channel 1 — Local audio (server host)

When `EnableLoudAudioAlert` is true (vault), the engine runs a short `Console.Beep` sequence on the Core host for operators physically present at the console.

#### Escalation channel 2 — Telegram (Markdown)

When `EnableTelegramAlert` is true and vault bot credentials are present, the engine constructs a Markdown-formatted message and POSTs asynchronously to `https://api.telegram.org/bot{token}/sendMessage`. Failures are logged; they do not propagate to the sweeper thread.

#### Escalation channel 3 — VoIP voice call (Twilio)

When `EnableVoiceCallAlert` is true and vault Twilio credentials are present, the engine initiates an outbound call via Twilio's REST API with dynamic TwiML (`<Say language="ar-XA" voice="Polly.Zeina">`). Message content resolves from `CustomVoiceMessage` when provided; otherwise a structured default referencing `AppId`. Authentication uses HTTP Basic encoding of `{AccountSid}:{AuthToken}`—sourced exclusively from the vault, never from the client.

#### Non-blocking guarantee

Escalation and database persistence are delegated via `Task.Run` / async continuations. The sweeper and ingress paths do not await external API latency or DbContext I/O, preserving cadence under alert and ingest load.

---

## 7. Configuration Payload (`UrVaultSimulation`)

Alert policy and provider credentials are **server-side only**, keyed by `AppId` under the simulated vault section in `src/UrPulse.Core/appsettings.json`. Clients do not embed this structure in heartbeat POSTs.

### Vault configuration sample (`appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "UrVaultSimulation": {
    "Apps": {
      "vector-kanban": {
        "EnableAlerts": true,
        "EscalationThresholdSeconds": 60,
        "EnableLoudAudioAlert": true,

        "EnableTelegramAlert": true,
        "TelegramBotToken": "YOUR_BOT_TOKEN",
        "TelegramChatId": "YOUR_CHAT_ID",

        "EnableVoiceCallAlert": true,
        "TwilioAccountSid": "YOUR_ACCOUNT_SID",
        "TwilioAuthToken": "YOUR_AUTH_TOKEN",
        "TwilioFromNumber": "+15551234567",
        "TargetPhoneNumber": "+9647XXXXXXXX",
        "CustomVoiceMessage": "Attention! The vector kanban node has stopped breathing. Check infrastructure."
      }
    }
  }
}
```

### Vault field reference

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `EnableAlerts` | `bool` | `true` | Master switch for escalation pipeline. |
| `EscalationThresholdSeconds` | `int` | `60` | Seconds after `OfflineSince` before critical escalation. |
| `EnableLoudAudioAlert` | `bool` | `true` | Enable server-host `Console.Beep` sequence. |
| `EnableTelegramAlert` | `bool` | `false` | Enable Telegram Bot API dispatch. |
| `TelegramBotToken` | `string` | `""` | Bot token from [@BotFather](https://t.me/BotFather). |
| `TelegramChatId` | `string` | `""` | Target chat or group ID. |
| `EnableVoiceCallAlert` | `bool` | `false` | Enable Twilio outbound voice call. |
| `TwilioAccountSid` | `string` | `""` | Twilio account SID. |
| `TwilioAuthToken` | `string` | `""` | Twilio auth token. |
| `TwilioFromNumber` | `string` | `""` | Twilio-provisioned originating number (E.164). |
| `TargetPhoneNumber` | `string` | `""` | Operator destination number (E.164). |
| `CustomVoiceMessage` | `string` | `""` | TwiML `<Say>` content; empty uses auto-generated message. |

> **Security note:** Treat `UrVaultSimulation` as sensitive configuration. Do not commit live production tokens. Replace `LocalJsonSecretProvider` with a real vault adapter for production deployments.

### Client heartbeat telemetry (wire format)

The Client POSTs a credential-free envelope:

```json
{
  "AppId": "vector-kanban",
  "ServiceName": "Auth-Service",
  "Status": "Healthy",
  "Metadata": {
    "MachineName": "PROD-WEB-01",
    "OS": "Microsoft Windows NT 10.0.26200.0",
    "MemoryUsage_MB": "48.32",
    "MemoryLimit_MB": "100.00",
    "TotalCpuTime_Sec": "12.45",
    "DotNetVersion": "10.0.0"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `AppId` | `string` | **Required.** Logical application identifier; must match a vault key under `UrVaultSimulation:Apps` for escalation. |
| `ServiceName` | `string` | Sub-service or component name (default `"General"`). |
| `Status` | `string` | Client-evaluated `Healthy` or `Degraded`; server overrides to `Offline` on silence. |
| `Metadata` | `object` | Hardware/environment telemetry; also persisted as `HardwareMetricsJson` on health-log writes. |

> **Note:** `Timestamp`, `OfflineSince`, and `EscalationTriggered` are server-managed fields. Clients omit them; the Core assigns authoritative values on ingress and during sweeper evaluation.

### Live status response

`GET /api/pulse/status` returns an array of all registered `HeartbeatPulse` records currently held in the engine's `ConcurrentDictionary`.

---

## 8. Execution Guide

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (solution targets `net10.0`)
- Network access to `localhost:5252` for client ↔ core communication
- Optional: Telegram bot token/chat ID and Twilio credentials configured under `UrVaultSimulation` in Core `appsettings.json` for live escalation testing

### Build

From the repository root:

```bash
dotnet build UrPulse.slnx
```

### Run the central engine

**Terminal 1:**

```bash
dotnet run --project src/UrPulse.Core
```

The Core binds to **`http://localhost:5252`** (configured in `launchSettings.json`). OpenAPI metadata is available in Development at `/openapi/v1.json`. On startup, EF Core uses the SQLite database `UrPulseHealth.db` for historical health logs.

Verify registration state and history:

```bash
curl http://localhost:5252/api/pulse/status
curl http://localhost:5252/api/pulse/logs
curl http://localhost:5252/api/pulse/logs/vector-kanban
```

### Run the sample client

**Terminal 2** (with Core running):

```bash
dotnet run --project src/UrPulse.Sample
```

The sample:

1. Starts emitting **telemetry-only** heartbeats every **5 seconds** for `vector-kanban` / `Auth-Service`.
2. Waits for a keypress, then **stops** the client to simulate process termination.
3. Observes the Core timeline: **~15 s** to `Offline` (and Offline health log), then vault-threshold escalation (Telegram, optional Twilio, server audio).

Configure alert credentials exclusively in `src/UrPulse.Core/appsettings.json` under `UrVaultSimulation:Apps:vector-kanban`. Do not place tokens in the Sample or Client.

### Integrate into an existing application

Add a project reference to `UrPulse.Client` and initialize (no alert credentials):

```csharp
using var pulseClient = new UrPulseClient(
    serverUrl: "http://localhost:5252",
    appId: "my-application",
    serviceName: "Api-Gateway",
    intervalSeconds: 10,
    maxMemoryHealthyMb: 256.0
);

pulseClient.Start();

// ... application lifetime ...

pulseClient.Stop();
```

Ensure a matching entry exists under `UrVaultSimulation:Apps:my-application` on the Core host if escalation is required.

### API surface summary

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/pulse/heartbeat` | Register or refresh a service pulse (telemetry only). Requires non-empty `AppId`. Returns `400` if missing. |
| `GET` | `/api/pulse/status` | Returns all in-memory registered pulses and their current state. |
| `GET` | `/api/pulse/logs` | Returns the latest 100 historical `HealthLog` rows. |
| `GET` | `/api/pulse/logs/{appId}` | Returns the latest 50 historical rows for the specified application. |

---

**UrLabs** — Ur Pulse · Infrastructure heartbeat monitoring with telemetry-only clients, server-side secret engine emulation, durable SQLite uptime history, and asynchronous multi-channel escalation.
