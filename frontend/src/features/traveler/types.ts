export interface WishlistItem {
  id: string;
  propertyId: string;
  title: string;
  location: string;
  nightlyRate: number;
  currency: string;
  imageUrl?: string;
  addedAt: string;
  sortOrder: number;
}

export interface WishlistCollection {
  id: string;
  name: string;
  items: WishlistItem[];
  createdAt: string;
  sortOrder: number;
}

export interface SavedPaymentMethod {
  id: string;
  brand: string;
  last4: string;
  expMonth: number;
  expYear: number;
  isDefault: boolean;
  createdAt: string;
}

export interface TravelerProfile {
  userId: string;
  displayName: string;
  email: string;
  phone?: string;
  photoUrl?: string;
  patoisPreference: boolean;
  accessibilityPreferences: string[];
  notificationPreferences: {
    email: boolean;
    sms: boolean;
    push: boolean;
  };
  identityStatus: "NotSubmitted" | "Pending" | "Verified" | "Expired" | "Rejected";
  identityVerifiedAt?: string;
  identityExpiresAt?: string;
}

export interface ReviewItem {
  id: string;
  bookingId: string;
  propertyTitle: string;
  rating: number;
  comment: string;
  createdAt: string;
  hostReply?: string;
  canEdit: boolean;
}

export interface NotificationItem {
  id: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}
