import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { PenSquareIcon, InboxIcon, SendIcon, ReplyIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { MessageInboxViewModel, MessageViewModel, SendMessageViewModel, PublicPlayerViewModel } from '@/api/types'
import { relativeTime } from '@/lib/utils'
import { cn } from '@/lib/utils'

interface MessagesProps {
  gameId: string
}

function ComposeForm({
  players,
  onSent,
  replyTo,
}: {
  players: PublicPlayerViewModel[]
  onSent: () => void
  replyTo?: MessageViewModel
}) {
  const [recipientId, setRecipientId] = useState(replyTo?.senderId ?? '')
  const [subject, setSubject] = useState(replyTo ? `Re: ${replyTo.subject}` : '')
  const [body, setBody] = useState('')
  const [error, setError] = useState<string | null>(null)

  const send = useMutation({
    mutationFn: (req: SendMessageViewModel) =>
      apiClient.post('/api/messages/send', req),
    onSuccess: () => {
      setBody('')
      setSubject('')
      onSent()
    },
    onError: async (err: unknown) => {
      const msg = (err as { response?: { data?: string } })?.response?.data ?? 'Failed to send'
      setError(msg)
    },
  })

  return (
    <div className="space-y-3">
      {error && (
        <div className="rounded border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </div>
      )}
      <div>
        <label className="text-xs text-muted-foreground mb-1 block">Recipient</label>
        <select
          className="w-full rounded border bg-input px-2 py-1.5 text-sm"
          value={recipientId}
          onChange={(e) => setRecipientId(e.target.value)}
        >
          <option value="">— select player —</option>
          {players.map((p) => (
            <option key={p.playerId} value={p.playerId ?? ''}>
              {p.playerName}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label className="text-xs text-muted-foreground mb-1 block">Subject</label>
        <input
          className="w-full rounded border bg-input px-2 py-1.5 text-sm"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          maxLength={100}
        />
      </div>
      <div>
        <label className="text-xs text-muted-foreground mb-1 block">Message</label>
        <textarea
          className="w-full rounded border bg-input px-2 py-1.5 text-sm min-h-[80px]"
          value={body}
          onChange={(e) => setBody(e.target.value)}
          maxLength={2000}
        />
      </div>
      <button
        onClick={() => send.mutate({ recipientId, subject, body })}
        disabled={!recipientId || !subject.trim() || !body.trim() || send.isPending}
        className="flex items-center gap-1.5 rounded bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
      >
        <SendIcon className="h-4 w-4" />
        {send.isPending ? 'Sending…' : 'Send'}
      </button>
    </div>
  )
}

export function Messages({ gameId }: MessagesProps) {
  const queryClient = useQueryClient()
  const [tab, setTab] = useState<'inbox' | 'sent'>('inbox')
  const [selected, setSelected] = useState<MessageViewModel | null>(null)
  const [composing, setComposing] = useState(false)

  const { data: inbox } = useQuery({
    queryKey: ['messages-inbox', gameId],
    queryFn: () =>
      apiClient.get<MessageInboxViewModel>('/api/messages/inbox').then((r) => r.data),
  })

  const { data: sent } = useQuery({
    queryKey: ['messages-sent', gameId],
    queryFn: () =>
      apiClient.get<MessageInboxViewModel>('/api/messages/sent').then((r) => r.data),
  })

  const { data: players = [] } = useQuery({
    queryKey: ['players-list'],
    queryFn: () =>
      apiClient.get<PublicPlayerViewModel[]>('/api/playerranking').then((r) => r.data),
  })

  const markRead = useMutation({
    mutationFn: (id: string) => apiClient.post(`/api/messages/${id}/read`, {}),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['messages-inbox'] }),
  })

  function openMessage(msg: MessageViewModel) {
    setSelected(msg)
    setComposing(false)
    if (!msg.isRead) markRead.mutate(msg.messageId)
  }

  const messages = tab === 'inbox' ? inbox?.messages ?? [] : sent?.messages ?? []
  const unread = inbox?.messages.filter((m) => !m.isRead).length ?? 0

  return (
    <div className="max-w-3xl">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold">Messages</h1>
        <button
          onClick={() => { setComposing(true); setSelected(null) }}
          className="flex items-center gap-1.5 rounded bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90"
        >
          <PenSquareIcon className="h-4 w-4" />
          Compose
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-[240px_1fr] gap-4">
        {/* List pane */}
        <div className="rounded-lg border overflow-hidden">
          <div className="flex border-b">
            <button
              onClick={() => setTab('inbox')}
              className={cn(
                'flex-1 flex items-center justify-center gap-1.5 py-2 text-sm',
                tab === 'inbox' ? 'bg-secondary text-foreground font-medium' : 'text-muted-foreground hover:text-foreground'
              )}
            >
              <InboxIcon className="h-3.5 w-3.5" />
              Inbox
              {unread > 0 && (
                <span className="ml-1 rounded-full bg-primary px-1.5 text-[10px] text-primary-foreground">
                  {unread}
                </span>
              )}
            </button>
            <button
              onClick={() => setTab('sent')}
              className={cn(
                'flex-1 flex items-center justify-center gap-1.5 py-2 text-sm',
                tab === 'sent' ? 'bg-secondary text-foreground font-medium' : 'text-muted-foreground hover:text-foreground'
              )}
            >
              <SendIcon className="h-3.5 w-3.5" />
              Sent
            </button>
          </div>

          <ul className="divide-y max-h-[420px] overflow-y-auto">
            {messages.length === 0 ? (
              <li className="px-3 py-4 text-center text-sm text-muted-foreground">Empty</li>
            ) : (
              messages.map((msg) => (
                <li key={msg.messageId}>
                  <button
                    onClick={() => openMessage(msg)}
                    className={cn(
                      'w-full text-left px-3 py-2.5 text-sm hover:bg-secondary/60 transition-colors',
                      selected?.messageId === msg.messageId && 'bg-secondary',
                      tab === 'inbox' && !msg.isRead && 'font-semibold'
                    )}
                  >
                    <div className="truncate">{tab === 'inbox' ? msg.senderName : msg.recipientName}</div>
                    <div className="truncate text-xs text-muted-foreground">{msg.subject}</div>
                    <div className="text-[10px] text-muted-foreground mt-0.5">{relativeTime(msg.sentAt)}</div>
                  </button>
                </li>
              ))
            )}
          </ul>
        </div>

        {/* Detail / compose pane */}
        <div className="rounded-lg border p-4 min-h-[200px]">
          {composing ? (
            <>
              <h3 className="font-semibold text-sm mb-3">{selected ? 'Reply' : 'New Message'}</h3>
              <ComposeForm
                players={players}
                replyTo={selected ?? undefined}
                onSent={() => {
                  setComposing(false)
                  setSelected(null)
                  queryClient.invalidateQueries({ queryKey: ['messages-sent'] })
                }}
              />
            </>
          ) : selected ? (
            <div className="space-y-3">
              <div>
                <div className="flex justify-between items-start">
                  <h3 className="font-semibold">{selected.subject}</h3>
                  <span className="text-xs text-muted-foreground">{relativeTime(selected.sentAt)}</span>
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">
                  From: <span className="text-foreground">{selected.senderName}</span>
                </p>
              </div>
              <p className="text-sm whitespace-pre-wrap">{selected.body}</p>
              {tab === 'inbox' && (
                <button
                  onClick={() => { setComposing(true) }}
                  className="flex items-center gap-1 text-sm text-primary hover:underline"
                >
                  <ReplyIcon className="h-3.5 w-3.5" />
                  Reply
                </button>
              )}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground text-center mt-8">Select a message to read it.</p>
          )}
        </div>
      </div>
    </div>
  )
}
