import { useState } from "react";
import { QrCode } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { SampleDataChip } from "../../pages/SpecScreens";
import { cx } from "../../lib/ui";
import type { ChatThread, ChatMessage } from "./types";

interface MessagingCenterProps {
  token: string;
}

/* MSG-01 (DS v2) — guest↔host threads. SPEC screen: the messaging API is still
   to come, so threads are local sample data (chipped); sending appends locally.
   All communication stays on-platform. */
export function MessagingCenter({ token: _token }: MessagingCenterProps) {
  const [threads, setThreads] = useState<ChatThread[]>([
    {
      id: "th-1",
      propertyId: "11111111-1111-4111-8111-111111111111",
      propertyTitle: "Cliffside Retreat",
      participantName: "Marcia — Cliffside Retreat",
      participantRole: "Host",
      unreadCount: 1,
      lastMessageAt: new Date().toISOString(),
      messages: [
        {
          id: "m-1",
          senderId: "host-1",
          senderName: "Marcia",
          senderRole: "Host",
          content: "Welcome Keisha! Anything you need before check-in on the 12th?",
          sentAt: new Date().toISOString(),
          isRead: true,
        },
        {
          id: "m-2",
          senderId: "user-current",
          senderName: "Traveler Guest",
          senderRole: "Traveler",
          content: "Could we get a late check-out on the 18th?",
          sentAt: new Date().toISOString(),
          isRead: true,
        },
        {
          id: "m-3",
          senderId: "host-1",
          senderName: "Marcia",
          senderRole: "Host",
          content: "No problem — 1 pm works. Linens and a welcome basket will be ready.",
          sentAt: new Date().toISOString(),
          isRead: true,
          cardType: "GatePass",
          cardPayload: { passCode: "NSTY-GATE-99" },
        },
      ],
    },
    {
      id: "th-2",
      propertyId: "22222222-2222-4222-8222-222222222222",
      propertyTitle: "Sea Grape Cottage",
      participantName: "Devon — Sea Grape Cottage",
      participantRole: "Host",
      unreadCount: 0,
      lastMessageAt: new Date().toISOString(),
      messages: [
        {
          id: "m-4",
          senderId: "host-2",
          senderName: "Devon",
          senderRole: "Host",
          content: "The gate code changes Friday.",
          sentAt: new Date().toISOString(),
          isRead: true,
        },
      ],
    },
  ]);

  const [activeThreadId, setActiveThreadId] = useState<string>("th-1");
  const [inputMessage, setInputMessage] = useState("");

  const activeThread = threads.find((t) => t.id === activeThreadId) || threads[0];

  function handleSend() {
    if (!inputMessage.trim() || !activeThread) return;
    const newMsg: ChatMessage = {
      id: `m-${Date.now()}`,
      senderId: "user-current",
      senderName: "Traveler Guest",
      senderRole: "Traveler",
      content: inputMessage.trim(),
      sentAt: new Date().toISOString(),
      isRead: true,
    };
    setThreads(threads.map((t) => (t.id === activeThread.id ? { ...t, messages: [...t.messages, newMsg] } : t)));
    setInputMessage("");
  }

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="msg-01-page" id="MSG-01">
      <div className="flex flex-wrap items-center gap-2.5">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">Messages</h1>
        <SampleDataChip />
      </div>

      <div className="grid min-h-[480px] overflow-hidden rounded-card border border-sand-border bg-cream md:grid-cols-[minmax(240px,320px)_1fr]">
        <div className="flex flex-col border-b border-sand-border md:border-b-0 md:border-r">
          {threads.map((thread) => (
            <button
              className={cx(
                "flex min-h-11 w-full cursor-pointer flex-col gap-0.5 border-b border-shell px-4 py-3.5 text-left font-sans",
                activeThreadId === thread.id ? "bg-shell" : "bg-transparent hover:bg-shell/60",
              )}
              key={thread.id}
              onClick={() => setActiveThreadId(thread.id)}
              type="button"
            >
              <span className="flex justify-between gap-2">
                <span className="text-[13.5px] font-bold text-ink">{thread.participantName}</span>
                {thread.unreadCount > 0 && (
                  <span className="inline-flex size-5 shrink-0 items-center justify-center rounded-full bg-deep text-[10.5px] font-bold text-yellow">
                    {thread.unreadCount}
                  </span>
                )}
              </span>
              <span className="truncate text-[12.5px] text-gray-600">
                {thread.messages[thread.messages.length - 1]?.content}
              </span>
            </button>
          ))}
        </div>

        <div className="flex flex-col">
          <div className="flex flex-wrap items-center justify-between gap-2.5 border-b border-shell px-5 py-3.5">
            <div>
              <div className="text-sm font-bold">{activeThread?.participantName}</div>
              <div className="text-[11.5px] text-sand-500">Verified host · responds in ~1 hr</div>
            </div>
            <span className="inline-flex items-center rounded-pill bg-success-tint px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-success-text">
              BOOKING: CONFIRMED
            </span>
          </div>

          <div className="flex flex-1 flex-col gap-3 p-5">
            {activeThread?.messages.map((message) => (
              <div
                className={cx("flex max-w-[70%] flex-col", message.senderRole === "Traveler" ? "items-end self-end" : "items-start self-start")}
                key={message.id}
              >
                <div
                  className={cx(
                    "px-4 py-3 text-[13.5px]",
                    message.senderRole === "Traveler"
                      ? "rounded-[16px_16px_4px_16px] bg-deep text-on-dark-heading"
                      : "rounded-[16px_16px_16px_4px] bg-shell text-ink",
                  )}
                >
                  {message.content}
                  {message.cardType === "GatePass" && (
                    <div className="mt-2.5 flex flex-col items-center gap-1 rounded-field border border-sand-border bg-cream px-4 py-3 text-center text-ink">
                      <QrCode className="text-deep-hover" size={34} />
                      <strong className="text-[10.5px] uppercase tracking-[0.08em]">Gate pass</strong>
                      <span className="font-mono text-lg font-bold text-deep">{message.cardPayload?.passCode}</span>
                    </div>
                  )}
                </div>
                <span className="mt-1 text-[11px] text-sand-500">
                  {new Date(message.sentAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                </span>
              </div>
            ))}
          </div>

          <div className="flex gap-2.5 border-t border-shell px-5 py-3.5">
            <input
              className="min-h-12 flex-1 rounded-pill border-[1.5px] border-sand-input bg-white px-[18px] font-sans text-sm text-ink outline-none focus:border-deep-hover"
              onChange={(event) => setInputMessage(event.target.value)}
              onKeyDown={(event) => event.key === "Enter" && handleSend()}
              placeholder="Write a message…"
              type="text"
              value={inputMessage}
            />
            <button
              className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
              onClick={handleSend}
              type="button"
            >
              Send
            </button>
          </div>
        </div>
      </div>

      <div className="text-[12.5px] text-sand-500">
        Document sharing in threads →{" "}
        <AppLink className="font-semibold text-deep-hover" href="/messages/document">
          MSG-DOC
        </AppLink>
      </div>
    </div>
  );
}
