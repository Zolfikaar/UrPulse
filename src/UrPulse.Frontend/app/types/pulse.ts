/** Mirrors UrPulse.Core.Models.HeartbeatPulse */
export interface HeartbeatPulse {
  appId: string
  serviceName: string
  status: 'Healthy' | 'Offline' | 'Degraded' | string
  timestamp: string
  offlineSince: string | null
  escalationTriggered: boolean
  metadata: Record<string, string>
}

/** Mirrors UrPulse.Core.Entities.HealthLog */
export interface HealthLog {
  id: string
  appId: string
  serviceName: string
  status: string
  timestamp: string
  hardwareMetricsJson: string
}

/** Response from GET /api/pulse/logs/paginated */
export interface PaginatedHealthLogs {
  logs: HealthLog[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

/** Public timing contract for monitored apps — GET /api/pulse/client-config */
export interface PulseClientConfig {
  heartbeatIntervalSeconds: number
  offlineThresholdSeconds: number
  escalationDelaySeconds: number
  scanIntervalSeconds: number
}

/** Mirrors UrPulse.Core.Models.UrPulseSettings — global system + alerting */
export interface TelegramSettings {
  botToken: string
  chatId: string
}

export interface TwilioSettings {
  accountSid: string
  authToken: string
  fromNumber: string
  toNumber: string
  voiceMessage: string
}

export interface UrPulseSettings {
  globalHeartbeatIntervalSeconds: number
  globalOfflineThresholdSeconds: number
  globalScanIntervalSeconds: number
  globalEscalationDelaySeconds: number
  enableAlerts: boolean
  localAudioAlerts: boolean
  telegram: TelegramSettings
  twilio: TwilioSettings
}

export type ToastType = 'success' | 'error' | 'info'

export interface ToastMessage {
  id: number
  type: ToastType
  title: string
  message: string
}

export function createDefaultUrPulseSettings(): UrPulseSettings {
  return {
    globalHeartbeatIntervalSeconds: 10,
    globalOfflineThresholdSeconds: 20,
    globalScanIntervalSeconds: 5,
    globalEscalationDelaySeconds: 30,
    enableAlerts: true,
    localAudioAlerts: true,
    telegram: {
      botToken: '',
      chatId: '',
    },
    twilio: {
      accountSid: '',
      authToken: '',
      fromNumber: '',
      toNumber: '',
      voiceMessage: '',
    },
  }
}
