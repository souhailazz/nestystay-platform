import { useState } from "react";
import { MessageSquare, Send, Paperclip, ShieldCheck, QrCode, PhoneCall, AlertTriangle, Check, DollarSign } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";
import type { ChatThread, ChatMessage } from "./types";

interface MessagingCenterProps {
  token: string;
}

export function MessagingCenter({ token }: MessagingCenterProps) {
  const [threads, setThreads] = useState<ChatThread[]>([
    {
      id: "th-1",
      propertyId: "11111111-1111-4111-8111-111111111111",
      propertyTitle: "Ocho Rios Verified Villa",
      participantName: "Island Villa Hosting",
      participantRole: "Host",
      unreadCount: 1,
      lastMessageAt: new Date().toISOString(),
      messages: [
        {
          id: "m-1",
          senderId: "host-1",
          senderName: "Island Villa Hosting",
          senderRole: "Host",
          content: "Welcome to Ocho Rios! Here is your security gate pass for arrival.",
          sentAt: new Date().toISOString(),
          isRead: true,
          cardType: "GatePass",
          cardPayload: { passCode: "NSTY-GATE-99" }
        }
      ]
    }
  ]);

  const [activeThreadId, setActiveThreadId] = useState<string>("th-1");
  const [inputMessage, setInputMessage] = useState("");

  const activeThread = threads.find(t => t.id === activeThreadId) || threads[0];

  function handleSend() {
    if (!inputMessage.trim() || !activeThread) return;
    const newMsg: ChatMessage = {
      id: `m-${Date.now()}`,
      senderId: "user-current",
      senderName: "Traveler Guest",
      senderRole: "Traveler",
      content: inputMessage.trim(),
      sentAt: new Date().toISOString(),
      isRead: true
    };
    setThreads(threads.map(t => t.id === activeThread.id ? { ...t, messages: [...t.messages, newMsg] } : t));
    setInputMessage("");
  }

  return (
    <div className="page-container container py-6" data-testid="msg-01-page" id="MSG-01">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">MSG-01 through MSG-09</span>
          <h2>Guest & Host Messaging Center</h2>
          <PatoisPhrase phrase="Chat & Stay Coordination" translation="Real-time messaging, gate pass sharing, special offers, and 119 emergency safety alerts." />
        </div>
        {/* MSG-09 Emergency 119 Alert */}
        <button 
          type="button" 
          className="btn btn-ghost text-coral border border-coral font-bold flex items-center gap-1"
          onClick={() => alert("EMERGENCY 119: Contacting Jamaican Police & Security Dispatch")}
          id="MSG-09"
        >
          <PhoneCall size={16} /> 119 Emergency Alert
        </button>
      </header>

      <div className="layout-grid-2-1 h-[650px] border rounded-xl overflow-hidden bg-white">
        {/* Thread List Sidebar (MSG-01) */}
        <div className="border-r overflow-y-auto p-4 space-y-2">
          <h3 className="font-bold text-sm text-gray-500 uppercase mb-3">Conversations</h3>
          {threads.map((t) => (
            <div 
              key={t.id} 
              className={`p-3 rounded-lg cursor-pointer transition ${activeThreadId === t.id ? "bg-sun-light border-l-4 border-sun" : "hover:bg-gray-50"}`}
              onClick={() => setActiveThreadId(t.id)}
            >
              <div className="flex justify-between items-start">
                <strong className="text-sm">{t.participantName}</strong>
                {t.unreadCount > 0 && <span className="badge badge-sun">{t.unreadCount}</span>}
              </div>
              <p className="subtext text-xs mt-1">{t.propertyTitle}</p>
            </div>
          ))}
        </div>

        {/* Active Chat Window (MSG-02) */}
        <div className="flex flex-col h-full bg-gray-50">
          <div className="p-4 bg-white border-b flex justify-between items-center">
            <div>
              <strong className="text-lg">{activeThread?.participantName}</strong>
              <p className="subtext text-xs">{activeThread?.propertyTitle}</p>
            </div>
            <span className="badge badge-green flex items-center gap-1"><ShieldCheck size={12} /> Encrypted Chat</span>
          </div>

          <div className="flex-1 p-4 overflow-y-auto space-y-4">
            {activeThread?.messages.map((m) => (
              <div key={m.id} className={`flex flex-col ${m.senderRole === "Traveler" ? "items-end" : "items-start"}`}>
                <div className={`p-3 rounded-xl max-w-md ${m.senderRole === "Traveler" ? "bg-sun text-white" : "bg-white border shadow-sm"}`}>
                  <p className="text-sm">{m.content}</p>

                  {/* MSG-07 Special Gate Pass Card */}
                  {m.cardType === "GatePass" && (
                    <div className="bg-sun-light p-3 rounded mt-2 border border-sun text-gray-900 text-center" id="MSG-07">
                      <QrCode size={36} className="mx-auto text-sun mb-1" />
                      <strong className="text-xs uppercase">Jamaican Gate Pass</strong>
                      <p className="font-bold text-lg text-sun">{m.cardPayload?.passCode}</p>
                    </div>
                  )}
                </div>
                <span className="text-xs subtext mt-1">{new Date(m.sentAt).toLocaleTimeString()}</span>
              </div>
            ))}
          </div>

          <div className="p-3 bg-white border-t flex gap-2 items-center">
            <input 
              type="text" 
              className="input-control flex-1" 
              placeholder="Write a message to host..." 
              value={inputMessage} 
              onChange={(e) => setInputMessage(e.target.value)} 
              onKeyDown={(e) => e.key === "Enter" && handleSend()} 
            />
            <button type="button" className="btn btn-primary" onClick={handleSend}>
              <Send size={16} />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
