# Ur Pulse

**UrLabs · Centralized Dead Man's Switch & Infrastructure Heartbeat Monitor**

Ur Pulse is the production monitoring spine of the **UrLabs** studio ecosystem: a .NET 10 Minimal API Core, a telemetry-only Client SDK, and a Nuxt operator dashboard. Together they deliver live service liveness, multi-channel escalation, durable health history, and **live-tunable** system configuration—without ever placing alert secrets on monitored hosts.

---

## Architectural Crown — Final Engineering Milestone

This release closes the operator control plane under the UrLabs banner:

| Pillar | What shipped |
|--------|----------------|
| **Dynamic system configuration** | Operators retune **scan intervals** (1–60 s) and **offline dead-line timeouts** (5–300 s, default 20 s) at runtime via `GET/POST /api/settings/system`. The Core sweeper cadence updates in-process through `Timer.Change`—no restart required. |
| **Configurable dashboard row limits** | `dashboardLogLimit` (Nuxt `useState`, default **10**) lets operators cap the Historical Telemetry Timeline preview for layout performance; full history remains on `/logs`. |
| **Out-of-band secure secret vaults** | Telegram, Twilio, and audio policy live in `ISecretProvider` / `UrVaultSimulation`. Clients send telemetry only; the Settings Vault UI binds escalation config to an `AppId` without putting credentials on the wire. |
| **Non-blocking asynchronous EF Core persistence** | `Healthy` / `Offline` transitions are written on background tasks through `IServiceScopeFactory`-scoped `UrPulseDbContext` instances, so the sweeper never awaits DbContext or HTTP I/O on the timer thread. |
| **Server-side log pagination** | Full history streams from `GET /api/pulse/logs/paginated?page=&pageSize=20` as `{ totalCount, page, pageSize, logs }` into `/logs` with Previous / Next controls. |

**Solution layout:** `UrPulse.sln` / `UrPulse.slnx` — `src/UrPulse.Core`, `UrPulse.Client`, `UrPulse.Sample`, `UrPulse.Frontend` (.NET 10 + Nuxt).

---

## 1. Philosophy

Distributed systems fail silently. Ur Pulse treats absence of heartbeat as failure (Dead Man's Switch), while still allowing clients to self-report `Degraded` when local resource ceilings are breached.

| Principle | Implementation |
|-----------|----------------|
| **Telemetry-only Client SDK** | Identity, status, and hardware metadata only—never tokens or alert policy. |
| **Out-of-band secret resolution** | Core loads per-`AppId` vault rows asynchronously when escalation fires. |
| **Server-authoritative time** | Ingress stamps `DateTime.UtcNow`; client clocks cannot skew liveness. |
| **Live system tuning** | Threshold + scan interval are mutable on the running `PulseEngine` singleton. |
| **Non-blocking escalation & logging** | Alerts and EF Core writes run off the sweeper thread. |

---

## 2. Architecture

```
Monitored Apps (UrPulse.Client)
        │ POST /api/pulse/heartbeat
        ▼
UrPulse.Core — PulseEngine (singleton)
  · ConcurrentDictionary live registry
  · Dynamic sweeper (_timerInterval, Timer.Change)
  · Configurable offline threshold (_offlineThreshold)
  · ISecretProvider vault (UrVaultSimulation)
  · Background EF Core → UrPulseHealth.db
        │
        ├── GET /api/pulse/status
        ├── GET /api/pulse/logs (preview)
        ├── GET /api/pulse/logs/paginated
        ├── GET/POST /api/vault/settings/{appId}
        └── GET/POST /api/settings/system
                │
                ▼
        UrPulse.Frontend (Nuxt)
          Dashboard · /logs · Settings Vault
```

### Projects

| Project | Role |
|---------|------|
| **UrPulse.Core** | Minimal API host, `PulseEngine`, vault provider, SQLite health logs, system settings API. |
| **UrPulse.Client** | Timer-driven telemetry emitter; local memory → `Healthy` / `Degraded`. |
| **UrPulse.Sample** | Console crash-simulation harness against a local Core. |
| **UrPulse.Frontend** | Nuxt dashboard: live cards, connection gate, vault forms, paginated logs, tunable row limits. |

---

## 3. Core APIs (operator-facing)

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/api/pulse/heartbeat` | Ingress telemetry |
| `GET` | `/api/pulse/status` | Live registry snapshot |
| `GET` | `/api/pulse/logs` | Recent transitions (dashboard preview source) |
| `GET` | `/api/pulse/logs/paginated?page=&pageSize=` | `{ totalCount, page, pageSize, totalPages, logs }` |
| `GET` | `/api/pulse/logs/{appId}` | Per-app history slice |
| `GET/POST` | `/api/vault/settings/{appId}` | Out-of-band `AlertSettings` |
| `GET/POST` | `/api/settings/system` | `{ thresholdSeconds, intervalSeconds }` |

**System settings body (POST):** `SystemSettingsModel(ThresholdSeconds, IntervalSeconds)`  
- Threshold: **5–300** seconds (default **20**)  
- Interval: **1–60** seconds (default **5**)

---

## 4. Out-of-band Secret Vault

Escalation credentials never leave the Core. `LocalJsonSecretProvider` reads/writes `UrVaultSimulation` in `appsettings.json`, keyed by `AppId`. When silence exceeds the vault’s `EscalationThresholdSeconds`, Core may sound local audio, send Telegram, and/or place a Twilio voice call.

The Nuxt **Settings Vault** edits these rows on demand; selecting an App ID does **not** auto-register the app—registration happens on the first heartbeat.

---

## 5. Persistence (non-blocking EF Core)

`PulseEngine` enqueues health-log writes via `Task.Run` + a fresh DI scope:

- Entity: `HealthLog` (`Id`, `AppId`, `ServiceName`, `Status`, `Timestamp`, `HardwareMetricsJson`)
- Store: SQLite `UrPulseHealth.db`
- Operator UI: `dashboardLogLimit`-capped preview + paginated `/logs`

---

## 6. Operator Frontend (`UrPulse.Frontend`)

- **Global connection engine** (`useState`): loading / failed / connected with exponential initial retries
- **`dashboardLogLimit`**: shared client preference (default 10) for timeline density
- **Dashboard**: live status cards + capped timeline + link to full history
- **Log History** (`/logs`): server-side pagination
- **Settings**: system tuning (`/api/settings/system`) + row limit + per-app vault forms (lazy fetch)

```bash
# Core — http://localhost:5252
cd src/UrPulse.Core && dotnet run

# Dashboard — http://localhost:3000
cd src/UrPulse.Frontend && npm install && npm run dev
```

Optional: `NUXT_PUBLIC_API_BASE=http://localhost:5252`

---

## 7. Quick validation

1. Start Core, then Frontend.
2. Run `UrPulse.Sample` (or any client) with `AppId` e.g. `vector-kanban`.
3. Confirm Dashboard cards update; stop the client and wait for the configured offline threshold.
4. In Settings → General System Settings, load/save scan interval, threshold, and dashboard row limit.
5. Open **Log History** and page through `/api/pulse/logs/paginated` results.

---

## License & stewardship

Ur Pulse is developed under the **UrLabs** studio umbrella for internal and studio-ecosystem service reliability.
