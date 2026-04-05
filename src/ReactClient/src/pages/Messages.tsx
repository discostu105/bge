import { useState, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { PenSquareIcon, InboxIcon, SendIcon, ReplyIcon, MessageSquareIcon } from 'lucide-react'
import apiClient from '@/api/client'
import type { MessageInboxViewModel, MessageViewModel, MessageThreadViewModel, SendMessageViewModel, PublicPlayerViewModel, PaginatedResponse } from '@/api/types'
import { relativeTime } from '@/lib/utils'
import { cn } from '@/lib/utils'
import { ChatPanel } from '@/pages/Chat'
import { PageLoader } from '@/components/PageLoader'
import { ApiError } from '@/components/ApiError'

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

function MessageBody({ body, isSystemMessage, gameId }: { body: string; isSystemMessage: boolean; gameId: string }) {
  const navigate = useNavigate()

  const handleClick = useCallback(
    (e: React.MouseEvent) => {
      const target = e.target as HTMLElement
      const anchor = target.closest('a')
      if (!anchor) return
      const href = anchor.getAttribute('href')
      if (href && href.startsWith('/battles/')) {
        e.preventDefault()
        const reportId = href.replace('/battles/', '')
        void navigate(`/games/${gameId}/battles/${reportId}`)
      }
    },
    [gameId, navigate],
  )

  // System messages (senderId is null) contain server-generated HTML that is safe
  // to render — player names are HTML-encoded by BattleReportGenerator.
  if (isSystemMessage) {
    return (
      <div
        className="text-sm battle-report-message"
        onClick={handleClick}
        dangerouslySetInnerHTML={{ __html: body }}
      />
    )
  }
  return <p className="text-sm whitespace-pre-wrap">{body}</p>
}

export function Messages({ gameId }: MessagesProps) {
  const queryClient = useQueryClient()
  const [tab, setTab] = useState<'inbox' | 'sent' | 'chat'>('inbox')
  const [selected, setSelected] = useState<MessageViewModel | null>(null)
  const [composing, setComposing] = useState(false)

  const { data: inboxResp, isLoading: inboxLoading, error: inboxError, refetch: refetchInbox } = useQuery({
    queryKey: ['messages-inbox', gameId],
    queryFn: () =>
      apiClient.get<PaginatedResponse<MessageViewModel>>('/api/messages/inbox', { params: { pageSize: 100 } }).then((r) => r.data),
  })
  const inbox = inboxResp ? { messages: inboxResp.items } as MessageInboxViewModel : undefined

  const { data: sentResp } = useQuery({
    queryKey: ['messages-sent', gameId],
    queryFn: () =>
      apiClient.get<PaginatedResponse<MessageViewModel>>('/api/messages/sent', { params: { pageSize: 100 } }).then((r) => r.data),
  })
  const sent = sentResp ? { messages: sentResp.items } as MessageInboxViewModel : undefined

  const { data: playersResp } = useQuery({
    queryKey: ['players-list'],
    queryFn: () =>
      apiClient.get<PaginatedResponse<PublicPlayerViewModel>>('/api/playerranking', { params: { pageSize: 100 } }).then((r) => r.data),
  })
  const players = playersResp?.items ?? []

  // Derive thread partner ID as a stable string — never use the `selected` object
  // directly as a query key, as object identity changes on each render and would
  // cause the thread query to re-run in an infinite loop.
  const threadPartnerId: string | null = selected
    ? tab === 'inbox'
      ? (selected.senderId ?? null)
      : selected.recipientId
    : null

  const { data: thread } = useQuery({
    queryKey: ['messages-thread', gameId, threadPartnerId],
    queryFn: () =>
      apiClient
        .get<MessageThreadViewModel>(`/api/messages/thread/${threadPartnerId}`)
        .then((r) => r.data),
    enabled: !!threadPartnerId,
  })

  const markRead = useMutation({
    mutationFn: (id: string) => apiClient.post(`/api/messages/${id}/read`, {}),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['messages-inbox'] }),
  })

  function openMessage(msg: MessageViewModel) {
    setSelected(msg)
    setComposing(false)
    // Only mark as read for inbox messages — sent messages are owned by the
    // recipient and the server will return 403 if we try to mark them read.
    if (tab === 'inbox' && !msg.isRead) markRead.mutate(msg.messageId)
  }

  const messages = tab === 'inbox' ? inbox?.messages ?? [] : sent?.messages ?? []
  const unread = inbox?.messages.filter((m) => !m.isRead).length ?? 0

  if (inboxLoading) return <PageLoader message="Loading messages..." />
  if (inboxError && !inbox) return <ApiError message="Failed to load messages." onRetry={() => void refetchInbox()} />

  return (
    <div className="max-w-3xl">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold">Messages</h1>
        {tab !== 'chat' && (
          <button
            onClick={() => { setComposing(true); setSelected(null) }}
            className="flex items-center gap-1.5 rounded bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90"
          >
            <PenSquareIcon className="h-4 w-4" />
            Compose
          </button>
        )}
      </div>

      {/* Top-level tabs */}
      <div className="flex border-b mb-4">
        <button
          onClick={() => setTab('inbox')}
          className={cn(
            'flex items-center gap-1.5 px-4 py-2 text-sm border-b-2 -mb-px transition-colors',
            tab === 'inbox' ? 'border-primary text-foreground font-medium' : 'border-transparent text-muted-foreground hover:text-foreground'
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
            'flex items-center gap-1.5 min-h-[44px] px-4 py-2 text-sm border-b-2 -mb-px transition-colors',
            tab === 'sent' ? 'border-primary text-foreground font-medium' : 'border-transparent text-muted-foreground hover:text-foreground'
          )}
        >
          <SendIcon className="h-3.5 w-3.5" />
          Sent
        </button>
        <button
          onClick={() => { setTab('chat'); setComposing(false); setSelected(null) }}
          className={cn(
            'flex items-center gap-1.5 min-h-[44px] px-4 py-2 text-sm border-b-2 -mb-px transition-colors',
            tab === 'chat' ? 'border-primary text-foreground font-medium' : 'border-transparent text-muted-foreground hover:text-foreground'
          )}
        >
          <MessageSquareIcon className="h-3.5 w-3.5" />
          Alliance Chat
        </button>
      </div>

      {tab === 'chat' ? (
        <ChatPanel gameId={gameId} />
      ) : (
      <div className="grid grid-cols-1 md:grid-cols-[240px_1fr] gap-4">
        {/* List pane */}
        <div className="rounded-lg border overflow-hidden">
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
              <div className="border-b pb-2">
                <h3 className="font-semibold">{selected.subject}</h3>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {tab === 'sent'
                    ? <>To: <span className="text-foreground">{selected.recipientName}</span></>
                    : <>From: <span className="text-foreground">{selected.senderName}</span></>
                  }
                </p>
              </div>
              {thread ? (
                <ul className="space-y-2 max-h-80 overflow-y-auto">
                  {thread.messages.map((msg) => {
                    const isMine = msg.senderId !== threadPartnerId && msg.senderId !== null
                    return (
                      <li
                        key={msg.messageId}
                        className={cn(
                          'rounded p-2 text-sm',
                          isMine ? 'ml-6 bg-primary/10' : 'mr-6 bg-secondary'
                        )}
                      >
                        <div className="flex justify-between text-xs text-muted-foreground mb-1">
                          <span>{isMine ? 'You' : msg.senderName}</span>
                          <span>{relativeTime(msg.sentAt)}</span>
                        </div>
                        <MessageBody body={msg.body} isSystemMessage={msg.senderId === null} gameId={gameId} />
                      </li>
                    )
                  })}
                </ul>
              ) : (
                <MessageBody body={selected.body} isSystemMessage={selected.senderId === null} gameId={gameId} />
              )}
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
      )}
    </div>
  )
}
