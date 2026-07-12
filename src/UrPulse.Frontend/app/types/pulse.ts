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

/** Response from GET /api/settings/system */
export interface SystemSettings {
  thresholdSeconds: number
  intervalSeconds: number
}

/** Mirrors UrPulse.Core.Models.AlertSettings */
export interface AlertSettings {
  enableAlerts: boolean
  escalationThresholdSeconds: number
  enableLoudAudioAlert: boolean
  enableTelegramAlert: boolean
  telegramBotToken: string
  telegramChatId: string
  enableVoiceCallAlert: boolean
  twilioAccountSid: string
  twilioAuthToken: string
  twilioFromNumber: string
  targetPhoneNumber: string
  customVoiceMessage: string
}

export type ToastType = 'success' | 'error' | 'info'

export interface ToastMessage {
  id: number
  type: ToastType
  title: string
  message: string
}

export function createDefaultAlertSettings(): AlertSettings {
  return {
    enableAlerts: false,
    escalationThresholdSeconds: 60,
    enableLoudAudioAlert: true,
    enableTelegramAlert: false,
    telegramBotToken: '',
    telegramChatId: '',
    enableVoiceCallAlert: false,
    twilioAccountSid: '',
    twilioAuthToken: '',
    twilioFromNumber: '',
    targetPhoneNumber: '',
    customVoiceMessage: '',
  }
}
