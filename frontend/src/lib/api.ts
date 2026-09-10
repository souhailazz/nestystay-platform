export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "/api").replace(/\/+$/, "");

export type UserRole = "Guest" | "Host" | "Owner" | "Admin" | "Officer" | "ServiceProvider" | "LocalBusiness" | "PropertyManager";

export type AdminPermission =
  | "super_administration"
  | "booking_management"
  | "refund_management"
  | "payment_management"
  | "user_management"
  | "property_moderation"
  | "officer_management"
  | "financial_reporting"
  | "audit_log_access"
  | "system_configuration";

export type RegisterUserRequest = {
  email: string;
  password: string;
  displayName: string;
  phone?: string;
  confirmPassword: string;
  acceptedTerms: boolean;
  acceptedPrivacy: boolean;
  role: Extract<UserRole, "Guest" | "Host" | "Owner" | "PropertyManager" | "Officer" | "ServiceProvider" | "LocalBusiness">;
};

export type RegisterUserResponse = {
  userId: string;
  email: string;
  displayName: string;
  requiresTwoFactor: boolean;
};

export type LoginRequest = {
  email: string;
  password: string;
  deviceName?: string;
  rememberDevice?: boolean;
  userAgent?: string;
  ipAddress?: string;
};

export type LoginResponse = {
  userId: string;
  email: string;
  requiresTwoFactor: boolean;
  challengeId?: string | null;
  challengeExpiresAt?: string | null;
  accessToken?: string | null;
  expiresAt?: string | null;
  roles?: UserRole[] | null;
  permissions?: AdminPermission[] | null;
};

export type IntegrationStatus = {
  key: string;
  provider: string;
  status: string;
  detail: string;
};

export type VerifyTwoFactorResponse = {
  userId: string;
  accessToken: string;
  expiresAt: string;
  roles: UserRole[];
  permissions?: AdminPermission[] | null;
};

export type TwoFactorEnrollment = {
  enrollmentId: string;
  manualKey: string;
  otpAuthUri: string;
  expiresAt: string;
};

export type ConfirmTwoFactorEnrollmentResponse = {
  enabled: boolean;
  recoveryCodes: string[];
};

export type DisableTwoFactorResponse = {
  disabled: boolean;
};

export type GoogleSignInRequest = {
  credential: string;
  role?: Extract<UserRole, "Guest" | "Host">;
};

export type GoogleSignInResponse = VerifyTwoFactorResponse & {
  email: string;
  displayName: string;
  provider: "Google" | string;
};

export type PasswordResetRequestResponse = {
  requestId: string;
  message: string;
  expiresAt: string;
};

export type CompletePasswordResetResponse = {
  status: string;
  passwordChanged: boolean;
};

export type UserProfile = {
  userId: string;
  email: string;
  displayName: string;
  phone?: string | null;
  roles: UserRole[];
  isTwoFactorEnabled: boolean;
  photo?: UserProfilePhoto | null;
};

export type UserSession = {
  id: string;
  deviceName: string;
  browser: string;
  approximateLocation?: string | null;
  issuedAt: string;
  lastUsedAt: string;
  expiresAt: string;
  isCurrent: boolean;
  isTrusted: boolean;
  trustedUntil?: string | null;
  isRevoked: boolean;
};

export type SmsTwoFactorChallenge = {
  flowId: string;
  challengeId: string;
  maskedPhone: string;
  expiresAt: string;
  attemptsRemaining: number;
};

export type Passkey = {
  id: string;
  label: string;
  createdAt: string;
  lastUsedAt?: string | null;
  isActive: boolean;
};

export type UserProfilePhoto = {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: string;
  scanStatus: string;
  uploadedAt: string;
  sha256Hash?: string | null;
};

export type ProfilePhotoUpload = {
  id: string;
  userId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  objectKey: string;
  uploadUrl: string;
  status: string;
  scanStatus: string;
  expiresAt: string;
  sha256Hash?: string | null;
};

export type ProfilePhotoDownload = {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  url: string;
  expiresAt: string;
};

export type PropertyListing = {
  id: string;
  hostUserId: string;
  hostName: string;
  title: string;
  location: string;
  country: string;
  nightlyRate: number;
  currency: string;
  badgeLevel: string;
  guestVerificationEnabled: boolean;
  insuraGuestEnabled: boolean;
  cancellationPolicy: string;
  highlights: string[];
  isArchived?: boolean;
  isDraft?: boolean;
  minimumNights?: number;
  imageUrl?: string;
};

export type PropertyRevision = {
  id: string;
  propertyId: string;
  version: number;
  snapshotJson: string;
  createdAt: string;
  createdByUserId?: string | null;
};

export type CalendarFeed = {
  id: string;
  propertyId: string;
  feedUrl: string;
  status: string;
  lastSyncAttemptAt?: string | null;
  lastSyncAt?: string | null;
  lastError?: string | null;
  blockCount: number;
  nextSyncAt?: string | null;
  etag?: string | null;
};

export type CalendarSyncEvent = {
  id: string;
  status: string;
  blockCount: number;
  error?: string | null;
  startedAt: string;
  completedAt?: string | null;
};

export type PropertyAvailabilityDay = { date: string; status: string; source: string; label?: string | null };
export type PropertyAvailability = { propertyId: string; from: string; to: string; days: PropertyAvailabilityDay[] };

export type PropertyPhotoUpload = {
  id: string;
  propertyId: string;
  hostUserId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  objectKey: string;
  uploadUrl: string;
  status: string;
  scanStatus: string;
  expiresAt: string;
  sha256Hash?: string | null;
};

export type CreatePropertyRequest = {
  hostUserId: string;
  hostName: string;
  hostEmail: string;
  title: string;
  location: string;
  country: string;
  nightlyRate: number;
  currency: string;
  badgeLevel: string;
  guestVerificationEnabled: boolean;
  insuraGuestEnabled: boolean;
  cancellationPolicy: string;
  highlights: string[];
};

export type UpdatePropertyRequest = Omit<CreatePropertyRequest, "hostUserId">;

export type BookingPriceLine = {
  code: string;
  description: string;
  amount: number;
  currency: string;
  isRefundable: boolean;
};

export type BookingQuote = {
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
  holdExpiresAt?: string | null;
  priceBreakdown: BookingPriceLine[];
};

export type Booking = {
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
  holdExpiresAt?: string | null;
  nights: number;
  nightlyRate: number;
  staySubtotal: number;
  guestPlatformFee: number;
  totalAmount: number;
  currency: string;
  propertyTitle?: string | null;
  hostName?: string | null;
  ekycProvider?: string | null;
  ekycTransactionId?: string | null;
  ekycTransactionUrl?: string | null;
  paymentProvider?: string | null;
  paymentAuthorizationReference?: string | null;
  paymentClientSecret?: string | null;
  paymentCaptureReference?: string | null;
  paymentRefundReference?: string | null;
  refundedAmount: number;
  refundReason?: string | null;
  refundedAt?: string | null;
  priceBreakdown: BookingPriceLine[];
  notifications: {
    recipientType: string;
    recipient: string;
    subject: string;
    queuedAt: string;
  }[];
  timeline: string[];
};

export type BookingQuoteRequest = {
  propertyId: string;
  checkIn: string;
  checkOut: string;
  adults?: number;
  children?: number;
  accessibilityNeeds?: string;
  protectionPlan?: string;
};

export type CreateBookingRequest = BookingQuoteRequest & {
  guestUserId: string;
  billingCountry?: string;
  termsAccepted?: boolean;
  ekycMetaInfo?: string;
  documentType?: string;
  ekycCallbackUrl?: string;
};

export type BadgeLevel = "Free" | "Verified" | "Trusted" | "Wellness";
export type BadgeAssignmentStatus = "Active" | "Expired" | "Suspended";
export type PaymentStatus = "Pending" | "Authorized" | "Captured" | "Cancelled" | "Failed";
export type FoundingTier = "Standard" | "Silver" | "Gold" | "Platinum";

export type PhaseTwoPricebookItem = {
  key: string;
  label: string;
  amount: number;
  currency: string;
  cadence: string;
  appliesTo: string;
  isConfigurable: boolean;
  isActive: boolean;
  activeFrom?: string | null;
  activeTo?: string | null;
};

export type UpdatePricebookItemRequest = {
  amount: number;
  currency?: string | null;
  cadence?: string | null;
  activeFrom?: string | null;
  activeTo?: string | null;
  isActive?: boolean;
};

export type BadgeDefinition = {
  id: string;
  key: string;
  level: BadgeLevel;
  appliesTo: string;
  annualPrice: number;
  currency: string;
  unlocks: string[];
  priceCadence?: string;
};

export type PurchaseBadgeRequest = {
  subjectType: string;
  subjectId: string;
  level: BadgeLevel;
  campaignKey?: string | null;
  hostVerificationPassed?: boolean;
  completedApprovedBookings?: number;
  hasPropertyAddress?: boolean;
  hasWellnessSubscription?: boolean;
  paymentSucceeded?: boolean;
};

export type BadgeEligibility = {
  level: BadgeLevel;
  eligible: boolean;
  missingRequirements: string[];
};

export type BadgeAssignment = {
  id: string;
  badgeKey: string;
  level: BadgeLevel;
  subjectType: string;
  subjectId: string;
  status: BadgeAssignmentStatus | string;
  earnedAt: string;
  paidThrough: string;
  expiresAt: string;
  amountCharged: number;
  currency: string;
  paymentStatus: PaymentStatus | string;
  paymentReference: string;
  unlocks: string[];
};

export type BadgeFeatureAccess = {
  subjectType: string;
  subjectId: string;
  activeLevel: BadgeLevel;
  unlockedFeatures: string[];
  lockedFeatures: string[];
};

export type BadgeRenewal = {
  id: string;
  badgeAssignmentId: string;
  reminderDueAt: string;
  paymentAttemptedAt?: string | null;
  paymentStatus: PaymentStatus | string;
  amountDue: number;
  currency: string;
};

export type Campaign = {
  id: string;
  key: string;
  name: string;
  campaignType: string;
  overrideAmount?: number | null;
  appliesTo?: string | null;
  opensAt?: string | null;
  closesAt?: string | null;
  isActive: boolean;
};

export type CreateCampaignRequest = {
  key: string;
  name: string;
  campaignType: string;
  overrideAmount?: number | null;
  appliesTo?: string | null;
  opensAt?: string | null;
  closesAt?: string | null;
  isActive?: boolean;
};

export type CampaignEnrollment = {
  id: string;
  campaignKey: string;
  subjectType: string;
  subjectId: string;
  enrolledAt: string;
};

export type FoundingBenefit = {
  propertyId: string;
  tier: FoundingTier;
  guestFlatFee: number;
  hostCommissionPercent: number;
  isLifetimeGuestFee: boolean;
  isTransferableWithProperty: boolean;
  isForfeited: boolean;
};

export type FoundingBenefitRequest = {
  propertyId: string;
  tier: FoundingTier;
  isEligible?: boolean;
};

export type FoundingTransferEvaluationRequest = {
  previousOwnerVerified: boolean;
  previousOwnerTrusted: boolean;
  hasPropertyId: boolean;
  hasCurrentTaxReceipt: boolean;
};

export type FoundingTransferEvaluation = {
  canTransfer: boolean;
  missingRequirements: string[];
};

export type CommissionQuoteRequest = {
  bookingValue: number;
  nights: number;
  tier?: FoundingTier;
};

export type CommissionQuote = {
  bookingValue: number;
  nights: number;
  tier: FoundingTier;
  hostCommissionPercent: number;
  hostCommissionAmount: number;
  guestFeeAmount: number;
  guestFeeDescription: string;
  nestyStayRevenue: number;
};

export type WellnessOfficer = {
  id: string;
  userId?: string | null;
  badgeNumber: string;
  parish: string;
  coverageArea: string;
  isActiveOffDuty: boolean;
  isRetired: boolean;
  verificationStatus: string;
  onboardingStatus: string;
  availabilityStatus: string;
  freeBadges: string[];
  createdAt: string;
  updatedAt: string;
  adminReviewSummary?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  serviceRadiusKm?: number | null;
};

export type OnboardOfficerRequest = {
  userId?: string | null;
  badgeNumber: string;
  parish: string;
  coverageArea: string;
  isActiveOffDuty: boolean;
  isRetired: boolean;
  verificationMetadata?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  serviceRadiusKm?: number | null;
};

export type WellnessQuote = {
  hostUserId: string;
  propertyId: string;
  visitType: string;
  scheduledAt: string;
  durationMinutes: number;
  price: number;
  platformFee: number;
  officerPayoutAmount: number;
  currency: string;
  eligible: boolean;
  missingRequirements: string[];
  emergencyNumber: string;
};

export type WellnessQuoteRequest = {
  hostUserId: string;
  propertyId: string;
  visitType: string;
  scheduledAt: string;
  parish: string;
  area?: string | null;
};

export type CreateWellnessVisitRequest = WellnessQuoteRequest;

export type WellnessVisit = {
  id: string;
  hostUserId: string;
  propertyId: string;
  officerId?: string | null;
  officerBadgeNumber?: string | null;
  parish: string;
  area: string;
  visitType: string;
  scheduledAt: string;
  durationMinutes: number;
  price: number;
  platformFee: number;
  officerPayoutAmount: number;
  currency: string;
  paymentStatus: string;
  visitStatus: string;
  reportStatus: string;
  paymentAuthorizationReference?: string | null;
  paymentCaptureReference?: string | null;
  timeline: string[];
  createdAt: string;
  updatedAt: string;
  scheduledTimeZone?: string;
};

export type WellnessReportPhotoUpload = {
  id: string;
  visitId: string;
  officerId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  objectKey: string;
  uploadUrl: string;
  status: string;
  scanStatus: string;
  expiresAt: string;
  sha256Hash?: string | null;
};

export type WellnessPayout = {
  id: string;
  visitId: string;
  officerId: string;
  grossAmount: number;
  platformFee: number;
  officerAmount: number;
  currency: string;
  status: string;
  eligibleAt?: string | null;
  paidAt?: string | null;
  providerReference?: string | null;
};

export type WellnessAdminDashboard = {
  pendingOfficers: number;
  verifiedOfficers: number;
  requestedVisits: number;
  scheduledVisits: number;
  completedVisits: number;
  pendingPayouts: number;
  pendingPayoutAmount: number;
  officerQueue: WellnessOfficer[];
  recentVisits: WellnessVisit[];
  payouts: WellnessPayout[];
};

export type PublicContentPage = {
  slug: string;
  title: string;
  kind: string;
  summary: string;
  body: string;
  sections: string[];
  links: string[];
};

export type Experience = {
  id: string;
  slug: string;
  name: string;
  category: string;
  parish: string;
  providerName: string;
  price: number;
  currency: string;
  durationMinutes: number;
  rating: number;
  summary: string;
  description: string;
  images: string[];
  included: string[];
  rules: string[];
  availability: string[];
};

export type JournalArticle = {
  id: string;
  slug: string;
  title: string;
  category: string;
  author: string;
  publishedAt: string;
  summary: string;
  body: string;
  tags: string[];
  relatedSlugs: string[];
};

export type HostProfile = {
  id: string;
  hostUserId: string;
  slug: string;
  displayName: string;
  parish: string;
  bio: string;
  responseTime: string;
  badges: BadgeLevel[];
  listingIds: string[];
  rating: number;
  reviewCount: number;
  isPublic: boolean;
  highlights: string[];
};

export type TravelerWorkspace = {
  userId: string;
  wishlistCollections: WishlistCollection[];
  paymentMethods: TravelerPaymentMethod[];
  identityDocuments: IdentityDocument[];
  reviews: TravelerReview[];
  notifications: TravelerNotification[];
};

export type WishlistCollection = {
  id: string;
  userId: string;
  name: string;
  sortOrder: number;
  items: WishlistItem[];
};

export type WishlistItem = {
  id: string;
  collectionId: string;
  userId: string;
  propertyId: string;
  propertyTitle: string;
  status: string;
  sortOrder: number;
  createdAt: string;
};

export type TravelerPaymentMethod = {
  id: string;
  userId: string;
  providerName: string;
  providerPaymentMethodReference: string;
  brand: string;
  last4: string;
  expMonth: number;
  expYear: number;
  isDefault: boolean;
  createdAt: string;
};

export type PaymentMethodSetupIntent = {
  providerName: string;
  setupIntentReference: string;
  clientSecret: string;
  status: string;
  expiresAt: string;
  publishableKey?: string | null;
};

export type IdentityDocument = {
  id: string;
  userId: string;
  documentType: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: string;
  scanStatus: string;
  uploadedAt: string;
  issuingCountry?: string | null;
  expiresOn?: string | null;
};

export type IdentityDocumentUpload = {
  id: string;
  userId: string;
  documentType: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  objectKey: string;
  uploadUrl: string;
  status: string;
  scanStatus: string;
  expiresAt: string;
  sha256Hash?: string | null;
  identityDocumentId?: string | null;
};

export type TravelerReview = {
  id: string;
  userId: string;
  propertyId?: string | null;
  bookingId?: string | null;
  subjectTitle: string;
  rating: number;
  text: string;
  status: string;
  hostReply?: string | null;
  createdAt: string;
  editableUntil: string;
};

export type TravelerNotification = {
  id: string;
  userId: string;
  type: string;
  title: string;
  body: string;
  deepLink: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
};

export type DirectoryProvider = {
  id: string;
  slug: string;
  kind: string;
  category: string;
  name: string;
  parish: string;
  badgeLevel: string;
  description: string;
  availabilitySummary: string;
  contactMode: string;
  rating: number;
  reviewCount: number;
  isActive: boolean;
  ownerUserId?: string | null;
  verificationStatus?: string;
  status?: string;
  isBrickAndMortar?: boolean;
  policeBadgeNumber?: string | null;
  services?: string[] | null;
  openingHours?: string | null;
  emergencyAvailable?: boolean;
  serviceRadiusKm?: number | null;
  weeklyHoursJson?: string;
  holidayClosuresJson?: string;
  promotionsJson?: string;
  accessibilityInfo?: string | null;
  createdAt?: string;
  updatedAt?: string;
};

export type DirectoryQuote = { id: string; providerId: string; providerSlug: string; requesterUserId: string; scope: string; preferredAt?: string | null; budget?: number | null; responseAmount?: number | null; status: string; message?: string | null; createdAt: string; expiresAt?: string | null; respondedAt?: string | null };
export type DirectoryReview = { id: string; providerId: string; providerSlug: string; reviewerUserId: string; rating: number; body: string; status: string; providerResponse?: string | null; createdAt: string; respondedAt?: string | null };
export type DirectoryProviderInsights = { providerSlug: string; quoteRequests: number; acceptedQuotes: number; reviews: number; averageRating: number; responses: number; quotes: DirectoryQuote[]; reviewsList: DirectoryReview[] };

export type DirectoryProviderDocument = {
  id: string;
  providerId: string;
  documentType: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: string;
  scanStatus: string;
  uploadedAt?: string | null;
  createdAt: string;
};

export type DirectoryProviderDocumentUpload = DirectoryProviderDocument & {
  objectKey: string;
  uploadUrl: string;
  expiresAt: string;
  sha256Hash?: string | null;
};

export type WellnessReport = {
  id: string;
  visitId: string;
  officerId: string;
  submittedAt: string;
  reportStatus: string;
  notes: string;
  photos: string[];
};

export type WellnessOfficerDocument = {
  id: string;
  officerId: string;
  documentType: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: string;
  scanStatus: string;
  expiresOn?: string | null;
  reviewStatus: string;
  reviewReason?: string | null;
  createdAt: string;
  uploadedAt?: string | null;
};
export type WellnessOfficerDocumentUpload = WellnessOfficerDocument & { objectKey: string; uploadUrl: string; expiresAt: string; sha256Hash?: string | null };
export type WellnessReportTemplate = { id: string; name: string; version: number; definitionJson: string; isActive: boolean; createdAt: string; createdByUserId: string };
export type WellnessReportComment = { id: string; reportId: string; authorUserId: string; body: string; createdAt: string };
export type WellnessReportCollaboration = { reportId: string; comments: WellnessReportComment[]; acknowledgement?: { reportId: string; acknowledgedByUserId: string; acknowledgedAt: string } | null; followUpTasks: WellnessFollowUpTask[] };
export type WellnessFollowUpTask = { id: string; reportId: string; visitId: string; propertyId: string; title: string; description: string; priority: string; assigneeUserId?: string | null; dueAt?: string | null; status: string; createdAt: string };
export type WellnessPayoutStatement = { officerUserId: string; from: string; to: string; grossTotal: number; platformFeeTotal: number; officerTotal: number; rows: Array<{ payoutId: string; visitId: string; eligibleAt?: string | null; grossAmount: number; platformFee: number; officerAmount: number; currency: string; status: string; paidAt?: string | null; providerReference?: string | null }>; format: string; downloadFileName?: string | null; downloadBase64?: string | null };
export type WellnessPayoutDispute = { id: string; payoutId: string; officerId: string; reason: string; evidenceJson?: string | null; status: string; decision?: string | null; decisionNotes?: string | null; decidedByUserId?: string | null; createdAt: string; resolvedAt?: string | null };

export type WellnessSubscription = {
  id: string;
  hostUserId: string;
  planKey: string;
  monthlyAmount: number;
  currency: string;
  status: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  includedVisits: number;
  usedVisits: number;
  remainingVisits: number;
  paymentProvider: string;
  paymentReference: string;
};

export type QrIssueResult = {
  id: string;
  bookingId: string;
  propertyId: string;
  validFrom: string;
  expiresAt: string;
  status: string;
  token: string;
  validationUrl: string;
};

export type QrAccess = {
  id: string;
  bookingId: string;
  propertyId: string;
  validFrom: string;
  expiresAt: string;
  isRevoked: boolean;
  validationCount: number;
  lastValidatedAt?: string | null;
  status: string;
  revokeReason?: string | null;
  token?: string | null;
  validationUrl?: string | null;
};

export type QrHistoryEvent = {
  id: string;
  qrAccessCodeId: string;
  eventType: string;
  status: string;
  occurredAt: string;
  reason?: string | null;
  deviceMetadata?: string | null;
};

export type QrValidationResult = {
  valid: boolean;
  result: string;
  bookingId?: string | null;
  propertyId?: string | null;
  validFrom?: string | null;
  expiresAt?: string | null;
  validationCount: number;
  message: string;
};

export type MessagingInbox = {
  userId: string;
  conversations: ConversationSummary[];
};

export type ConversationSummary = {
  id: string;
  subject: string;
  participantLabel: string;
  lastMessage: string;
  updatedAt: string;
  unreadCount: number;
  isSupportThread: boolean;
  onlineStatus: string;
};

export type Conversation = {
  id: string;
  subject: string;
  bookingId?: string | null;
  isSupportThread: boolean;
  participants: ConversationParticipant[];
  messages: Message[];
};

export type ConversationParticipant = {
  userId: string;
  displayName: string;
  role: string;
  lastReadAt?: string | null;
  onlineStatus: string;
};

export type MessageAttachment = {
  attachmentId?: string | null;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  url?: string | null;
  status: string;
  objectKey?: string | null;
  expiresAt?: string | null;
  scanStatus?: string | null;
  thumbnailUrl?: string | null;
};

export type AttachmentUpload = {
  id: string;
  conversationId: string;
  ownerUserId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  objectKey: string;
  uploadUrl: string;
  status: string;
  expiresAt: string;
  storageProviderName: string;
  scanStatus: string;
  sha256Hash?: string | null;
  thumbnailUrl?: string | null;
};

export type AttachmentDownload = {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  url: string;
  expiresAt: string;
};

export type Message = {
  id: string;
  conversationId: string;
  senderUserId: string;
  body: string;
  status: string;
  sentAt: string;
  readAt?: string | null;
  attachments: MessageAttachment[];
};

export type HostOperations = {
  hostUserId: string;
  analytics: HostAnalytics;
  pricingRules: HostPricingRule[];
  promotions: HostPromotion[];
  reviews: TravelerReview[];
  payouts?: HostPayoutSummary | null;
};

export type HostPayout = { id: string; bookingId: string; grossAmount: number; platformFee: number; netAmount: number; currency: string; status: string; eligibleAt?: string | null; paidAt?: string | null; settlementReference?: string | null; notes: string };
export type HostPayoutSummary = { pendingAmount: number; availableAmount: number; paidAmount: number; currency: string; settlementMode: string; history: HostPayout[] };

export type TravelerRecommendation = { propertyId: string; propertyTitle: string; location: string; country: string; nightlyRate: number; currency: string; badgeLevel: string; highlights: string[]; score: number; reason: string; isDismissed: boolean; generatedAt: string };
export type TravelerPreference = { userId: string; preferredParish?: string | null; maximumNightlyRate?: number | null; preferredBadgeLevel?: string | null; preferredHighlights: string[]; updatedAt: string };

export type HostAnalytics = {
  revenue: number;
  occupancyPercent: number;
  averageNightlyRate: number;
  bookingCount: number;
  conversionPercent: number;
  revenueSeries: ChartPoint[];
  occupancySeries: ChartPoint[];
};

export type ChartPoint = { label: string; value: number };

export type HostPricingRule = {
  id: string;
  hostUserId: string;
  propertyId: string;
  name: string;
  startsOn: string;
  endsOn: string;
  nightlyRate: number;
  minimumStay: number;
  isActive: boolean;
};

export type HostPromotion = {
  id: string;
  hostUserId: string;
  propertyId: string;
  name: string;
  discountPercent: number;
  startsOn: string;
  endsOn: string;
  minimumNights: number;
  badgeLevel: string;
  isActive: boolean;
};

export type AdminOperations = {
  cases: AdminCase[];
  auditEvents: AuditEvent[];
  metrics: { label: string; value: string }[];
};

export type AdminCase = {
  id: string;
  caseType: string;
  subjectType: string;
  subjectId?: string | null;
  status: string;
  priority: string;
  reason: string;
  assignedTo: string;
  resolutionNotes: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
  evidence: AdminCaseEvidence[];
};

export type AdminCaseEvidence = {
  id: string;
  caseId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: string;
  scanStatus: string;
  uploadedAt: string;
  sha256Hash?: string | null;
};

export type AdminCaseEvidenceUpload = {
  id: string;
  caseId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  objectKey: string;
  uploadUrl: string;
  status: string;
  scanStatus: string;
  expiresAt: string;
  sha256Hash?: string | null;
};

export type AdminCaseEvidenceDownload = {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  url: string;
  expiresAt: string;
};

export type AuditEvent = {
  id: string;
  actorUserId?: string | null;
  actorRole: string;
  action: string;
  subjectType: string;
  subjectId?: string | null;
  reason: string;
  createdAt: string;
  effectivePermission?: string | null;
  correlationId?: string | null;
  previousStateJson?: string | null;
  newStateJson?: string | null;
};

export type AuthFlowResult = {
  id: string;
  userId?: string | null;
  flowType: string;
  destination: string;
  status: string;
  deliveryChannel: string;
  expiresAt: string;
  lastSentAt?: string | null;
  attemptsRemaining: number;
  message?: string | null;
};

export type PasswordlessLoginResponse = {
  userId: string;
  email: string;
  displayName: string;
  accessToken?: string | null;
  expiresAt: string;
  roles: UserRole[];
  permissions?: AdminPermission[] | null;
};

export type SocialAuthConfig = {
  googleEnabled: boolean;
  appleEnabled: boolean;
  facebookEnabled: boolean;
  requiredEnvironmentVariables: string[];
};

type RequestOptions = Omit<RequestInit, "body"> & {
  body?: unknown;
  token?: string;
};

const unsafeMethods = new Set(["POST", "PUT", "PATCH", "DELETE"]);

function readCookie(name: string): string | undefined {
  if (typeof document === "undefined") return undefined;
  const prefix = `${encodeURIComponent(name)}=`;
  const value = document.cookie.split(";").map((part) => part.trim()).find((part) => part.startsWith(prefix));
  return value ? decodeURIComponent(value.slice(prefix.length)) : undefined;
}

type UploadOptions = {
  signal?: AbortSignal;
  onProgress?: (progress: number) => void;
};

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
    public readonly retryAfterSeconds?: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export type DownloadedFile = {
  blob: Blob;
  fileName: string;
  contentType: string;
};

export type PropertyManagerOwner = { id: string; ownerUserId: string; displayName: string; email: string; verificationStatus: string; invitationStatus: string; communityId?: string | null };
export type PropertyManagerProperty = { id: string; ownerUserId: string; communityId?: string | null; title: string; unitNumber: string; address: string; status: string; occupancyStatus: string; rentalListingId?: string | null };
export type PropertyManagerInvoiceLine = { id: string; description: string; quantity: number; unitAmount: number; amount: number };
export type PropertyManagerInvoice = { id: string; ownerUserId: string; propertyId?: string | null; invoiceNumber: string; issueDate: string; dueDate: string; subtotal: number; tax: number; total: number; amountPaid: number; balance: number; currency: string; status: string; lines: PropertyManagerInvoiceLine[] };
export type PropertyManagerUtility = { id: string; ownerUserId: string; propertyId: string; utilityType: string; billingPeriod: string; usage: number; rate: number; amount: number; invoiceId?: string | null; status: string };
export type PropertyManagerMaintenance = { id: string; ownerUserId: string; propertyId: string; vendorId?: string | null; title: string; description: string; category: string; urgency: string; status: string; scheduledAt?: string | null; cost: number; notes: string };
export type PropertyManagerVendor = { id: string; name: string; category: string; contact: string; verificationStatus: string; isActive: boolean; notes: string; serviceAreas?: string[] | null; rate?: number | null; rating?: number; isPreferred?: boolean; isSuspended?: boolean; completedJobCount?: number; spendTotal?: number };
export type PropertyManagerNotice = { id: string; communityId?: string | null; targetOwnerUserId?: string | null; title: string; body: string; publishAt: string; expiresAt?: string | null; isPinned: boolean; isArchived: boolean; category?: string; audienceRoles?: string[]; audienceOwnerIds?: string[]; acknowledgementDueAt?: string | null };
export type PropertyManagerProposal = { id: string; communityId?: string | null; title: string; description: string; opensAt: string; closesAt: string; status: string; isAnonymous: boolean; quorum?: number | null; eligibleVoters: number; votesCast: number; results: Record<string, number> };
export type PropertyManagerDocument = { id: string; ownerUserId?: string | null; propertyId?: string | null; title: string; category: string; fileName: string; contentType: string; sizeBytes: number; accessScope: string; isArchived: boolean; expiresOn?: string | null; createdAt: string };
export type PropertyManagerDocumentDownload = { id: string; fileName: string; contentType: string; sizeBytes: number; url: string; expiresAt: string };
export type PropertyManagerDocumentExport = { id: string; status: string; documentCount: number; fileName?: string | null; url?: string | null; error?: string | null; createdAt: string; completedAt?: string | null; expiresAt?: string | null };
export type PropertyManagerGateMessage = { id: string; communityId?: string | null; propertyId?: string | null; recipient: string; message: string; visitorType: string; validFrom: string; validUntil: string };
export type PropertyManagerSubscription = { managerUserId: string; businessName: string; subscriptionTier: string; monthlyAmount: number; subscriptionStatus: string; nextBillingAt: string; pendingSubscriptionTier?: string | null; pendingSubscriptionEffectiveAt?: string | null; autoRenew: boolean; billingProviderStatus: string; unitLimit?: number | null; unitsUsed: number; cancellationReason?: string | null };
export type PropertyManagerSubscriptionEvent = { id: string; eventType: string; fromTier: string; toTier: string; status: string; reason?: string | null; effectiveAt: string };
export type PropertyManagerDashboard = { manager: PropertyManagerSubscription; totalOwners: number; totalProperties: number; outstandingBalance: number; invoicesDue: number; openMaintenance: number; pendingVerification: number; gateActivity: number; owners: PropertyManagerOwner[]; properties: PropertyManagerProperty[]; invoices: PropertyManagerInvoice[]; maintenance: PropertyManagerMaintenance[]; utilities: PropertyManagerUtility[]; vendors: PropertyManagerVendor[]; notices: PropertyManagerNotice[]; proposals: PropertyManagerProposal[]; documents: PropertyManagerDocument[]; gateMessages: PropertyManagerGateMessage[] };
export type PropertyManagerStatement = { ownerUserId: string; from: string; to: string; openingBalance: number; entries: { date: string; type: string; description: string; amount: number; invoiceId?: string | null }[]; closingBalance: number; invoices: PropertyManagerInvoice[]; payments: { id: string; invoiceId: string; amount: number; provider: string; providerReference: string; status: string; createdAt: string }[] };
export type PropertyManagerOwnerPortal = { ownerUserId: string; properties: PropertyManagerProperty[]; invoices: PropertyManagerInvoice[]; statement: PropertyManagerStatement; utilities: PropertyManagerUtility[]; maintenance: PropertyManagerMaintenance[]; notices: PropertyManagerNotice[]; proposals: PropertyManagerProposal[]; documents: PropertyManagerDocument[] };
export type PropertyManagerQr = { id: string; token: string; subjectType: string; propertyId?: string | null; validFrom: string; validUntil: string };
export type PropertyManagerQrAccessRecord = { id: string; subjectType: string; propertyId?: string | null; validFrom: string; validUntil: string; isRevoked: boolean; validationCount: number; lastValidatedAt?: string | null; status: string };
export type PropertyManagerQrScan = { id: string; qrAccessId: string; gateGuardUserId?: string | null; propertyId?: string | null; result: string; scannedAt: string };
export type PropertyManagerQrValidation = { result: string; status: string; propertyId?: string | null; subjectType: string; validUntil?: string | null; qrId?: string | null; message?: string | null };
export type PropertyManagerPaymentOperation = { id: string; invoiceId: string; ownerUserId: string; amount: number; refundedAmount: number; provider: string; providerReference: string; status: string; reconciliationStatus: string; reconciliationReference?: string | null; refundReason?: string | null; createdAt: string };
export type PropertyManagerMeterReading = { id: string; ownerUserId: string; propertyId: string; utilityType: string; billingPeriod: string; previousReading: number; currentReading: number; usage: number; isAnomaly: boolean; status: string; createdAt: string };
export type PropertyManagerReport = { from: string; to: string; propertiesManaged: number; owners: number; openMaintenance: number; openWorkOrders: number; grossInvoiceRevenue: number; paymentRevenue: number; maintenanceSpend: number; utilityRevenue: number; outstandingBalance: number; pmFeeRevenue: number; invoiceIds: string[]; maintenanceIds: string[] };
export type PropertyManagerWorkOrder = { id: string; propertyId: string; ownerUserId: string; vendorId?: string | null; workOrderNumber: string; scope: string; status: string; quoteAmount?: number | null; approvedAmount?: number | null; laborAmount: number; partsAmount: number; slaDueAt?: string | null; scheduledAt?: string | null };
export type PropertyManagerCalendarEvent = { id: string; propertyId?: string | null; ownerUserId?: string | null; eventType: string; title: string; startsAt: string; endsAt: string; status: string; sourceType: string };

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers);

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (options.token) {
    headers.set("Authorization", `Bearer ${options.token}`);
  }

  if (unsafeMethods.has((options.method ?? "GET").toUpperCase())) {
    const csrf = readCookie("nestyStay.csrf");
    if (csrf) headers.set("X-CSRF-Token", csrf);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    credentials: "include",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (!response.ok) {
    throw await buildApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function requestFile(path: string, token?: string): Promise<DownloadedFile> {
  const headers = new Headers();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  const response = await fetch(`${API_BASE_URL}${path}`, { headers, credentials: "include" });

  if (!response.ok) {
    throw await buildApiError(response);
  }

  return {
    blob: await response.blob(),
    fileName: parseContentDispositionFileName(response.headers.get("Content-Disposition")) ?? "nestystay-booking-document.pdf",
    contentType: response.headers.get("Content-Type") ?? "application/octet-stream",
  };
}

function requestUpload<T>(path: string, token: string | undefined, file: File, options: UploadOptions = {}): Promise<T> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    const abort = () => xhr.abort();
    const cleanup = () => options.signal?.removeEventListener("abort", abort);

    xhr.open("PUT", `${API_BASE_URL}${path}`);
    xhr.withCredentials = true;
    xhr.responseType = "json";
    if (token) {
      xhr.setRequestHeader("Authorization", `Bearer ${token}`);
    }
    xhr.setRequestHeader("Content-Type", file.type || "application/octet-stream");
    const csrf = readCookie("nestyStay.csrf");
    if (csrf) xhr.setRequestHeader("X-CSRF-Token", csrf);

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        options.onProgress?.(Math.round((event.loaded / event.total) * 100));
      }
    };

    xhr.onload = () => {
      cleanup();
      if (xhr.status >= 200 && xhr.status < 300) {
        options.onProgress?.(100);
        resolve(xhr.response as T);
        return;
      }

      const problem = xhr.response as ApiProblem | null;
      reject(new ApiError(
        problem?.title ?? problem?.detail ?? problem?.message ?? xhr.statusText ?? `Request failed with status ${xhr.status}`,
        xhr.status,
        readProblemCode(problem),
        parseRetryAfterHeader(xhr.getResponseHeader("Retry-After")),
      ));
    };

    xhr.onerror = () => {
      cleanup();
      reject(new ApiError("Attachment upload failed.", xhr.status || 0));
    };

    xhr.onabort = () => {
      cleanup();
      reject(new ApiError("Attachment upload cancelled.", 0));
    };

    options.signal?.addEventListener("abort", abort, { once: true });
    xhr.send(file);
  });
}

function parseContentDispositionFileName(value: string | null): string | null {
  if (!value) return null;
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(value);
  if (utf8Match?.[1]) return decodeURIComponent(utf8Match[1].trim().replace(/^"|"$/g, ""));
  const asciiMatch = /filename=([^;]+)/i.exec(value);
  return asciiMatch?.[1]?.trim().replace(/^"|"$/g, "") ?? null;
}

type ApiProblem = {
  title?: string;
  detail?: string;
  message?: string;
  code?: string;
  extensions?: { code?: string };
};

async function buildApiError(response: Response): Promise<ApiError> {
  let problem: ApiProblem | null = null;
  let message = `Request failed with status ${response.status}`;
  try {
    problem = (await response.json()) as ApiProblem;
    message = problem.title ?? problem.detail ?? problem.message ?? message;
  } catch {
    message = response.statusText || message;
  }

  return new ApiError(
    message,
    response.status,
    readProblemCode(problem),
    parseRetryAfterHeader(response.headers.get("Retry-After")),
  );
}

function readProblemCode(problem: ApiProblem | null): string | undefined {
  return problem?.code ?? problem?.extensions?.code;
}

function parseRetryAfterHeader(value: string | null): number | undefined {
  if (!value) return undefined;
  const seconds = Number(value);
  if (Number.isFinite(seconds) && seconds > 0) {
    return Math.ceil(seconds);
  }

  const retryAt = Date.parse(value);
  if (!Number.isNaN(retryAt)) {
    return Math.max(1, Math.ceil((retryAt - Date.now()) / 1000));
  }

  return undefined;
}

export type P0OwnerProfile = { id: string; managerUserId: string; ownerUserId: string; status: string; legalName: string; contactEmail: string; contactPhone: string; billingAddress: string; preferredCurrency: "JMD" | "USD" | string; timeZone: string; billingMetadataJson: string; paymentProviderCustomerReference?: string | null; operationalMetadataJson: string; notes: string; version: number; activatedAt?: string | null; suspendedAt?: string | null; archivedAt?: string | null };
export type P0OwnerLifecycleEvent = { id: string; ownerUserId: string; eventType: string; fromStatus: string; toStatus: string; reason: string; actorUserId: string; createdAt: string };
export type P0Portfolio = { items: P0PortfolioRow[]; total: number; page: number; pageSize: number };
export type P0PortfolioRow = { propertyId: string; ownerUserId: string; ownerName: string; ownerEmail: string; ownerStatus: string; propertyTitle: string; unitNumber: string; address: string; propertyStatus: string; ownershipChangedAt?: string | null };
export type PropertyAssignmentHistoryDto = { id: string; propertyId: string; previousOwnerUserId?: string | null; newOwnerUserId: string; actorUserId: string; reason: string; batchId: string; changedAt: string };
export type P0Agreement = { id: string; managerUserId: string; ownerUserId: string; propertyId?: string | null; version: number; supersedesAgreementId?: string | null; status: string; effectiveFrom: string; effectiveTo?: string | null; currency: string; termsJson: string; feeRuleJson: string; maintenanceApprovalLimit: number; expenseApprovalLimit: number; documentId?: string | null; documentKey?: string | null; activatedAt?: string | null; terminatedAt?: string | null; terminationReason?: string | null; rowVersion: number };
export type P0FeeRule = { id: string; managerUserId: string; ownerUserId: string; propertyId?: string | null; category: string; ruleType: string; calculationBasis: string; currency: string; percentage: number; fixedAmount: number; minimumAmount: number; cleaningMarkup: number; maintenanceMarkup: number; effectiveFrom: string; effectiveTo?: string | null; isActive: boolean; rowVersion: number };
export type P0FeeCalculation = { ruleId?: string | null; category: string; currency: string; baseAmount: number; calculatedAmount: number; percentageAmount: number; fixedAmount: number; minimumTopUp: number; markupAmount: number; on: string; sourceId?: string | null; posted: boolean; journalId?: string | null };
export type P0Account = { id: string; code: string; name: string; accountType: string; currency: string; ownerUserId?: string | null; propertyId?: string | null; isClientMoney: boolean; isPmMoney: boolean; isThirdParty: boolean; status: string };
export type P0JournalLine = { id: string; accountId: string; accountCode: string; ownerUserId?: string | null; propertyId?: string | null; debit: number; credit: number; description: string; sourceReference: string };
export type P0Journal = { id: string; managerUserId: string; journalNumber: string; sourceType: string; sourceId?: string | null; idempotencyKey?: string | null; currency: string; accountingDate: string; memo: string; status: string; reconciliationStatus: string; totalDebit: number; totalCredit: number; reversalOfJournalId?: string | null; postedAt?: string | null; lines: P0JournalLine[] };
export type P0Reconciliation = { id: string; journalId: string; externalReference: string; status: string; amount: number; currency: string; reason: string; actorUserId: string; reconciledAt: string };
export type P0StatementEntry = { date: string; sourceType: string; sourceId?: string | null; description: string; currency: string; income: number; expenses: number; managementFees: number; payouts: number; net: number; journalId: string };
export type P0Statement = { snapshotId?: string | null; managerUserId: string; ownerUserId: string; propertyId?: string | null; currency: string; from: string; to: string; status: string; openingBalance: number; income: number; expenses: number; managementFees: number; payouts: number; closingBalance: number; hasUnresolvedSuspense: boolean; entries: P0StatementEntry[]; contentHash?: string | null };
export type P0Profitability = { currency: string; from: string; to: string; income: number; expenses: number; managementFees: number; payouts: number; ownerNet: number; pmMargin: number; cashCollected: number; unreconciledCash: number; rows: { ownerUserId: string; propertyId?: string | null; income: number; expenses: number; managementFees: number; ownerNet: number; pmMargin: number }[] };
export type P0Approval = { id: string; managerUserId: string; ownerUserId: string; propertyId?: string | null; agreementId?: string | null; feeRuleId?: string | null; approvalType: string; description: string; amount: number; threshold: number; currency: string; status: string; evidenceJson: string; decisionReason?: string | null; decidedByUserId?: string | null; decidedAt?: string | null; rowVersion: number; history: { id: string; actorUserId: string; eventType: string; fromStatus: string; toStatus: string; reason: string; createdAt: string }[] };
export type P0StaffMembership = { id: string; managerUserId: string; staffUserId: string; role: string; propertyIds: string[]; ownerIds: string[]; canManageFinance: boolean; canApprovePayouts: boolean; approvalLimit: number; status: string; acceptedAt?: string | null; suspendedAt?: string | null; revokedAt?: string | null; rowVersion: number };
export type P0StaffEvent = { id: string; membershipId: string; actorUserId: string; eventType: string; fromStatus: string; toStatus: string; reason: string; createdAt: string };
export type P0PayoutAvailability = { managerUserId: string; ownerUserId: string; currency: string; from: string; to: string; income: number; expenses: number; managementFees: number; existingPayouts: number; availableReconciledCash: number; reservedInDraftOrProcessing: number; payableAmount: number; hasUnresolvedSuspense: boolean };
export type P0PayoutBatch = { id: string; managerUserId: string; ownerUserId: string; currency: string; periodFrom: string; periodTo: string; amount: number; reservedAmount: number; status: string; idempotencyKey?: string | null; providerReference?: string | null; failureReason?: string | null; approvedByUserId?: string | null; approvedAt?: string | null; processedAt?: string | null; cancelledAt?: string | null; statementSnapshotId?: string | null; rowVersion: number; items: { id: string; ownerUserId: string; propertyId?: string | null; amount: number; statementSnapshotId?: string | null; sourceJson: string }[]; history: { id: string; actorUserId: string; eventType: string; fromStatus: string; toStatus: string; reason: string; providerReference?: string | null; createdAt: string }[] };
export type P0OwnerPortal = { managerUserId: string; profile?: P0OwnerProfile | null; properties: PropertyManagerProperty[]; agreements: P0Agreement[]; approvals: P0Approval[]; statement: P0Statement; transactions: P0Journal[]; payouts: P0PayoutBatch[]; propertyFinancials: { ownerUserId: string; propertyId?: string | null; income: number; expenses: number; managementFees: number; ownerNet: number; pmMargin: number }[] };
export type PmOwnerBlock = { id: string; ownerUserId: string; propertyId: string; startsAt: string; endsAt: string; timeZone: string; category: string; reason: string; notes: string; status: string; bookingId?: string | null; rowVersion: number };
export type PmOwnerBlockHistory = { id: string; blockId: string; actorUserId: string; action: string; reason: string; createdAt: string };
export type PmReservation = { bookingId: string; propertyId: string; guestUserId: string; hostUserId: string; checkIn: string; checkOut: string; status: string; paymentStatus: string; totalAmount: number; currency: string; notes: PmReservationNote[]; guestName?: string; guestEmail?: string; propertyTitle?: string | null; updatedAt?: string | null };
export type PmReservationNote = { id: string; bookingId: string; authorUserId: string; body: string; visibility: string; createdAt: string };
export type PmReservationEvent = { id: string; bookingId: string; actorUserId: string; eventType: string; fromStatus: string; toStatus: string; reason: string; createdAt: string };
export type PmReservationDateChangePreview = { bookingId: string; currentCheckIn: string; currentCheckOut: string; proposedCheckIn: string; proposedCheckOut: string; currentNights: number; proposedNights: number; currentTotal: number; proposedTotal: number; currency: string; allowed: boolean; blockingReason?: string | null };
export type PmCalendarItem = { type: string; sourceId: string; propertyId: string; startsAt: string; endsAt: string; title: string; status: string; ownerUserId?: string | null };
export type PmOperationalDashboard = { reservations: number; occupiedNights: number; portfolioNights: number; occupancyPercent: number; openMaintenance: number; openWorkOrders: number; notReady: number; upcomingInspections: number; openIncidents: number; anomalousUtilities: number; pendingApprovals: number; activeVendors: number; overdueActions: number };
export type PmMaintenanceCase = { id: string; ownerUserId: string; propertyId: string; vendorId?: string | null; number: string; title: string; description: string; status: string; priority: string; selectedQuoteAmount?: number | null; expenseAmount: number; ownerCharge: number; managerFee: number; scheduledAt?: string | null; currency: string; rowVersion: number; selectedQuoteId?: string | null; financiallyPosted?: boolean; financialJournalId?: string | null };
export type PmMaintenanceQuote = { id: string; maintenanceId: string; vendorId: string; amount: number; currency: string; scope: string; status: string; expiresAt?: string | null };
export type PmMaintenanceEvent = { id: string; maintenanceId: string; actorUserId: string; eventType: string; fromStatus: string; toStatus: string; details: string; createdAt: string };
export type PmCleaning = { id: string; propertyId: string; bookingId?: string | null; assignedUserId?: string | null; vendorId?: string | null; dueAt: string; status: string; checklistJson: string; photosJson: string; issues: string; completedAt?: string | null; rowVersion: number; templateName?: string; templateVersion?: number };
export type PmAsset = { id: string; propertyId: string; assetTag: string; name: string; description: string; serialReference: string; purchaseDate?: string | null; purchaseCost?: number | null; warrantyExpiry?: string | null; condition: string; category: string; status: string; quantity: number; location: string; metadataJson: string; photosJson: string; retiredAt?: string | null; rowVersion: number };
export type PmIncident = { id: string; propertyId: string; bookingId?: string | null; incidentType: string; severity: string; occurredAt: string; description: string; involvedPartiesJson: string; evidenceJson: string; actionTaken: string; followUp: string; financialImpact: number; insuranceReference?: string | null; status: string; resolvedAt?: string | null; rowVersion: number };
export type PmInspection = { id: string; propertyId: string; assignedUserId?: string | null; inspectionType: string; scheduledAt: string; checklistJson: string; evidenceJson: string; findingsJson: string; status: string; signedOffAt?: string | null; rowVersion: number; correctiveWorkOrderId?: string | null };

function withQuery(path: string, params: Record<string, string | number | boolean | undefined>) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") {
      search.set(key, String(value));
    }
  });
  const query = search.toString();
  return query ? `${path}?${query}` : path;
}

export const api = {
  health: () =>
    request<{
      service: string;
      status: string;
      architecture: string;
      database: string;
      openApi: string;
    }>("/health"),
  integrationStatus: (token: string) =>
    request<{ generatedAt: string; services: IntegrationStatus[] }>("/health/integrations", { token }),
  register: (body: RegisterUserRequest) =>
    request<RegisterUserResponse>("/auth/register", { method: "POST", body }),
  login: (body: LoginRequest) => request<LoginResponse>("/auth/login", { method: "POST", body, headers: { "X-Session-Mode": "cookie" } }),
  googleSignIn: (body: GoogleSignInRequest) =>
    request<GoogleSignInResponse>("/auth/google", { method: "POST", body, headers: { "X-Session-Mode": "cookie" } }),
  verifyTwoFactor: (challengeId: string, code: string, options?: { deviceName?: string; rememberDevice?: boolean; userAgent?: string; ipAddress?: string }) =>
    request<VerifyTwoFactorResponse>("/auth/2fa/verify", {
      method: "POST",
      body: { challengeId, code, ...options },
      headers: { "X-Session-Mode": "cookie" },
    }),
  requestSmsTwoFactor: (challengeId: string) =>
    request<SmsTwoFactorChallenge>("/auth/2fa/sms/request", { method: "POST", body: { challengeId }, headers: { "X-Session-Mode": "cookie" } }),
  verifySmsTwoFactor: (body: { challengeId: string; flowId: string; code: string; deviceName?: string; rememberDevice?: boolean; userAgent?: string; ipAddress?: string }) =>
    request<VerifyTwoFactorResponse>("/auth/2fa/sms/verify", { method: "POST", body, headers: { "X-Session-Mode": "cookie" } }),
  getDevelopmentTwoFactorCode: (challengeId: string) =>
    request<{ challengeId: string; code: string; expiresAt: string }>(`/auth/development/challenges/${challengeId}`),
  beginTwoFactorEnrollment: (token: string) =>
    request<TwoFactorEnrollment>("/auth/2fa/enrollments", { method: "POST", token }),
  confirmTwoFactorEnrollment: (token: string, body: { enrollmentId: string; code: string }) =>
    request<ConfirmTwoFactorEnrollmentResponse>("/auth/2fa/enrollments/confirm", { method: "POST", token, body }),
  disableTwoFactor: (token: string, body: { code: string }) =>
    request<DisableTwoFactorResponse>("/auth/2fa", { method: "DELETE", token, body }),
  logout: (token?: string) =>
    request<{ loggedOut: boolean; invalidatedAt: string }>("/auth/logout", { method: "POST", token }),
  getSessions: (token?: string) => request<UserSession[]>("/auth/sessions", { token }),
  revokeSession: (sessionId: string, token?: string) => request<UserSession>(`/auth/sessions/${sessionId}`, { method: "DELETE", token }),
  revokeOtherSessions: (token?: string) => request<{ revoked: number }>("/auth/sessions/revoke-others", { method: "POST", token }),
  getPasskeys: (token?: string) => request<Passkey[]>("/auth/passkeys", { token }),
  beginPasskeyRegistration: (token?: string) => request<{ challengeId: string; options: PublicKeyCredentialCreationOptions; expiresAt: string }>("/auth/passkeys/register/options", { method: "POST", token }),
  completePasskeyRegistration: (body: { challengeId: string; response: unknown; label?: string }, token?: string) => request<Passkey>("/auth/passkeys/register/complete", { method: "POST", body, token }),
  beginPasskeyAssertion: (email?: string) => request<{ challengeId: string; options: PublicKeyCredentialRequestOptions; expiresAt: string }>("/auth/passkeys/assertion/options", { method: "POST", body: { email } }),
  completePasskeyAssertion: (body: { challengeId: string; response: unknown }) => request<VerifyTwoFactorResponse & { email: string; displayName: string }>("/auth/passkeys/assertion/complete", { method: "POST", body, headers: { "X-Session-Mode": "cookie" } }),
  removePasskey: (id: string, token?: string) => request<void>(`/auth/passkeys/${id}`, { method: "DELETE", token }),
  getProfile: (token?: string) => request<UserProfile>("/auth/profile", { token }),
  updateProfile: (token: string | undefined, body: { displayName: string; phone?: string | null }) =>
    request<UserProfile>("/auth/profile", { method: "PATCH", token, body }),
  prepareProfilePhotoUpload: (token: string | undefined, body: { fileName: string; contentType: string; sizeBytes: number }) =>
    request<ProfilePhotoUpload>("/auth/profile/photo/uploads", { method: "POST", token, body }),
  uploadProfilePhotoContent: (token: string | undefined, photoId: string, file: File, options?: UploadOptions) =>
    requestUpload<ProfilePhotoUpload>(`/auth/profile/photo/uploads/${photoId}/content`, token, file, options),
  getProfilePhotoDownload: (token: string | undefined, photoId: string) =>
    request<ProfilePhotoDownload>(`/auth/profile/photo/${photoId}/download`, { token }),
  requestPasswordReset: (email: string) =>
    request<PasswordResetRequestResponse>("/auth/password-reset/request", {
      method: "POST",
      body: { email },
    }),
  completePasswordReset: (body: { requestId: string; token: string; newPassword: string; confirmPassword: string }) =>
    request<CompletePasswordResetResponse>("/auth/password-reset/complete", { method: "POST", body }),
  requestPasswordlessLogin: (email: string) =>
    request<AuthFlowResult>("/auth/passwordless/request", { method: "POST", body: { email }, headers: { "X-Session-Mode": "cookie" } }),
  completePasswordlessLogin: (body: { flowId: string; token?: string; code?: string }) =>
    request<PasswordlessLoginResponse>("/auth/passwordless/complete", { method: "POST", body, headers: { "X-Session-Mode": "cookie" } }),
  getDevelopmentPasswordResetToken: (requestId: string) =>
    request<{ requestId: string; token: string; expiresAt: string }>(`/auth/development/password-resets/${requestId}`),
  getProperties: () => request<PropertyListing[]>("/properties"),
  getOwnedProperties: (token: string) => request<PropertyListing[]>("/properties/owned", { token }),
  getProperty: (id: string) => request<PropertyListing>(`/properties/${id}`),
  createProperty: (body: CreatePropertyRequest, token: string) =>
    request<PropertyListing>("/properties", { method: "POST", token, body }),
  updateProperty: (id: string, token: string, body: UpdatePropertyRequest) =>
    request<PropertyListing>(`/properties/${id}`, { method: "PUT", token, body }),
  preparePropertyPhotoUpload: (propertyId: string, token: string, body: { fileName: string; contentType: string; sizeBytes: number; sortOrder?: number }) =>
    request<PropertyPhotoUpload>(`/properties/${propertyId}/photos/uploads`, { method: "POST", token, body }),
  uploadPropertyPhotoContent: (propertyId: string, photoId: string, token: string, file: File, options?: UploadOptions) =>
    requestUpload<PropertyPhotoUpload>(`/properties/${propertyId}/photos/${photoId}/content`, token, file, options),
  archiveProperty: (id: string, token: string) =>
    request<PropertyListing>(`/properties/${id}/archive`, { method: "POST", token }),
  restoreProperty: (id: string, token: string) =>
    request<PropertyListing>(`/properties/${id}/restore`, { method: "POST", token }),
  duplicateProperty: (id: string, token: string, title?: string) =>
    request<PropertyListing>(`/properties/${id}/duplicate`, { method: "POST", token, body: { title } }),
  bulkArchiveProperties: (propertyIds: string[], token: string, isArchived = true) =>
    request<PropertyListing[]>("/properties/bulk/archive", { method: "POST", token, body: { propertyIds, isArchived } }),
  previewBulkEditProperties: (body: { propertyIds: string[]; nightlyRate?: number; cancellationPolicy?: string; guestVerificationEnabled?: boolean; insuraGuestEnabled?: boolean }, token: string) =>
    request<{ requestedCount: number; matchedCount: number; propertyIds: string[]; changedFields: string[] }>("/properties/bulk/edit/preview", { method: "POST", token, body }),
  bulkEditProperties: (body: { propertyIds: string[]; nightlyRate?: number; cancellationPolicy?: string; guestVerificationEnabled?: boolean; insuraGuestEnabled?: boolean; idempotencyKey?: string }, token: string) =>
    request<PropertyListing[]>("/properties/bulk/edit", { method: "POST", token, body }),
  getPropertyAvailability: (propertyId: string, from?: string, to?: string) =>
    request<PropertyAvailability>(withQuery(`/properties/${propertyId}/availability`, { from, to })),
  publishProperty: (id: string, token: string) =>
    request<PropertyListing>(`/properties/${id}/publish`, { method: "POST", token }),
  getPropertyRevisions: (id: string, token: string) =>
    request<PropertyRevision[]>(`/properties/${id}/revisions`, { token }),
  restorePropertyRevision: (id: string, revisionId: string, token: string) =>
    request<PropertyListing>(`/properties/${id}/revisions/${revisionId}/restore`, { method: "POST", token }),
  getCalendarFeeds: (propertyId: string, token: string) => request<CalendarFeed[]>(`/properties/${propertyId}/calendar/feeds`, { token }),
  connectCalendarFeed: (propertyId: string, token: string, feedUrl: string) => request<CalendarFeed>(`/properties/${propertyId}/calendar/feeds`, { method: "POST", token, body: { feedUrl } }),
  syncCalendarFeed: (propertyId: string, feedId: string, token: string, icsContent?: string) => request<CalendarFeed>(`/properties/${propertyId}/calendar/feeds/${feedId}/sync`, { method: "POST", token, body: { icsContent } }),
  disconnectCalendarFeed: (propertyId: string, feedId: string, token: string) => request<void>(`/properties/${propertyId}/calendar/feeds/${feedId}`, { method: "DELETE", token }),
  getCalendarFeedHistory: (propertyId: string, feedId: string, token: string) => request<CalendarSyncEvent[]>(`/properties/${propertyId}/calendar/feeds/${feedId}/history`, { token }),
  exportCalendarUrl: (propertyId: string) => `${API_BASE_URL}/properties/${propertyId}/calendar/export.ics`,
  deleteProperty: (id: string, token: string) =>
    request<void>(`/properties/${id}`, { method: "DELETE", token }),
  getBookings: (token?: string) =>
    request<Booking[]>("/bookings", { token }),
  getBooking: (id: string, token?: string) => request<Booking>(`/bookings/${id}`, { token }),
  quoteBooking: (body: BookingQuoteRequest) =>
    request<BookingQuote>("/bookings/quote", { method: "POST", body }),
  getBookingQuote: (body: BookingQuoteRequest) =>
    request<BookingQuote>("/bookings/quote", { method: "POST", body }),
  createBooking: (body: CreateBookingRequest, token: string) =>
    request<Booking>("/bookings", { method: "POST", body, token }),
  resolveVerification: (bookingId: string, passed: boolean, providerReference: string, token: string) =>
    request<Booking>(`/bookings/${bookingId}/verification-result`, {
      method: "POST",
      body: { passed, providerReference },
      token,
    }),
  capturePayment: (bookingId: string, token: string) =>
    request<Booking>(`/bookings/${bookingId}/capture-payment`, { method: "POST", token }),
  refundPayment: (bookingId: string, token: string, body: { amount?: number; reason?: string; idempotencyKey?: string }) =>
    request<Booking>(`/bookings/${bookingId}/refund-payment`, { method: "POST", token, body }),
  downloadBookingInvoice: (bookingId: string, token: string) =>
    requestFile(`/bookings/${bookingId}/invoice`, token),
  downloadBookingReceipt: (bookingId: string, token: string) =>
    requestFile(`/bookings/${bookingId}/receipt`, token),
  getPlatformModules: () => request<unknown[]>("/platform/modules"),
  getPlatformPortals: () => request<unknown[]>("/platform/portals"),
  getPlatformVendors: () => request<unknown[]>("/platform/vendors"),
  getBookingWorkflow: () => request<unknown>("/platform/booking-workflow"),
  getPricebook: () => request<PhaseTwoPricebookItem[]>("/badges-pricing/pricebook"),
  getBackendTables: () => request<unknown[]>("/backend-schema/tables"),
  getBackendRules: () => request<{ area: string; rule: string }[]>("/backend-schema/rules"),
  getBackendSeedPricebook: () => request<unknown[]>("/backend-schema/seed/pricebook"),
  getBackendJobs: () => request<unknown[]>("/backend-jobs"),
  getBadgePricebook: () => request<PhaseTwoPricebookItem[]>("/badges-pricing/pricebook"),
  getBadgePricebookItem: (key: string) =>
    request<PhaseTwoPricebookItem>(`/badges-pricing/pricebook/${encodeURIComponent(key)}`),
  updateBadgePricebookItem: (key: string, body: UpdatePricebookItemRequest, token: string) =>
    request<PhaseTwoPricebookItem>(`/badges-pricing/pricebook/${encodeURIComponent(key)}`, {
      method: "PUT",
      body,
      token,
    }),
  getBadgeDefinitions: () => request<BadgeDefinition[]>("/badges-pricing/badges"),
  getBadgeEligibility: (body: PurchaseBadgeRequest, token: string) =>
    request<BadgeEligibility>("/badges-pricing/badges/eligibility", { method: "POST", body, token }),
  purchaseBadge: (body: PurchaseBadgeRequest, token: string) =>
    request<BadgeAssignment>("/badges-pricing/badges/purchase", { method: "POST", body, token }),
  getBadgeAssignments: (token: string, subjectType?: string, subjectId?: string) =>
    request<BadgeAssignment[]>(
      withQuery("/badges-pricing/badges/assignments", { subjectType, subjectId }),
      { token },
    ),
  getBadgeFeatureAccess: (subjectType: string, subjectId: string, token: string) =>
    request<BadgeFeatureAccess>(
      `/badges-pricing/badges/features/${encodeURIComponent(subjectType)}/${encodeURIComponent(subjectId)}`,
      { token },
    ),
  expireBadgeAssignment: (assignmentId: string, token: string, reason?: string) =>
    request<BadgeAssignment>(`/badges-pricing/badges/assignments/${assignmentId}/expire${reason ? `?reason=${encodeURIComponent(reason)}` : ""}`, {
      method: "POST",
      token,
    }),
  suspendBadgeAssignment: (assignmentId: string, token: string, reason?: string) =>
    request<BadgeAssignment>(`/badges-pricing/badges/assignments/${assignmentId}/suspend${reason ? `?reason=${encodeURIComponent(reason)}` : ""}`, {
      method: "POST",
      token,
    }),
  getBadgeRenewals: (token: string, assignmentId?: string) =>
    request<BadgeRenewal[]>(withQuery("/badges-pricing/renewals", { assignmentId }), { token }),
  payBadgeRenewal: (assignmentId: string, token: string) =>
    request<BadgeAssignment>(`/badges-pricing/renewals/${assignmentId}/pay`, { method: "POST", token }),
  getCampaigns: () => request<Campaign[]>("/badges-pricing/campaigns"),
  createCampaign: (body: CreateCampaignRequest, token: string) =>
    request<Campaign>("/badges-pricing/campaigns", { method: "POST", body, token }),
  enrollCampaign: (campaignKey: string, subjectType: string, subjectId: string, token: string) =>
    request<CampaignEnrollment>(`/badges-pricing/campaigns/${encodeURIComponent(campaignKey)}/enroll`, {
      method: "POST",
      body: { subjectType, subjectId },
      token,
    }),
  upsertFoundingBenefit: (body: FoundingBenefitRequest, token: string) =>
    request<FoundingBenefit>("/badges-pricing/founding-benefits", { method: "POST", body, token }),
  getFoundingBenefit: (propertyId: string, token: string) =>
    request<FoundingBenefit>(`/badges-pricing/founding-benefits/${propertyId}`, { token }),
  evaluateFoundingTransfer: (body: FoundingTransferEvaluationRequest) =>
    request<FoundingTransferEvaluation>("/badges-pricing/founding-benefits/transfer-evaluation", {
      method: "POST",
      body,
    }),
  quoteCommission: (body: CommissionQuoteRequest) =>
    request<CommissionQuote>("/badges-pricing/commission-quote", { method: "POST", body }),
  onboardWellnessOfficer: (body: OnboardOfficerRequest) =>
    request<WellnessOfficer>("/wellness/officers", { method: "POST", body }),
  getWellnessOfficers: (token: string, status?: string) =>
    request<WellnessOfficer[]>(withQuery("/wellness/officers", { status }), { token }),
  getWellnessOfficerDocuments: (officerId: string, token?: string) =>
    request<WellnessOfficerDocument[]>(`/wellness/officers/${officerId}/documents`, { token }),
  prepareWellnessOfficerDocumentUpload: (officerId: string, body: { documentType: string; fileName: string; contentType: string; sizeBytes: number; expiresOn?: string | null }, token?: string) =>
    request<WellnessOfficerDocumentUpload>(`/wellness/officers/${officerId}/documents/uploads`, { method: "POST", body, token }),
  uploadWellnessOfficerDocumentContent: (officerId: string, documentId: string, file: File, token?: string, options?: UploadOptions) =>
    requestUpload<WellnessOfficerDocumentUpload>(`/wellness/officers/${officerId}/documents/${documentId}/content`, token, file, options),
  reviewWellnessOfficerDocument: (documentId: string, body: { decision: string; reason?: string | null }, token: string) =>
    request<WellnessOfficerDocument>(`/wellness/officers/documents/${documentId}/review`, { method: "POST", body, token }),
  getAvailableWellnessOfficers: (token: string, parish: string, scheduledAt: string) =>
    request<WellnessOfficer[]>(withQuery("/wellness/officers/available", { parish, scheduledAt }), { token }),
  approveWellnessOfficer: (officerId: string, token: string, reason?: string) =>
    request<WellnessOfficer>(`/wellness/officers/${officerId}/approve`, {
      method: "POST",
      token,
      body: { reason },
    }),
  rejectWellnessOfficer: (officerId: string, token: string, reason?: string) =>
    request<WellnessOfficer>(`/wellness/officers/${officerId}/reject`, {
      method: "POST",
      token,
      body: { reason },
    }),
  suspendWellnessOfficer: (officerId: string, token: string, reason?: string) =>
    request<WellnessOfficer>(`/wellness/officers/${officerId}/suspend`, {
      method: "POST",
      token,
      body: { reason },
    }),
  quoteWellnessVisit: (body: WellnessQuoteRequest) =>
    request<WellnessQuote>("/wellness/quote", { method: "POST", body }),
  getWellnessSubscription: (token: string) => request<WellnessSubscription | null>("/wellness/subscriptions", { token }),
  startWellnessSubscription: (token: string) => request<WellnessSubscription>("/wellness/subscriptions", { method: "POST", token }),
  renewWellnessSubscription: (token: string) => request<WellnessSubscription>("/wellness/subscriptions/renew", { method: "POST", token }),
  cancelWellnessSubscription: (token: string) => request<WellnessSubscription>("/wellness/subscriptions/cancel", { method: "POST", token }),
  createWellnessVisit: (body: CreateWellnessVisitRequest, token?: string) =>
    request<WellnessVisit>("/wellness/visits", { method: "POST", body, token }),
  getWellnessVisits: (params: { hostUserId?: string; propertyId?: string; officerId?: string } = {}, token?: string) =>
    request<WellnessVisit[]>("/wellness/visits" + withQuery("", params), { token }),
  getWellnessReport: (visitId: string, token: string) => request<WellnessReport>(`/wellness/visits/${visitId}/report`, { token }),
  getWellnessReportCollaboration: (reportId: string, token?: string) => request<WellnessReportCollaboration>(`/wellness/reports/${reportId}/collaboration`, { token }),
  downloadWellnessReportPdf: (reportId: string, token?: string) => requestFile(`/wellness/reports/${reportId}/pdf`, token),
  addWellnessReportComment: (reportId: string, body: { body: string }, token?: string) => request<WellnessReportComment>(`/wellness/reports/${reportId}/comments`, { method: "POST", body, token }),
  acknowledgeWellnessReport: (reportId: string, token?: string) => request<{ reportId: string; acknowledgedByUserId: string; acknowledgedAt: string }>(`/wellness/reports/${reportId}/acknowledge`, { method: "POST", token }),
  createWellnessFollowUpTask: (reportId: string, body: { title: string; description: string; priority?: string; assigneeUserId?: string | null; dueAt?: string | null }, token?: string) => request<WellnessFollowUpTask>(`/wellness/reports/${reportId}/follow-up`, { method: "POST", body, token }),
  getWellnessFollowUpTasks: (token?: string) => request<WellnessFollowUpTask[]>("/wellness/follow-up-tasks", { token }),
  getWellnessReportTemplates: (token?: string, activeOnly = true) => request<WellnessReportTemplate[]>(withQuery("/wellness/report-templates", { activeOnly: String(activeOnly) }), { token }),
  saveWellnessReportTemplate: (body: { name: string; definitionJson: string; isActive?: boolean }, token: string) => request<WellnessReportTemplate>("/wellness/report-templates", { method: "POST", body, token }),
  assignWellnessOfficer: (visitId: string, officerId: string, token: string) =>
    request<WellnessVisit>(`/wellness/visits/${visitId}/assign`, {
      method: "POST",
      token,
      body: { officerId },
    }),
  cancelWellnessVisit: (visitId: string, token: string, reason?: string) =>
    request<WellnessVisit>(`/wellness/visits/${visitId}/cancel`, {
      method: "POST",
      token,
      body: { reason },
    }),
  rescheduleWellnessVisit: (visitId: string, token: string, scheduledAt: string, timeZone = "America/Jamaica", reason?: string) =>
    request<WellnessVisit>(`/wellness/visits/${visitId}/reschedule`, {
      method: "POST",
      token,
      body: { scheduledAt, timeZone, reason },
    }),
  prepareWellnessReportPhotoUpload: (visitId: string, tokenOrBody: string | { officerBadgeNumber: string; fileName: string; contentType: string; sizeBytes: number }, bodyMaybe?: { officerBadgeNumber: string; fileName: string; contentType: string; sizeBytes: number }) => {
    const token = typeof tokenOrBody === "string" ? tokenOrBody : undefined;
    const body = typeof tokenOrBody === "string" ? bodyMaybe! : tokenOrBody;
    return request<WellnessReportPhotoUpload>(`/wellness/visits/${visitId}/report/photos/uploads`, { method: "POST", body, token });
  },
  uploadWellnessReportPhotoContent: (visitId: string, photoId: string, officerBadgeNumber: string, token: string | File, fileOrOptions?: File | UploadOptions, options?: UploadOptions) => {
    const authToken = typeof token === "string" ? token : undefined;
    const file = (typeof token === "string" ? fileOrOptions : token) as File;
    const requestOptions = (typeof token === "string" ? options : fileOrOptions) as UploadOptions | undefined;
    return requestUpload<WellnessReportPhotoUpload>(
      withQuery(`/wellness/visits/${visitId}/report/photos/${photoId}/content`, { officerBadgeNumber }),
      authToken,
      file,
      requestOptions,
    );
  },
  prepareAdminWellnessReportPhotoUpload: (visitId: string, token: string, body: { officerBadgeNumber: string; fileName: string; contentType: string; sizeBytes: number }) =>
    request<WellnessReportPhotoUpload>(`/wellness/visits/${visitId}/complete/photos/uploads`, { method: "POST", token, body }),
  uploadAdminWellnessReportPhotoContent: (visitId: string, photoId: string, token: string, file: File, options?: UploadOptions) =>
    requestUpload<WellnessReportPhotoUpload>(`/wellness/visits/${visitId}/complete/photos/${photoId}/content`, token, file, options),
  submitWellnessReport: (visitId: string, tokenOrBody: string | { officerBadgeNumber: string; notes: string; photos?: string[] }, bodyMaybe?: { officerBadgeNumber: string; notes: string; photos?: string[] }) => {
    const token = typeof tokenOrBody === "string" ? tokenOrBody : undefined;
    const body = typeof tokenOrBody === "string" ? bodyMaybe! : tokenOrBody;
    return request<WellnessVisit>(`/wellness/visits/${visitId}/report`, { method: "POST", body, token });
  },
  completeWellnessVisit: (
    visitId: string,
    token: string,
    body: { officerBadgeNumber: string; notes: string; photos?: string[] },
  ) => request<WellnessVisit>(`/wellness/visits/${visitId}/complete`, { method: "POST", token, body }),
  markWellnessPayoutPaid: (visitId: string, token: string, providerReference?: string, notes?: string) =>
    request<WellnessPayout>(`/wellness/visits/${visitId}/payout`, {
      method: "POST",
      token,
      body: { providerReference, notes },
    }),
  getWellnessPayouts: (token: string, status?: string) =>
    request<WellnessPayout[]>(withQuery("/wellness/payouts", { status }), { token }),
  getWellnessPayoutStatement: (token?: string, format = "json", from?: string, to?: string) => request<WellnessPayoutStatement>(withQuery("/wellness/payouts/statement", { format, from, to }), { token }),
  createWellnessPayoutDispute: (payoutId: string, body: { reason: string; evidenceJson?: string | null }, token?: string) => request<WellnessPayoutDispute>(`/wellness/payouts/${payoutId}/disputes`, { method: "POST", body, token }),
  resolveWellnessPayoutDispute: (disputeId: string, body: { decision: string; notes?: string | null }, token: string) => request<WellnessPayoutDispute>(`/wellness/payout-disputes/${disputeId}/resolve`, { method: "POST", body, token }),
  getWellnessAdminDashboard: (token: string) =>
    request<WellnessAdminDashboard>("/wellness/admin/dashboard", { token }),
  seedSpecCompletion: () => request<unknown>("/spec/seed", { method: "POST" }),
  getPublicPages: () => request<PublicContentPage[]>("/spec/public/pages"),
  getPublicPage: (slug: string) => request<PublicContentPage>(`/spec/public/pages/${slug}`),
  createContactRequest: (body: { name: string; email: string; subject: string; message: string }) =>
    request<unknown>("/spec/public/contact", { method: "POST", body }),
  getExperiences: (params: { category?: string; parish?: string; query?: string } = {}) =>
    request<Experience[]>(withQuery("/spec/experiences", params)),
  getExperience: (slug: string) => request<Experience>(`/spec/experiences/${slug}`),
  getJournal: (params: { category?: string; query?: string } = {}) =>
    request<JournalArticle[]>(withQuery("/spec/journal", params)),
  getJournalArticle: (slug: string) => request<JournalArticle>(`/spec/journal/${slug}`),
  getHostProfiles: () => request<HostProfile[]>("/spec/host-profiles"),
  getHostProfile: (slug: string) => request<HostProfile>(`/spec/host-profiles/${slug}`),
  updateHostProfile: (slug: string, token: string, body: Partial<HostProfile> & { hostUserId: string }) =>
    request<HostProfile>(`/spec/host-profiles/${slug}`, { method: "PUT", token, body }),
  getTravelerWorkspace: (userId: string, token: string) =>
    request<TravelerWorkspace>(`/spec/traveler/${userId}`, { token }),
  prepareIdentityDocumentUpload: (userId: string, token: string, body: { documentType: string; fileName: string; contentType: string; sizeBytes: number; issuingCountry?: string | null; expiresOn?: string | null }) =>
    request<IdentityDocumentUpload>(`/spec/traveler/${userId}/identity-documents/uploads`, { method: "POST", token, body }),
  uploadIdentityDocumentContent: (userId: string, uploadId: string, token: string, file: File, options?: UploadOptions) =>
    requestUpload<IdentityDocumentUpload>(`/spec/traveler/${userId}/identity-documents/uploads/${uploadId}/content`, token, file, options),
  createWishlistCollection: (userId: string, token: string, body: { name: string; sortOrder?: number }) =>
    request<WishlistCollection>(`/spec/traveler/${userId}/wishlist/collections`, { method: "POST", token, body }),
  renameWishlistCollection: (userId: string, collectionId: string, token: string, body: { name: string; sortOrder?: number }) =>
    request<WishlistCollection>(`/spec/traveler/${userId}/wishlist/collections/${collectionId}`, { method: "PUT", token, body }),
  deleteWishlistCollection: (userId: string, collectionId: string, token: string) =>
    request<void>(`/spec/traveler/${userId}/wishlist/collections/${collectionId}`, { method: "DELETE", token }),
  addWishlistItem: (userId: string, collectionId: string, token: string, body: { propertyId: string; propertyTitle: string; status?: string; sortOrder?: number }) =>
    request<WishlistItem>(`/spec/traveler/${userId}/wishlist/collections/${collectionId}/items`, { method: "POST", token, body }),
  removeWishlistItem: (userId: string, itemId: string, token: string) =>
    request<void>(`/spec/traveler/${userId}/wishlist/items/${itemId}`, { method: "DELETE", token }),
  createPaymentMethodSetupIntent: (userId: string, token: string) =>
    request<PaymentMethodSetupIntent>(`/spec/traveler/${userId}/payment-methods/setup-intents`, { method: "POST", token }),
  addPaymentMethod: (userId: string, token: string, body: { setupIntentReference: string; isDefault?: boolean }) =>
    request<TravelerPaymentMethod>(`/spec/traveler/${userId}/payment-methods`, { method: "POST", token, body }),
  setDefaultPaymentMethod: (userId: string, methodId: string, token: string) =>
    request<void>(`/spec/traveler/${userId}/payment-methods/${methodId}/default`, { method: "POST", token }),
  removePaymentMethod: (userId: string, methodId: string, token: string) =>
    request<void>(`/spec/traveler/${userId}/payment-methods/${methodId}`, { method: "DELETE", token }),
  submitReview: (userId: string, token: string, body: { propertyId?: string; bookingId?: string; subjectTitle: string; rating: number; text: string }) =>
    request<TravelerReview>(`/spec/traveler/${userId}/reviews`, { method: "POST", token, body }),
  replyToReview: (hostUserId: string, reviewId: string, token: string, body: { reply: string }) =>
    request<TravelerReview>(`/spec/host/${hostUserId}/reviews/${reviewId}/reply`, { method: "POST", token, body }),
  markNotificationRead: (userId: string, notificationId: string, token: string) =>
    request<void>(`/spec/traveler/${userId}/notifications/${notificationId}/read`, { method: "POST", token }),
  markAllNotificationsRead: (userId: string, token: string) =>
    request<void>(`/spec/traveler/${userId}/notifications/read-all`, { method: "POST", token }),
  getDirectoryProviders: (params: { kind?: string; category?: string; parish?: string; query?: string } = {}) =>
    request<DirectoryProvider[]>(withQuery("/spec/directories/providers", params)),
  getDirectoryProvider: (slug: string) => request<DirectoryProvider>(`/spec/directories/providers/${slug}`),
  upsertDirectoryProvider: (token: string, body: Partial<DirectoryProvider>) =>
    request<DirectoryProvider>("/spec/directories/providers", { method: "POST", token, body }),
  getM4DirectoryProviders: (params: { kind?: string; category?: string; parish?: string; query?: string } = {}, token?: string) =>
    request<DirectoryProvider[]>(withQuery("/directories/providers", params), { token }),
  getM4DirectoryModerationQueue: (token: string, params: { kind?: string; status?: string; query?: string } = {}) =>
    request<DirectoryProvider[]>(withQuery("/directories/providers/moderation", params), { token }),
  getM4DirectoryProvider: (slug: string, token?: string) => request<DirectoryProvider>(`/directories/providers/${slug}`, { token }),
  recordDirectoryRecentView: (providerId: string, token: string) => request<void>(`/directories/recent-views/${providerId}`, { method: "POST", token }),
  getDirectoryRecentViews: (token: string) => request<DirectoryProvider[]>("/directories/recent-views", { token }),
  removeDirectoryRecentView: (providerId: string, token: string) => request<void>(`/directories/recent-views/${providerId}`, { method: "DELETE", token }),
  clearDirectoryRecentViews: (token: string) => request<void>("/directories/recent-views", { method: "DELETE", token }),
  getM4DirectoryMine: (token: string) => request<DirectoryProvider[]>("/directories/providers/mine", { token }),
  saveM4DirectoryProvider: (token: string, body: { slug?: string; kind: string; category: string; name: string; parish: string; badgeLevel?: string; description: string; availabilitySummary: string; contactMode?: string; isBrickAndMortar?: boolean; policeBadgeNumber?: string | null; isActive?: boolean; services?: string[]; openingHours?: string; emergencyAvailable?: boolean; serviceRadiusKm?: number }) =>
    request<DirectoryProvider>("/directories/providers", { method: "POST", token, body }),
  getM4DirectoryProviderDocuments: (providerId: string, token: string) =>
    request<DirectoryProviderDocument[]>(`/spec/directories/providers/${providerId}/documents`, { token }),
  prepareM4DirectoryProviderDocumentUpload: (providerId: string, token: string, body: { documentType: string; fileName: string; contentType: string; sizeBytes: number }) =>
    request<DirectoryProviderDocumentUpload>(`/spec/directories/providers/${providerId}/documents/uploads`, { method: "POST", token, body }),
  uploadM4DirectoryProviderDocumentContent: (providerId: string, documentId: string, token: string, file: File, options?: UploadOptions) =>
    requestUpload<DirectoryProviderDocumentUpload>(`/spec/directories/providers/${providerId}/documents/${documentId}/content`, token, file, options),
  getM4DirectoryProviderDocumentDownload: (providerId: string, documentId: string, token: string) =>
    request<{ id: string; fileName: string; contentType: string; sizeBytes: number; url: string; expiresAt: string }>(`/spec/directories/providers/${providerId}/documents/${documentId}/download`, { token }),
  moderateM4DirectoryProvider: (slug: string, token: string, status: string, reason?: string) =>
    request<DirectoryProvider>(`/directories/providers/${slug}/moderate`, { method: "POST", token, body: { status, reason } }),
  createDirectoryQuote: (slug: string, token: string, body: { scope: string; preferredAt?: string; budget?: number; expiresAt?: string }) =>
    request<DirectoryQuote>(`/directories/providers/${slug}/quotes`, { method: "POST", token, body }),
  getDirectoryQuotes: (token: string) => request<DirectoryQuote[]>("/directories/quotes", { token }),
  respondDirectoryQuote: (quoteId: string, token: string, body: { status: string; amount?: number; message?: string }) =>
    request<DirectoryQuote>(`/directories/quotes/${quoteId}/respond`, { method: "POST", token, body }),
  getDirectoryReviews: (slug: string) => request<DirectoryReview[]>(`/directories/providers/${slug}/reviews`),
  createDirectoryReview: (slug: string, token: string, body: { rating: number; body: string }) =>
    request<DirectoryReview>(`/directories/providers/${slug}/reviews`, { method: "POST", token, body }),
  respondDirectoryReview: (reviewId: string, token: string, response: string) =>
    request<DirectoryReview>(`/directories/reviews/${reviewId}/respond`, { method: "POST", token, body: { response } }),
  saveDirectoryBusinessDetails: (slug: string, token: string, body: { weeklyHoursJson: string; holidayClosuresJson?: string; promotionsJson?: string; accessibilityInfo?: string }) =>
    request<DirectoryProvider>(`/directories/providers/${slug}/business-details`, { method: "PUT", token, body }),
  getDirectoryProviderInsights: (slug: string, token: string) => request<DirectoryProviderInsights>(`/directories/providers/${slug}/insights`, { token }),
  issueBookingQr: (bookingId: string, token: string) =>
    request<QrIssueResult>(`/access/qr/bookings/${bookingId}`, { method: "POST", token }),
  getBookingQr: (qrId: string, token: string) => request<QrAccess>(`/access/qr/${qrId}`, { token }),
  listBookingQrs: (token: string) => request<QrAccess[]>("/access/qr", { token }),
  getBookingQrHistory: (qrId: string, token: string) => request<QrHistoryEvent[]>(`/access/qr/${qrId}/history`, { token }),
  revokeBookingQr: (qrId: string, token: string, reason?: string) => request<QrAccess>(`/access/qr/${qrId}/revoke`, { method: "POST", token, body: { reason } }),
  validateQr: (token: string, propertyId: string, deviceMetadata?: string) =>
    request<QrValidationResult>("/access/qr/validate", { method: "POST", body: { token, propertyId, deviceMetadata } }),
  getInbox: (userId: string, token: string) =>
    request<MessagingInbox>(withQuery("/spec/messages/inbox", { userId }), { token }),
  getConversation: (conversationId: string, userId: string, token: string) =>
    request<Conversation>(withQuery(`/spec/messages/conversations/${conversationId}`, { userId }), { token }),
  createConversation: (userId: string, token: string, body: { subject: string; bookingId?: string | null; isSupportThread: boolean; participants: { userId: string; displayName: string; role: string }[]; initialMessage: string }) =>
    request<Conversation>(withQuery("/spec/messages/conversations", { userId }), { method: "POST", token, body }),
  prepareMessageAttachmentUpload: (conversationId: string, userId: string, token: string, body: { fileName: string; contentType: string; sizeBytes: number }) =>
    request<AttachmentUpload>(withQuery(`/spec/messages/conversations/${conversationId}/attachments/uploads`, { userId }), { method: "POST", token, body }),
  uploadMessageAttachmentContent: (conversationId: string, attachmentId: string, userId: string, token: string, file: File, options?: UploadOptions) =>
    requestUpload<AttachmentUpload>(withQuery(`/spec/messages/conversations/${conversationId}/attachments/${attachmentId}/content`, { userId }), token, file, options),
  completeMessageAttachmentUpload: (conversationId: string, attachmentId: string, userId: string, token: string, body: { contentType: string; sizeBytes: number; headerBytesBase64: string; sha256Hash: string }) =>
    request<AttachmentUpload>(withQuery(`/spec/messages/conversations/${conversationId}/attachments/${attachmentId}/complete`, { userId }), { method: "POST", token, body }),
  getMessageAttachmentDownload: (conversationId: string, attachmentId: string, userId: string, token: string) =>
    request<AttachmentDownload>(withQuery(`/spec/messages/conversations/${conversationId}/attachments/${attachmentId}/download`, { userId }), { token }),
  sendMessage: (conversationId: string, userId: string, token: string, body: { body: string; attachments?: MessageAttachment[] }) =>
    request<Message>(withQuery(`/spec/messages/conversations/${conversationId}/messages`, { userId }), { method: "POST", token, body }),
  markConversationRead: (conversationId: string, userId: string, token: string) =>
    request<void>(withQuery(`/spec/messages/conversations/${conversationId}/read`, { userId }), { method: "POST", token }),
  getHostOperations: (hostUserId: string, token: string) =>
    request<HostOperations>(`/spec/host/${hostUserId}/operations`, { token }),
  settleHostPayout: (payoutId: string, token: string, notes?: string) =>
    request<HostPayout>(`/spec/admin/host-payouts/${payoutId}/settle`, { method: "POST", token, body: { notes } }),
  getTravelerRecommendations: (userId: string, token: string, query?: { parish?: string; maximumNightlyRate?: number; badgeLevel?: string; limit?: number }) =>
    request<TravelerRecommendation[]>(withQuery(`/spec/traveler/${userId}/recommendations`, { parish: query?.parish, maximumNightlyRate: query?.maximumNightlyRate?.toString(), badgeLevel: query?.badgeLevel, limit: query?.limit?.toString() }), { token }),
  dismissTravelerRecommendation: (userId: string, propertyId: string, token: string) => request<TravelerRecommendation>(`/spec/traveler/${userId}/recommendations/${propertyId}/dismiss`, { method: "POST", token }),
  restoreTravelerRecommendation: (userId: string, propertyId: string, token: string) => request<TravelerRecommendation>(`/spec/traveler/${userId}/recommendations/${propertyId}/restore`, { method: "POST", token }),
  saveTravelerPreferences: (userId: string, token: string, body: { preferredParish?: string | null; maximumNightlyRate?: number | null; preferredBadgeLevel?: string | null; preferredHighlights?: string[] }) => request<TravelerPreference>(`/spec/traveler/${userId}/preferences`, { method: "PUT", token, body }),
  saveHostPricingRule: (hostUserId: string, token: string, body: Omit<HostPricingRule, "id" | "hostUserId">) =>
    request<HostPricingRule>(`/spec/host/${hostUserId}/pricing-rules`, { method: "POST", token, body }),
  saveHostPromotion: (hostUserId: string, token: string, body: Omit<HostPromotion, "id" | "hostUserId">) =>
    request<HostPromotion>(`/spec/host/${hostUserId}/promotions`, { method: "POST", token, body }),
  getAdminOperations: (token: string) => request<AdminOperations>("/spec/admin/operations", { token }),
  createAdminCase: (token: string, body: { caseType: string; subjectType: string; subjectId?: string | null; priority: string; reason: string; assignedTo?: string }) =>
    request<AdminCase>("/spec/admin/cases", { method: "POST", token, body }),
  resolveAdminCase: (token: string, caseId: string, body: { resolutionNotes: string; status?: string }) =>
    request<AdminCase>(`/spec/admin/cases/${caseId}/resolve`, { method: "POST", token, body }),
  prepareAdminCaseEvidenceUpload: (token: string, caseId: string, body: { fileName: string; contentType: string; sizeBytes: number }) =>
    request<AdminCaseEvidenceUpload>(`/spec/admin/cases/${caseId}/evidence/uploads`, { method: "POST", token, body }),
  uploadAdminCaseEvidenceContent: (token: string, caseId: string, evidenceId: string, file: File, options?: UploadOptions) =>
    requestUpload<AdminCaseEvidenceUpload>(`/spec/admin/cases/${caseId}/evidence/${evidenceId}/content`, token, file, options),
  getAdminCaseEvidenceDownload: (token: string, caseId: string, evidenceId: string) =>
    request<AdminCaseEvidenceDownload>(`/spec/admin/cases/${caseId}/evidence/${evidenceId}/download`, { token }),
  getAuditLog: (token: string) => request<AuditEvent[]>("/spec/admin/audit-log", { token }),
  startAuthFlow: (body: { userId?: string | null; flowType: string; destination: string }) =>
    request<AuthFlowResult>("/spec/auth/flows", { method: "POST", body }),
  completeAuthFlow: (body: { flowId: string; code?: string; token?: string }) =>
    request<AuthFlowResult>("/spec/auth/flows/complete", { method: "POST", body }),
  acceptOwnerInvitation: (body: { flowId: string; token?: string; code?: string }) =>
    request<AuthFlowResult>("/spec/auth/owner-invitation/accept", { method: "POST", body }),
  getDevelopmentAuthFlowSecret: (flowId: string) =>
    request<{ id: string; code: string; token: string; expiresAt: string }>(`/spec/auth/development/flows/${flowId}`),
  generateRecoveryCodes: (userId: string, token: string) =>
    request<{ code: string; used: boolean }[]>(`/spec/auth/${userId}/recovery-codes`, { method: "POST", token }),
  getSocialAuthConfig: () => request<SocialAuthConfig>("/spec/auth/social-config"),
  getPropertyManagerDashboard: (token: string) => request<PropertyManagerDashboard>("/property-manager/dashboard", { token }),
  invitePropertyManagerOwner: (token: string, body: { email: string; displayName: string; ownerUserId?: string; communityId?: string }) => request<PropertyManagerOwner>("/property-manager/owners", { method: "POST", token, body }),
  reviewPropertyManagerOwner: (token: string, ownerUserId: string, status: string) => request<PropertyManagerOwner>(`/property-manager/owners/${ownerUserId}/verification`, { method: "POST", token, body: { status } }),
  renewPropertyManagerSubscription: (token: string) => request<PropertyManagerSubscription>("/property-manager/subscription/renew", { method: "POST", token }),
  addPropertyManagerProperty: (token: string, body: { ownerUserId: string; title: string; unitNumber: string; address: string; communityId?: string }) => request<PropertyManagerProperty>("/property-manager/properties", { method: "POST", token, body }),
  createPropertyManagerInvoice: (token: string, body: { ownerUserId: string; propertyId?: string; dueDate: string; tax: number; lines: { description: string; quantity: number; unitAmount: number }[] }) => request<PropertyManagerInvoice>("/property-manager/invoices", { method: "POST", token, body }),
  bulkIssuePropertyManagerInvoices: (token: string, invoiceIds: string[]) => request<PropertyManagerInvoice[]>("/property-manager/invoices/bulk-issue", { method: "POST", token, body: { invoiceIds } }),
  markPropertyManagerInvoicesOverdue: (token: string) => request<PropertyManagerInvoice[]>("/property-manager/invoices/mark-overdue", { method: "POST", token }),
  updatePropertyManagerInvoice: (token: string, invoiceId: string, body: { dueDate: string; tax: number; lines: { description: string; quantity: number; unitAmount: number }[] }) => request<PropertyManagerInvoice>(`/property-manager/invoices/${invoiceId}`, { method: "PUT", token, body }),
  getPropertyManagerInvoice: (token: string, invoiceId: string) => request<PropertyManagerInvoice>(`/property-manager/invoices/${invoiceId}`, { token }),
  payPropertyManagerInvoice: (token: string, invoiceId: string, body: { amount: number; idempotencyKey: string }) => request<PropertyManagerInvoice>(`/property-manager/invoices/${invoiceId}/payments`, { method: "POST", token, body }),
  getPropertyManagerStatement: (token: string, ownerUserId: string, from?: string, to?: string) => request<PropertyManagerStatement>(withQuery(`/property-manager/owners/${ownerUserId}/statement`, { from, to }), { token }),
  createPropertyManagerUtility: (token: string, body: { ownerUserId: string; propertyId: string; utilityType: string; billingPeriod: string; usage: number; rate: number }) => request<PropertyManagerUtility>("/property-manager/utilities", { method: "POST", token, body }),
  createPropertyManagerMaintenance: (token: string, body: { ownerUserId: string; propertyId: string; title: string; description: string; category: string; urgency: string }) => request<PropertyManagerMaintenance>("/property-manager/maintenance", { method: "POST", token, body }),
  updatePropertyManagerMaintenance: (token: string, id: string, body: { status: string; vendorId?: string; scheduledAt?: string; cost: number; notes: string }) => request<PropertyManagerMaintenance>(`/property-manager/maintenance/${id}`, { method: "PATCH", token, body }),
  createPropertyManagerVendor: (token: string, body: { name: string; category: string; contact: string; notes: string }) => request<PropertyManagerVendor>("/property-manager/vendors", { method: "POST", token, body }),
  linkPropertyManagerRentalListing: (token: string, propertyId: string, rentalListingId?: string) => request<PropertyManagerProperty>(`/property-manager/properties/${propertyId}/rental-listing`, { method: "PATCH", token, body: { rentalListingId: rentalListingId ?? null } }),
  updatePropertyManagerVendor: (token: string, id: string, body: { contact?: string; notes?: string; serviceAreas?: string[]; availabilityJson?: string; rate?: number; rating?: number; isPreferred?: boolean; isSuspended?: boolean; isActive?: boolean }) => request<PropertyManagerVendor>(`/property-manager/vendors/${id}`, { method: "PATCH", token, body }),
  createPropertyManagerNotice: (token: string, body: { communityId?: string; targetOwnerUserId?: string; title: string; body: string; expiresAt?: string; isPinned: boolean; publishAt?: string; category?: string; audienceRoles?: string[]; audienceOwnerIds?: string[]; acknowledgementDueAt?: string }) => request<PropertyManagerNotice>("/property-manager/notices", { method: "POST", token, body }),
  getPropertyManagerNotices: (token: string) => request<PropertyManagerNotice[]>("/property-manager/notices", { token }),
  createPropertyManagerProposal: (token: string, body: { communityId?: string; title: string; description: string; opensAt: string; closesAt: string; isAnonymous: boolean; quorum?: number }) => request<PropertyManagerProposal>("/property-manager/governance/proposals", { method: "POST", token, body }),
  votePropertyManagerProposal: (token: string, proposalId: string, body: { choice: string; proxyId?: string }) => request<PropertyManagerProposal>(`/property-manager/governance/proposals/${proposalId}/votes`, { method: "POST", token, body }),
  createPropertyManagerProxy: (token: string, body: { proposalId: string; proxyUserId: string; validUntil: string }) => request<{ id: string; proposalId: string; ownerUserId: string; proxyUserId: string; status: string; validUntil: string }>("/property-manager/governance/proxies", { method: "POST", token, body }),
  getPropertyManagerDocuments: (token: string) => request<PropertyManagerDocument[]>("/property-manager/documents", { token }),
  addPropertyManagerDocument: (token: string, body: { ownerUserId?: string; propertyId?: string; title: string; category: string; fileName: string; contentType: string; sizeBytes: number; contentBase64?: string; expiresOn?: string }) => request<PropertyManagerDocument>("/property-manager/documents", { method: "POST", token, body }),
  getPropertyManagerDocumentDownload: (token: string, documentId: string) => request<PropertyManagerDocumentDownload>(`/property-manager/documents/${documentId}/download`, { token }),
  createPropertyManagerDocumentExport: (token: string, documentIds: string[]) => request<PropertyManagerDocumentExport>("/property-manager/documents/exports", { method: "POST", token, body: { documentIds } }),
  getPropertyManagerDocumentExport: (token: string, exportId: string) => request<PropertyManagerDocumentExport>(`/property-manager/documents/exports/${exportId}`, { token }),
  downloadPropertyManagerDocumentExport: (token: string, exportId: string) => requestFile(`/property-manager/documents/exports/${exportId}/download`, token),
  addPropertyManagerDocumentVersion: (token: string, body: { documentId: string; fileName: string; contentType: string; sizeBytes: number; contentBase64: string }) => request<{ id: string; documentId: string; version: number; fileName: string; contentType: string; sizeBytes: number; createdByUserId: string; createdAt: string }>("/property-manager/documents/versions", { method: "POST", token, body }),
  createPropertyManagerGateMessage: (token: string, body: { communityId?: string; propertyId?: string; recipient: string; message: string; visitorType: string; validFrom: string; validUntil: string }) => request<PropertyManagerGateMessage>("/property-manager/gate/messages", { method: "POST", token, body }),
  getPropertyManagerGateDelivery: (token: string, gateMessageId: string) => request<{ id: string; gateMessageId: string; recipient: string; status: string; providerReference?: string | null; attemptNumber: number; failureReason?: string | null; createdAt: string }[]>(`/property-manager/gate/messages/${gateMessageId}/delivery`, { token }),
  retryPropertyManagerGateDelivery: (token: string, gateMessageId: string) => request<{ id: string; gateMessageId: string; recipient: string; status: string; providerReference?: string | null; attemptNumber: number; failureReason?: string | null; createdAt: string }>(`/property-manager/gate/messages/${gateMessageId}/delivery/retry`, { method: "POST", token }),
  issuePropertyManagerQr: (token: string, body: { ownerUserId?: string; propertyId?: string; subjectType: string; validFrom: string; validUntil: string }) => request<PropertyManagerQr>("/property-manager/qr", { method: "POST", token, body }),
  listPropertyManagerQrs: (token: string) => request<PropertyManagerQrAccessRecord[]>("/property-manager/qr", { token }),
  listPropertyManagerQrHistory: (token: string, qrId: string) => request<PropertyManagerQrScan[]>(`/property-manager/qr/${qrId}/history`, { token }),
  validatePropertyManagerQr: (body: { token: string; propertyId?: string }) => request<PropertyManagerQrValidation>("/property-manager/qr/validate", { method: "POST", body }),
  revokePropertyManagerQr: (token: string, qrId: string, reason = "No longer needed") => request<PropertyManagerQrValidation>(`/property-manager/qr/${qrId}/revoke`, { method: "POST", token, body: { reason } }),
  listPropertyManagerProxies: (token: string) => request<{ id: string; proposalId: string; ownerUserId: string; proxyUserId: string; status: string; validUntil: string; acceptedAt?: string | null }[]>("/property-manager/governance/proxies", { token }),
  revokePropertyManagerProxy: (token: string, proxyId: string) => request<{ id: string; status: string }>(`/property-manager/governance/proxies/${proxyId}/revoke`, { method: "POST", token }),
  getOwnerPortal: (token: string) => request<PropertyManagerOwnerPortal>("/property-manager/owner/portal", { token }),
  bulkAssignPropertyManagerProperties: (token: string, body: { propertyIds: string[]; ownerUserId: string; reason: string; batchId?: string }) => request<PropertyManagerProperty[]>("/property-manager/properties/bulk-assign", { method: "POST", token, body }),
  getPropertyManagerPayments: (token: string, query?: { ownerUserId?: string; status?: string }) => request<PropertyManagerPaymentOperation[]>(withQuery("/property-manager/payments", query ?? {}), { token }),
  refundPropertyManagerPayment: (token: string, paymentId: string, body: { amount?: number; reason: string; idempotencyKey: string }) => request<PropertyManagerPaymentOperation>(`/property-manager/payments/${paymentId}/refund`, { method: "POST", token, body }),
  retryPropertyManagerPayment: (token: string, paymentId: string) => request<PropertyManagerPaymentOperation>(`/property-manager/payments/${paymentId}/retry`, { method: "POST", token }),
  recordPropertyManagerMeterReading: (token: string, body: { ownerUserId: string; propertyId: string; utilityType: string; billingPeriod: string; previousReading: number; currentReading: number; rate?: number }) => request<PropertyManagerMeterReading>("/property-manager/utilities/readings", { method: "POST", token, body }),
  getPropertyManagerMeterReadings: (token: string, propertyId: string, utilityType?: string) => request<PropertyManagerMeterReading[]>(withQuery(`/property-manager/utilities/${propertyId}/readings`, { utilityType }), { token }),
  getPropertyManagerReport: (token: string, from?: string, to?: string) => request<PropertyManagerReport>(withQuery("/property-manager/reports", { from, to }), { token }),
  createPropertyManagerWorkOrder: (token: string, body: { propertyId: string; ownerUserId: string; scope: string; vendorId?: string; quoteAmount?: number; slaDueAt?: string }) => request<PropertyManagerWorkOrder>("/property-manager/work-orders", { method: "POST", token, body }),
  updatePropertyManagerWorkOrder: (token: string, id: string, body: { status: string; vendorId?: string; approvedAmount?: number; laborAmount?: number; partsAmount?: number; scheduledAt?: string }) => request<PropertyManagerWorkOrder>(`/property-manager/work-orders/${id}`, { method: "PATCH", token, body }),
  getPropertyManagerCalendar: (token: string, query?: { from?: string; to?: string; propertyId?: string }) => request<PropertyManagerCalendarEvent[]>(withQuery("/property-manager/calendar/events", query ?? {}), { token }),
  createPropertyManagerCalendarEvent: (token: string, body: { propertyId?: string; ownerUserId?: string; eventType: string; title: string; startsAt: string; endsAt: string; status?: string }) => request<PropertyManagerCalendarEvent>("/property-manager/calendar/events", { method: "POST", token, body }),
  changePropertyManagerSubscription: (token: string, body: { action: string; targetTier?: string; reason?: string; autoRenew?: boolean }) => request<PropertyManagerDashboard["manager"]>("/property-manager/subscription/change", { method: "POST", token, body }),
  getPropertyManagerSubscriptionEvents: (token: string) => request<PropertyManagerSubscriptionEvent[]>("/property-manager/subscription/events", { token }),
  retryPropertyManagerSubscriptionPayment: (token: string) => request<PropertyManagerSubscriptionEvent>("/property-manager/subscription/payment-retry", { method: "POST", token }),
  getPropertyManagerDashboardPreference: (token: string) => request<{ managerUserId: string; kpiOrder: string[]; visibleKpis: string[]; savedFiltersJson: string; savedViews: string[] }>("/property-manager/dashboard/preferences", { token }),
  savePropertyManagerDashboardPreference: (token: string, body: { kpiOrder: string[]; visibleKpis: string[]; savedFiltersJson: string; savedViews: string[] }) => request<{ managerUserId: string; kpiOrder: string[]; visibleKpis: string[]; savedFiltersJson: string; savedViews: string[] }>("/property-manager/dashboard/preferences", { method: "PUT", token, body }),
  archivePropertyManagerDocument: (token: string, documentId: string, restore = false) => request<PropertyManagerDocument>(withQuery(`/property-manager/documents/${documentId}/archive`, { restore: String(restore) }), { method: "POST", token }),
  getPropertyManagerDocumentVersions: (token: string, documentId: string) => request<{ id: string; documentId: string; version: number; fileName: string; contentType: string; sizeBytes: number; createdByUserId: string; createdAt: string }[]>(`/property-manager/documents/${documentId}/versions`, { token }),
  closePropertyManagerProposal: (token: string, proposalId: string) => request<PropertyManagerProposal>(`/property-manager/governance/proposals/${proposalId}/close`, { method: "POST", token }),
  addPropertyManagerNoticeComment: (token: string, noticeId: string, body: string) => request<{ id: string; subjectId: string; actorUserId: string; type: string; body: string; createdAt: string }>(`/property-manager/notices/${noticeId}/comments`, { method: "POST", token, body: { noticeId, body } }),
  decidePropertyManagerVerification: (token: string, body: { ownerUserId: string; requirement: string; status: string; reason?: string }) => request<{ id: string; ownerUserId: string; requirement: string; status: string; reason?: string | null; documentKey?: string | null; createdAt: string }>("/property-manager/owners/verification-requirements", { method: "POST", token, body }),
  getP0OwnerProfile: (token: string, ownerUserId: string) => request<P0OwnerProfile>(`/property-manager/p0/owners/${ownerUserId}/profile`, { token }),
  saveP0OwnerProfile: (token: string, ownerUserId: string, body: { legalName: string; contactEmail: string; contactPhone: string; billingAddress: string; preferredCurrency: string; timeZone: string; billingMetadataJson: string; paymentProviderCustomerReference?: string; operationalMetadataJson: string; notes: string }) => request<P0OwnerProfile>(`/property-manager/p0/owners/${ownerUserId}/profile`, { method: "PUT", token, body }),
  changeP0OwnerStatus: (token: string, ownerUserId: string, body: { status: string; reason: string }) => request<P0OwnerProfile>(`/property-manager/p0/owners/${ownerUserId}/status`, { method: "POST", token, body }),
  getP0OwnerLifecycle: (token: string, ownerUserId: string) => request<P0OwnerLifecycleEvent[]>(`/property-manager/p0/owners/${ownerUserId}/lifecycle`, { token }),
  getP0Portfolio: (token: string, query?: { ownerUserId?: string; propertyId?: string; search?: string; status?: string; page?: number; pageSize?: number }) => request<P0Portfolio>(withQuery("/property-manager/p0/portfolio", Object.fromEntries(Object.entries(query ?? {}).map(([key, value]) => [key, value == null ? undefined : String(value)]))), { token }),
  getP0AssignmentHistory: (token: string, propertyId?: string) => request<PropertyAssignmentHistoryDto[]>(withQuery("/property-manager/p0/assignments/history", { propertyId }), { token }),
  assignP0Properties: (token: string, body: { propertyIds: string[]; ownerUserId: string; reason: string; expectedBatchId?: string }) => request<PropertyManagerProperty[]>("/property-manager/p0/assignments", { method: "POST", token, body }),
  getP0Agreements: (token: string, query?: { ownerUserId?: string; propertyId?: string; status?: string; page?: number; pageSize?: number }) => request<P0Agreement[]>(withQuery("/property-manager/p0/agreements", query ?? {}), { token }),
  createP0Agreement: (token: string, body: { ownerUserId: string; propertyId?: string; effectiveFrom: string; effectiveTo?: string; currency: string; termsJson: string; feeRuleJson: string; maintenanceApprovalLimit: number; expenseApprovalLimit: number; documentId?: string; documentKey?: string }) => request<P0Agreement>("/property-manager/p0/agreements", { method: "POST", token, body }),
  updateP0AgreementDraft: (token: string, id: string, body: { effectiveFrom: string; effectiveTo?: string; currency: string; termsJson: string; feeRuleJson: string; maintenanceApprovalLimit: number; expenseApprovalLimit: number; documentId?: string; documentKey?: string; rowVersion: number }) => request<P0Agreement>(`/property-manager/p0/agreements/${id}/draft`, { method: "PUT", token, body }),
  activateP0Agreement: (token: string, id: string) => request<P0Agreement>(`/property-manager/p0/agreements/${id}/activate`, { method: "POST", token }),
  renewP0Agreement: (token: string, id: string, body: { effectiveFrom: string; effectiveTo?: string; reason?: string }) => request<P0Agreement>(`/property-manager/p0/agreements/${id}/renew`, { method: "POST", token, body }),
  terminateP0Agreement: (token: string, id: string, reason: string) => request<P0Agreement>(`/property-manager/p0/agreements/${id}/terminate`, { method: "POST", token, body: { reason } }),
  getP0FeeRules: (token: string, query?: { ownerUserId?: string; propertyId?: string; currency?: string; category?: string; on?: string }) => request<P0FeeRule[]>(withQuery("/property-manager/p0/fees", query ?? {}), { token }),
  createP0FeeRule: (token: string, body: { ownerUserId: string; propertyId?: string; category: string; ruleType: string; calculationBasis: string; currency: string; percentage: number; fixedAmount: number; minimumAmount: number; cleaningMarkup: number; maintenanceMarkup: number; effectiveFrom: string; effectiveTo?: string }) => request<P0FeeRule>("/property-manager/p0/fees", { method: "POST", token, body }),
  calculateP0Fee: (token: string, body: { ownerUserId: string; propertyId?: string; category: string; currency: string; baseAmount: number; on: string; sourceId?: string }) => request<P0FeeCalculation>("/property-manager/p0/fees/calculate", { method: "POST", token, body }),
  postP0Fee: (token: string, body: { ownerUserId: string; propertyId?: string; category: string; currency: string; baseAmount: number; on: string; sourceId?: string; idempotencyKey: string }) => request<P0FeeCalculation>("/property-manager/p0/fees/post", { method: "POST", token, body }),
  getP0Accounts: (token: string, currency?: string) => request<P0Account[]>(withQuery("/property-manager/p0/accounting/accounts", { currency }), { token }),
  postP0Journal: (token: string, body: { sourceType: string; sourceId?: string; idempotencyKey?: string; currency: string; accountingDate: string; memo: string; lines: { accountCode: string; debit: number; credit: number; ownerUserId?: string; propertyId?: string; description?: string }[]; reconcile?: boolean; reconciliationReference?: string; approvalId?: string }) => request<P0Journal>("/property-manager/p0/accounting/journals", { method: "POST", token, body }),
  getP0Journals: (token: string, query?: { ownerUserId?: string; propertyId?: string; currency?: string; from?: string; to?: string; status?: string; page?: number; pageSize?: number }) => request<P0Journal[]>(withQuery("/property-manager/p0/accounting/journals", query ?? {}), { token }),
  reverseP0Journal: (token: string, id: string, body: { reason: string; idempotencyKey: string }) => request<P0Journal>(`/property-manager/p0/accounting/journals/${id}/reverse`, { method: "POST", token, body }),
  reconcileP0Journal: (token: string, body: { journalId: string; externalReference: string; amount: number; currency: string; reason: string }) => request<P0Reconciliation>("/property-manager/p0/accounting/reconcile", { method: "POST", token, body }),
  getP0Statement: (token: string, query: { ownerUserId: string; propertyId?: string; currency: string; from: string; to: string }) => request<P0Statement>(withQuery("/property-manager/p0/statements", query), { token }),
  finalizeP0Statement: (token: string, body: { ownerUserId: string; propertyId?: string; currency: string; from: string; to: string; idempotencyKey: string }) => request<P0Statement>("/property-manager/p0/statements/finalize", { method: "POST", token, body }),
  exportP0Statement: (token: string, snapshotId: string, format = "csv") => request<{ format: string; fileName: string; contentType: string; contentBase64: string; snapshotId: string }>(withQuery(`/property-manager/p0/statements/${snapshotId}/export`, { format }), { token }),
  getP0Profitability: (token: string, query: { currency: string; from: string; to: string; ownerUserId?: string; propertyId?: string }) => request<P0Profitability>(withQuery("/property-manager/p0/profitability", query), { token }),
  createP0Approval: (token: string, body: { ownerUserId: string; propertyId?: string; agreementId?: string; feeRuleId?: string; approvalType: string; description: string; amount: number; currency: string; evidenceDocumentIds?: string[] }) => request<P0Approval>("/property-manager/p0/approvals", { method: "POST", token, body }),
  getP0Approvals: (token: string, query?: { ownerUserId?: string; propertyId?: string; status?: string; page?: number; pageSize?: number }) => request<P0Approval[]>(withQuery("/property-manager/p0/approvals", query ?? {}), { token }),
  decideP0Approval: (token: string, id: string, body: { status: string; reason: string; rowVersion: number }) => request<P0Approval>(`/property-manager/p0/approvals/${id}/decision`, { method: "POST", token, body }),
  inviteP0Staff: (token: string, body: { staffUserId: string; role: string; propertyIds?: string[]; ownerIds?: string[]; canManageFinance?: boolean; canApprovePayouts?: boolean; approvalLimit?: number }) => request<P0StaffMembership>("/property-manager/p0/members", { method: "POST", token, body }),
  getP0Staff: (token: string) => request<P0StaffMembership[]>("/property-manager/p0/members", { token }),
  getP0StaffHistory: (token: string, id: string) => request<P0StaffEvent[]>(`/property-manager/p0/members/${id}/history`, { token }),
  acceptP0Staff: (token: string, id: string) => request<P0StaffMembership>(`/property-manager/p0/members/${id}/accept`, { method: "POST", token }),
  updateP0Staff: (token: string, id: string, body: { role: string; propertyIds?: string[]; ownerIds?: string[]; canManageFinance: boolean; canApprovePayouts: boolean; approvalLimit: number; status: string; rowVersion: number }) => request<P0StaffMembership>(`/property-manager/p0/members/${id}`, { method: "PATCH", token, body }),
  revokeP0Staff: (token: string, id: string, body: { status: string; reason: string }) => request<P0StaffMembership>(`/property-manager/p0/members/${id}/revoke`, { method: "POST", token, body }),
  getP0PayoutAvailability: (token: string, query: { ownerUserId: string; currency: string; from: string; to: string }) => request<P0PayoutAvailability>(withQuery("/property-manager/p0/payouts/availability", query), { token }),
  createP0Payout: (token: string, body: { ownerUserId: string; currency: string; periodFrom: string; periodTo: string; idempotencyKey: string; statementSnapshotId?: string }) => request<P0PayoutBatch>("/property-manager/p0/payouts/batches", { method: "POST", token, body }),
  getP0Payouts: (token: string, query?: { ownerUserId?: string; currency?: string; status?: string; page?: number; pageSize?: number }) => request<P0PayoutBatch[]>(withQuery("/property-manager/p0/payouts/batches", query ?? {}), { token }),
  approveP0Payout: (token: string, id: string, body: { reason: string; rowVersion: number }) => request<P0PayoutBatch>(`/property-manager/p0/payouts/batches/${id}/approve`, { method: "POST", token, body }),
  processP0Payout: (token: string, id: string, body?: { providerReference?: string; simulateFailure?: boolean; failureReason?: string }) => request<P0PayoutBatch>(`/property-manager/p0/payouts/batches/${id}/process`, { method: "POST", token, body: body ?? {} }),
  cancelP0Payout: (token: string, id: string, body: { reason: string; rowVersion: number }) => request<P0PayoutBatch>(`/property-manager/p0/payouts/batches/${id}/cancel`, { method: "POST", token, body }),
  retryP0Payout: (token: string, id: string) => request<P0PayoutBatch>(`/property-manager/p0/payouts/batches/${id}/retry`, { method: "POST", token }),
  getP0OwnerPortal: (token: string, managerUserId?: string) => request<P0OwnerPortal>(withQuery("/property-manager/p0/owner/portal", { managerUserId }), { token }),
  listProfessionalOwnerBlocks: (token: string, query?: { from?: string; to?: string; propertyId?: string }) => request<PmOwnerBlock[]>(withQuery("/property-manager/professional/owner-blocks", query ?? {}), { token }),
  createProfessionalOwnerBlock: (token: string, body: { ownerUserId: string; propertyId: string; startsAt: string; endsAt: string; timeZone?: string; reason: string; bookingId?: string; category?: string; notes?: string }) => request<PmOwnerBlock>("/property-manager/professional/owner-blocks", { method: "POST", token, body }),
  cancelProfessionalOwnerBlock: (token: string, id: string, body: { reason: string; rowVersion: number }) => request<PmOwnerBlock>(`/property-manager/professional/owner-blocks/${id}/cancel`, { method: "POST", token, body }),
  getProfessionalOwnerBlockHistory: (token: string, id: string) => request<PmOwnerBlockHistory[]>(`/property-manager/professional/owner-blocks/${id}/history`, { token }),
  listOwnerOperationalBlocks: (token: string, query?: { from?: string; to?: string }) => request<PmOwnerBlock[]>(withQuery("/property-manager/owner/owner-blocks", query ?? {}), { token }),
  createOwnerOperationalBlock: (token: string, body: { propertyId: string; startsAt: string; endsAt: string; timeZone?: string; reason: string; bookingId?: string; category?: string; notes?: string }) => request<PmOwnerBlock>("/property-manager/owner/owner-blocks", { method: "POST", token, body }),
  cancelOwnerOperationalBlock: (token: string, id: string, body: { reason: string; rowVersion: number }) => request<PmOwnerBlock>(`/property-manager/owner/owner-blocks/${id}/cancel`, { method: "POST", token, body }),
  listProfessionalReservations: (token: string, query?: { search?: string; status?: string; propertyId?: string; ownerUserId?: string }) => request<PmReservation[]>(withQuery("/property-manager/professional/reservations", query ?? {}), { token }),
  updateProfessionalReservation: (token: string, bookingId: string, body: { status: string; checkIn?: string; checkOut?: string; expectedUpdatedTicks?: number }) => request<PmReservation>(`/property-manager/professional/reservations/${bookingId}`, { method: "PATCH", token, body }),
  previewProfessionalReservationDateChange: (token: string, bookingId: string, body: { checkIn: string; checkOut: string }) => request<PmReservationDateChangePreview>(`/property-manager/professional/reservations/${bookingId}/date-change-preview`, { method: "POST", token, body }),
  cancelProfessionalReservation: (token: string, bookingId: string, body: { reason: string; idempotencyKey?: string; expectedUpdatedTicks?: number }) => request<PmReservation>(`/property-manager/professional/reservations/${bookingId}/cancel`, { method: "POST", token, body }),
  getProfessionalReservationHistory: (token: string, bookingId: string) => request<PmReservationEvent[]>(`/property-manager/professional/reservations/${bookingId}/history`, { token }),
  addProfessionalReservationNote: (token: string, bookingId: string, body: { body: string; visibility?: string }) => request<PmReservationNote>(`/property-manager/professional/reservations/${bookingId}/notes`, { method: "POST", token, body }),
  listProfessionalCalendar: (token: string, from: string, to: string, propertyId?: string) => request<PmCalendarItem[]>(withQuery("/property-manager/professional/calendar", { from, to, propertyId }), { token }),
  getProfessionalOperationalDashboard: (token: string) => request<PmOperationalDashboard>("/property-manager/professional/dashboard", { token }),
  listProfessionalTimeline: (token: string, query?: { propertyId?: string; from?: string; to?: string }) => request<{ id: string; actorUserId?: string | null; actorRole: string; action: string; subjectType: string; subjectId?: string | null; reason: string; metadataJson: string; createdAt: string }[]>(withQuery("/property-manager/professional/timeline", query ?? {}), { token }),
  listProfessionalMaintenance: (token: string, query?: { status?: string; propertyId?: string }) => request<PmMaintenanceCase[]>(withQuery("/property-manager/professional/maintenance", query ?? {}), { token }),
  createProfessionalMaintenance: (token: string, body: { ownerUserId: string; propertyId: string; title: string; description: string; priority?: string; vendorId?: string; quoteAmount?: number; currency?: string }) => request<PmMaintenanceCase>("/property-manager/professional/maintenance", { method: "POST", token, body }),
  transitionProfessionalMaintenance: (token: string, id: string, body: { status: string; vendorId?: string; approvedAmount?: number; expenseAmount?: number; ownerCharge?: number; scheduledAt?: string; details?: string; rowVersion: number; ownerApprovalId?: string }) => request<PmMaintenanceCase>(`/property-manager/professional/maintenance/${id}`, { method: "PATCH", token, body }),
  addProfessionalMaintenanceQuote: (token: string, id: string, body: { vendorId: string; amount: number; scope: string; currency?: string; expiresAt?: string }) => request<PmMaintenanceQuote>(`/property-manager/professional/maintenance/${id}/quotes`, { method: "POST", token, body }),
  listProfessionalMaintenanceQuotes: (token: string, id: string) => request<PmMaintenanceQuote[]>(`/property-manager/professional/maintenance/${id}/quotes`, { token }),
  getProfessionalMaintenanceHistory: (token: string, id: string) => request<PmMaintenanceEvent[]>(`/property-manager/professional/maintenance/${id}/history`, { token }),
  listProfessionalCleaning: (token: string, query?: { propertyId?: string; from?: string; to?: string }) => request<PmCleaning[]>(withQuery("/property-manager/professional/cleaning", query ?? {}), { token }),
  createProfessionalCleaning: (token: string, body: { propertyId: string; bookingId?: string; dueAt: string; assignedUserId?: string; vendorId?: string; checklistJson?: string }) => request<PmCleaning>("/property-manager/professional/cleaning", { method: "POST", token, body }),
  updateProfessionalCleaning: (token: string, id: string, body: { status: string; checklistJson: string; issues: string; photosJson: string; rowVersion: number }) => request<PmCleaning>(`/property-manager/professional/cleaning/${id}`, { method: "PATCH", token, body }),
  listProfessionalAssets: (token: string, query?: { propertyId?: string; status?: string }) => request<PmAsset[]>(withQuery("/property-manager/professional/assets", query ?? {}), { token }),
  createProfessionalAsset: (token: string, body: { propertyId: string; assetTag: string; name: string; category: string; quantity?: number; location?: string; metadataJson?: string; photosJson?: string; description?: string; serialReference?: string; purchaseDate?: string; purchaseCost?: number; warrantyExpiry?: string; condition?: string }) => request<PmAsset>("/property-manager/professional/assets", { method: "POST", token, body }),
  updateProfessionalAsset: (token: string, id: string, body: { status: string; quantity: number; location: string; metadataJson: string; photosJson: string; rowVersion: number; description?: string; serialReference?: string; purchaseDate?: string; purchaseCost?: number; warrantyExpiry?: string; condition?: string }) => request<PmAsset>(`/property-manager/professional/assets/${id}`, { method: "PATCH", token, body }),
  listProfessionalIncidents: (token: string, query?: { propertyId?: string; status?: string }) => request<PmIncident[]>(withQuery("/property-manager/professional/incidents", query ?? {}), { token }),
  createProfessionalIncident: (token: string, body: { propertyId: string; bookingId?: string; incidentType: string; severity: string; occurredAt: string; description: string; involvedPartiesJson?: string; evidenceJson?: string; actionTaken?: string; followUp?: string; financialImpact?: number; insuranceReference?: string }) => request<PmIncident>("/property-manager/professional/incidents", { method: "POST", token, body }),
  updateProfessionalIncident: (token: string, id: string, body: { status: string; actionTaken: string; followUp: string; insuranceReference?: string; rowVersion: number }) => request<PmIncident>(`/property-manager/professional/incidents/${id}`, { method: "PATCH", token, body }),
  listProfessionalInspections: (token: string, propertyId?: string) => request<PmInspection[]>(withQuery("/property-manager/professional/inspections", { propertyId }), { token }),
  createProfessionalInspection: (token: string, body: { propertyId: string; assignedUserId?: string; inspectionType: string; scheduledAt: string; checklistJson?: string }) => request<PmInspection>("/property-manager/professional/inspections", { method: "POST", token, body }),
  updateProfessionalInspection: (token: string, id: string, body: { status: string; evidenceJson: string; findingsJson: string; rowVersion: number }) => request<PmInspection>(`/property-manager/professional/inspections/${id}`, { method: "PATCH", token, body }),
  createProfessionalInspectionWorkOrder: (token: string, id: string, body: { scope: string; vendorId?: string; quoteAmount?: number; slaDueAt?: string }) => request<PropertyManagerWorkOrder>(`/property-manager/professional/inspections/${id}/work-order`, { method: "POST", token, body }),
};

export function formatMoney(amount: number, currency = "USD") {
  if (currency.toUpperCase() === "PERCENT") {
    return `${new Intl.NumberFormat("en-US", {
      maximumFractionDigits: amount % 1 === 0 ? 0 : 2,
    }).format(amount)}%`;
  }

  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
    maximumFractionDigits: amount % 1 === 0 ? 0 : 2,
  }).format(amount);
}
