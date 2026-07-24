export interface BookingQuoteRequest {
  propertyId: string;
  checkIn: string;
  checkOut: string;
  adults?: number;
  children?: number;
  accessibilityNeeds?: string;
  protectionPlan?: string;
}

export interface BookingPriceLine {
  code: string;
  description: string;
  amount: number;
  currency: string;
  isRefundable: boolean;
}

export interface BookingQuote {
  property: {
    id: string;
    title: string;
    location: string;
    country: string;
    hostName: string;
    badgeLevel: string;
    guestVerificationEnabled: boolean;
    insuraGuestEnabled: boolean;
    cancellationPolicy: string;
  };
  checkIn: string;
  checkOut: string;
  nights: number;
  nightlyRate: number;
  staySubtotal: number;
  guestPlatformFee: number;
  totalAmount: number;
  currency: string;
  requiresGuestVerification: boolean;
  datesAvailable: boolean;
  holdExpiresAt?: string;
  priceBreakdown: BookingPriceLine[];
}

export interface CreateBookingPayload {
  propertyId: string;
  guestUserId: string;
  checkIn: string;
  checkOut: string;
  adults?: number;
  children?: number;
  accessibilityNeeds?: string;
  protectionPlan?: string;
  billingCountry?: string;
  termsAccepted?: boolean;
  ekycMetaInfo?: string;
  documentType?: string;
  ekycCallbackUrl?: string;
}

export interface BookingNotification {
  recipientType: string;
  recipient: string;
  subject: string;
  queuedAt: string;
}

export interface BookingDetails {
  id: string;
  propertyId: string;
  hostUserId: string;
  guestUserId: string;
  checkIn: string;
  checkOut: string;
  status: string;
  verificationStatus: string;
  paymentStatus: string;
  requiresGuestVerification: boolean;
  datesHeld: boolean;
  holdExpiresAt?: string;
  nights: number;
  nightlyRate: number;
  staySubtotal: number;
  guestPlatformFee: number;
  totalAmount: number;
  currency: string;
  propertyTitle?: string;
  hostName?: string;
  ekycProvider?: string;
  ekycTransactionId?: string;
  ekycTransactionUrl?: string;
  paymentProvider?: string;
  paymentAuthorizationReference?: string;
  paymentClientSecret?: string;
  paymentCaptureReference?: string;
  paymentRefundReference?: string;
  refundedAmount: number;
  refundReason?: string;
  refundedAt?: string;
  priceBreakdown: BookingPriceLine[];
  notifications: BookingNotification[];
  timeline: string[];
}
