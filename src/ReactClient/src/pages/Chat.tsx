import { useState, useEffect, useRef } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { SendIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { ChatMessagesViewModel, PostChatMessageRequest } from '@/api/types'
import { cn } from '@/lib/utils'

interface ChatProps {
  gameId: string
}

export function Chat({ gameId }: ChatProps) {
  const [body, setBody] = useState('')
  const [lastMessageId, setLastMessageId] = useState<string | null>(null)
  const [allMessages, setAllMessages] = useState<ChatMessagesViewModel['messages']>([])
  const [error, setError] = useState<string | null>(null)
  const chatEndRef = useRef<HTMLDivElement>(null)

  // Initial load
  useEffect(() => {
    apiClient.get<ChatMessagesViewModel>('/api/chat').then((r) => {
      setAllMessages(r.data.messages)
      const msgs = r.data.messages
      if (msgs.length > 0) setLastMessageId(msgs[msgs.length - 1].messageId)
    })
  }, [gameId])

  // Poll for new messages
  useQuery({
    queryKey: ['chat-poll', gameId, lastMessageId],
    queryFn: async () => {
      const url = lastMessageId ? `/api/chat?after=${lastMessageId}` : '/api/chat'
      const r = await apiClient.get<ChatMessagesViewModel>(url)
      if (r.data.messages.length > 0) {
        setAllMessages((prev) => [...prev, ...r.data.messages])
        const last = r.data.messages[r.data.messages.length - 1]
        setLastMessageId(last.messageId)
      }
      return r.data
    },
    refetchInterval: 5_000,
    enabled: true,
  })

  // Scroll to bottom on new messages
  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [allMessages])

  const send = useMutation({
    mutationFn: (req: PostChatMessageRequest) =>
      apiClient.post('/api/chat', req),
    onSuccess: async () => {
      setBody('')
      const r = await apiClient.get<ChatMessagesViewModel>(
        lastMessageId ? `/api/chat?after=${lastMessageId}` : '/api/chat'
      )
      if (r.data.messages.length > 0) {
        setAllMessages((prev) => [...prev, ...r.data.messages])
        const last = r.data.messages[r.data.messages.length - 1]
        setLastMessageId(last.messageId)
      }
    },
    onError: async (err: unknown) => {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to send'
      setError(msg)
    },
  })

  function handleSend() {
    if (!body.trim()) return
    setError(null)
    send.mutate({ body: body.trim() })
  }

  return (
    <div className="max-w-2xl space-y-3">
      <h1 className="text-xl font-bold">Game Chat</h1>

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
          maxLength={500}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend() } }}
        />
        <button
          onClick={handleSend}
          disabled={!body.trim() || send.isPending}
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
      <div className="h-[420px] overflow-y-auto rounded-lg border bg-card p-2 space-y-1.5">
        {allMessages.length === 0 ? (
          <p className="text-center text-muted-foreground text-sm mt-8">No messages yet. Start the conversation!</p>
        ) : (
          allMessages.map((msg) => (
            <div key={msg.messageId} className="rounded-md border-l-2 border-primary/60 bg-secondary/30 px-3 py-2">
              <div className="flex justify-between text-xs mb-0.5">
                <span className="font-semibold text-primary">{msg.authorName}</span>
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
    </div>
  )
}
