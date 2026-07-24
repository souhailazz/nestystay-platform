export interface ChatMessage {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: "Traveler" | "Host" | "Admin" | "System";
  content: string;
  sentAt: string;
  isRead: boolean;
  cardType?: "Inquiry" | "SpecialOffer" | "GatePass" | "WellnessCheck" | "SafetyAlert" | "LinkMi";
  cardPayload?: any;
  attachmentUrl?: string;
  attachmentName?: string;
}

export interface ChatThread {
  id: string;
  propertyId: string;
  propertyTitle: string;
  participantName: string;
  participantRole: string;
  unreadCount: number;
  lastMessageAt: string;
  messages: ChatMessage[];
}
