import { useState, useEffect, useRef, useCallback } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { SendIcon, ShieldIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type {
  ChatMessagesViewModel,
  ChatMessageViewModel,
  PostChatMessageRequest,
  AllianceChatPostViewModel,
  MyAllianceStatusViewModel,
  PostAllianceChatRequest,
} from '@/api/types'
import { useSignalR } from '@/hooks/useSignalR'
import { cn } from '@/lib/utils'

const RACE_ICONS: Record<string, string> = {
  terran: '🔧',
  protoss: '🔮',
  zerg: '🦠',
}

function raceIcon(playerType: string): string {
  return RACE_ICONS[playerType] ?? ''
}

interface ChatProps {
  gameId: string
}

function GameChat({ gameId, signalR }: { gameId: string; signalR: ReturnType<typeof useSignalR> }) {
  const [body, setBody] = useState('')
  const [lastMessageId, setLastMessageId] = useState<string | null>(null)
  const [allMessages, setAllMessages] = useState<ChatMessageViewModel[]>([])
  const [error, setError] = useState<string | null>(null)
  const [rateLimited, setRateLimited] = useState(false)
  const chatEndRef = useRef<HTMLDivElement>(null)
  const isRealTime = signalR.status === 'connected'

  // Initial load
  useEffect(() => {
    apiClient.get<ChatMessagesViewModel>('/api/chat').then((r) => {
      setAllMessages(r.data.messages)
      const msgs = r.data.messages
      if (msgs.length > 0) setLastMessageId(msgs[msgs.length - 1].messageId)
    })
  }, [gameId])

  // Listen for real-time chat messages via SignalR
  const handleChatMessage = useCallback(
    (...args: unknown[]) => {
      const msg = args[0] as ChatMessageViewModel | undefined
      if (!msg?.messageId) return
      setAllMessages((prev) => {
        if (prev.some((m) => m.messageId === msg.messageId)) return prev
        return [...prev, msg]
      })
      setLastMessageId(msg.messageId)
    },
    []
  )

  useEffect(() => {
    signalR.on('ReceiveChatMessage', handleChatMessage)
    return () => signalR.off('ReceiveChatMessage', handleChatMessage)
  }, [signalR, handleChatMessage])

  // Fallback polling — only when SignalR is not connected
  useQuery({
    queryKey: ['chat-poll', gameId, lastMessageId],
    queryFn: async () => {
      const url = lastMessageId ? `/api/chat?after=${lastMessageId}` : '/api/chat'
      const r = await apiClient.get<ChatMessagesViewModel>(url)
      if (r.data.messages.length > 0) {
        setAllMessages((prev) => {
          const ids = new Set(prev.map((m) => m.messageId))
          const newMsgs = r.data.messages.filter((m) => !ids.has(m.messageId))
          return newMsgs.length > 0 ? [...prev, ...newMsgs] : prev
        })
        const last = r.data.messages[r.data.messages.length - 1]
        setLastMessageId(last.messageId)
      }
      return r.data
    },
    refetchInterval: isRealTime ? false : 5_000,
    enabled: !isRealTime,
  })

  // Scroll to bottom on new messages
  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [allMessages])

  const send = useMutation({
    mutationFn: (req: PostChatMessageRequest) =>
      apiClient.post('/api/chat', req),
    onSuccess: () => {
      setBody('')
      setError(null)
      setRateLimited(false)
    },
    onError: (err: unknown) => {
      const axErr = err as { response?: { status?: number; data?: string } }
      if (axErr?.response?.status === 429) {
        setRateLimited(true)
        setError(axErr.response.data ?? 'Rate limited — wait 2 seconds')
        setTimeout(() => setRateLimited(false), 2000)
      } else {
        setError(axErr?.response?.data ?? 'Failed to send')
      }
    },
  })

  function handleSend() {
    if (!body.trim() || rateLimited) return
    setError(null)
    send.mutate({ body: body.trim() })
  }

  return (
    <>
      {error && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </div>
      )}

      {/* Send box */}
      <div className="flex gap-2">
        <input
          className={cn(
            'flex-1 rounded-md border bg-input px-3 py-2 text-sm',
            'placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring'
          )}
          placeholder="Type a message…"
          aria-label="Chat message"
          maxLength={500}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend() } }}
        />
        <button
          onClick={handleSend}
          disabled={!body.trim() || send.isPending || rateLimited}
          className={cn(
            'flex items-center gap-1.5 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground',
            'hover:bg-primary/90 disabled:opacity-50 transition-colors'
          )}
        >
          <SendIcon className="h-4 w-4" />
          Send
        </button>
      </div>
      <p className="text-xs text-muted-foreground">{body.length} / 500</p>

      {/* Chat window */}
      <div className="h-[min(420px,60vh)] overflow-y-auto rounded-lg border bg-card p-2 space-y-1.5" role="log" aria-label="Game chat messages">
        {allMessages.length === 0 ? (
          <p className="text-center text-muted-foreground text-sm mt-8">No messages yet. Start the conversation!</p>
        ) : (
          allMessages.map((msg) => (
            <div key={msg.messageId} className="rounded-md border-l-2 border-primary/60 bg-secondary/30 px-3 py-2">
              <div className="flex justify-between text-xs mb-0.5">
                <span className="font-semibold text-primary">
                  {raceIcon(msg.playerType)} {msg.authorName}
                </span>
                <span className="text-muted-foreground">
                  {new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
                </span>
              </div>
              <p className="text-sm">{msg.body}</p>
            </div>
          ))
        )}
        <div ref={chatEndRef} />
      </div>
    </>
  )
}

function AllianceChat({ allianceId, signalR }: { allianceId: string; signalR: ReturnType<typeof useSignalR> }) {
  const [body, setBody] = useState('')
  const [allPosts, setAllPosts] = useState<AllianceChatPostViewModel[]>([])
  const [error, setError] = useState<string | null>(null)
  const [rateLimited, setRateLimited] = useState(false)
  const [loaded, setLoaded] = useState(false)
  const chatEndRef = useRef<HTMLDivElement>(null)
  const isRealTime = signalR.status === 'connected'

  // Initial load
  useEffect(() => {
    apiClient.get<AllianceChatPostViewModel[]>(`/api/alliances/${allianceId}/posts`).then((r) => {
      setAllPosts(r.data)
      setLoaded(true)
    })
  }, [allianceId])

  // Listen for real-time alliance chat messages via SignalR
  const handleAllianceMessage = useCallback(
    (...args: unknown[]) => {
      const msg = args[0] as AllianceChatPostViewModel | undefined
      if (!msg?.postId) return
      setAllPosts((prev) => {
        if (prev.some((p) => p.postId === msg.postId)) return prev
        return [...prev, msg]
      })
    },
    []
  )

  useEffect(() => {
    signalR.on('ReceiveAllianceChatMessage', handleAllianceMessage)
    return () => signalR.off('ReceiveAllianceChatMessage', handleAllianceMessage)
  }, [signalR, handleAllianceMessage])

  // Fallback polling when SignalR not connected
  useQuery({
    queryKey: ['alliance-chat-poll', allianceId],
    queryFn: async () => {
      const r = await apiClient.get<AllianceChatPostViewModel[]>(`/api/alliances/${allianceId}/posts`)
      setAllPosts(r.data)
      return r.data
    },
    refetchInterval: isRealTime ? false : 5_000,
    enabled: !isRealTime && loaded,
  })

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [allPosts])

  const send = useMutation({
    mutationFn: (req: PostAllianceChatRequest) =>
      apiClient.post(`/api/alliances/${allianceId}/posts`, req),
    onSuccess: () => {
      setBody('')
      setError(null)
      setRateLimited(false)
    },
    onError: (err: unknown) => {
      const axErr = err as { response?: { status?: number; data?: string } }
      if (axErr?.response?.status === 429) {
        setRateLimited(true)
        setError(axErr.response.data ?? 'Rate limited — wait 2 seconds')
        setTimeout(() => setRateLimited(false), 2000)
      } else {
        setError(axErr?.response?.data ?? 'Failed to send')
      }
    },
  })

  function handleSend() {
    if (!body.trim() || rateLimited) return
    setError(null)
    send.mutate({ body: body.trim() })
  }

  return (
    <>
      {error && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </div>
      )}

      {/* Send box */}
      <div className="flex gap-2">
        <input
          className={cn(
            'flex-1 rounded-md border bg-input px-3 py-2 text-sm',
            'placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring'
          )}
          placeholder="Message your alliance…"
          aria-label="Alliance chat message"
          maxLength={1000}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend() } }}
        />
        <button
          onClick={handleSend}
          disabled={!body.trim() || send.isPending || rateLimited}
          className={cn(
            'flex items-center gap-1.5 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground',
            'hover:bg-primary/90 disabled:opacity-50 transition-colors'
          )}
        >
          <SendIcon className="h-4 w-4" />
          Send
        </button>
      </div>
      <p className="text-xs text-muted-foreground">{body.length} / 1000</p>

      {/* Chat window */}
      <div className="h-[min(420px,60vh)] overflow-y-auto rounded-lg border bg-card p-2 space-y-1.5" role="log" aria-label="Alliance chat messages">
        {allPosts.length === 0 ? (
          <p className="text-center text-muted-foreground text-sm mt-8">No alliance messages yet.</p>
        ) : (
          allPosts.map((p) => (
            <div key={p.postId} className="rounded-md border-l-2 border-green-500/60 bg-secondary/30 px-3 py-2">
              <div className="flex justify-between text-xs mb-0.5">
                <span className="font-semibold text-green-600 dark:text-green-400">
                  {raceIcon(p.playerType)} {p.authorName}
                </span>
                <span className="text-muted-foreground">
                  {new Date(p.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
                </span>
              </div>
              <p className="text-sm">{p.body}</p>
            </div>
          ))
        )}
        <div ref={chatEndRef} />
      </div>
    </>
  )
}

type ChatTab = 'game' | 'alliance'

export function ChatPanel({ gameId }: ChatProps) {
  const [tab, setTab] = useState<ChatTab>('game')
  const signalR = useSignalR('/gamehub')

  const { data: allianceStatus } = useQuery<MyAllianceStatusViewModel>({
    queryKey: ['alliance-status-chat'],
    queryFn: () => apiClient.get('/api/alliances/my-status').then((r) => r.data),
    refetchInterval: 30_000,
  })

  const hasAlliance = allianceStatus?.allianceId && allianceStatus.isMember && !allianceStatus.isPending

  return (
    <div className="space-y-3">
      {/* Connection indicator */}
      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <span className={cn(
          'inline-block h-2 w-2 rounded-full',
          signalR.status === 'connected' ? 'bg-green-500' : signalR.status === 'reconnecting' ? 'bg-yellow-500' : 'bg-gray-400'
        )} />
        {signalR.status === 'connected' ? 'Live' : signalR.status === 'reconnecting' ? 'Reconnecting…' : 'Polling'}
      </div>

      {/* Tabs */}
      {hasAlliance && (
        <div className="flex gap-1 border-b">
          <button
            onClick={() => setTab('game')}
            className={cn(
              'px-3 py-1.5 text-sm font-medium transition-colors border-b-2 -mb-px',
              tab === 'game' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'
            )}
          >
            Game
          </button>
          <button
            onClick={() => setTab('alliance')}
            className={cn(
              'px-3 py-1.5 text-sm font-medium transition-colors border-b-2 -mb-px flex items-center gap-1',
              tab === 'alliance' ? 'border-green-500 text-green-600 dark:text-green-400' : 'border-transparent text-muted-foreground hover:text-foreground'
            )}
          >
            <ShieldIcon className="h-3.5 w-3.5" />
            Alliance
          </button>
        </div>
      )}

      {tab === 'game' && <GameChat gameId={gameId} signalR={signalR} />}
      {tab === 'alliance' && hasAlliance && (
        <AllianceChat allianceId={allianceStatus.allianceId!} signalR={signalR} />
      )}
    </div>
  )
}

export function Chat({ gameId }: ChatProps) {
  return (
    <div className="max-w-2xl space-y-3">
      <h1 className="text-xl font-bold">Game Chat</h1>
      <ChatPanel gameId={gameId} />
    </div>
  )
}
