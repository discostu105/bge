import { useEffect, useRef, useCallback, useState } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr'

export type SignalRStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'

export interface UseSignalRReturn {
  connection: HubConnection | null
  status: SignalRStatus
  on: (method: string, handler: (...args: unknown[]) => void) => void
  off: (method: string, handler: (...args: unknown[]) => void) => void
}

export function useSignalR(hubUrl: string = '/gamehub'): UseSignalRReturn {
  const connRef = useRef<HubConnection | null>(null)
  const [status, setStatus] = useState<SignalRStatus>('disconnected')

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build()

    connRef.current = connection

    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(() => setStatus('connected'))
    connection.onclose(() => setStatus('disconnected'))

    setStatus('connecting')
    connection
      .start()
      .then(() => setStatus('connected'))
      .catch(() => setStatus('disconnected'))

    return () => {
      connection.stop()
      connRef.current = null
    }
  }, [hubUrl])

  const on = useCallback(
    (method: string, handler: (...args: unknown[]) => void) => {
      connRef.current?.on(method, handler)
    },
    []
  )

  const off = useCallback(
    (method: string, handler: (...args: unknown[]) => void) => {
      connRef.current?.off(method, handler)
    },
    []
  )

  return { connection: connRef.current, status, on, off }
}
