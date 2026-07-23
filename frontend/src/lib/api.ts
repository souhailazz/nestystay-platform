export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "/api").replace(/\/+$/, "");

export type UserRole = "Guest" | "Host" | "Admin" | "Officer" | "PropertyManager";

export type RegisterUserRequest = {
  email: string;
  password: string;
  displayName: string;
  phone?: string;
  confirmPassword: string;
  acceptedTerms: boolean;
  acceptedPrivacy: boolean;
  role: Extract<UserRole, "Guest" | "Host">;
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
};

export type LoginResponse = {
  userId: string;
  email: string;
  requiresTwoFactor: boolean;
  challengeId: string;
  challengeExpiresAt: string;
};

export type VerifyTwoFactorResponse = {
  userId: string;
  accessToken: string;
  expiresAt: string;
  roles: UserRole[];
};

export type GoogleSignInRequest = {
  email: string;
  displayName: string;
  googleSubject?: string | null;
  pictureUrl?: string | null;
  credential?: string | null;
};

export type GoogleSignInResponse = VerifyTwoFactorResponse & {
  email: string;
  displayName: string;
  provider: "Google" | string;
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
};

export type CreateBookingRequest = BookingQuoteRequest & {
  guestUserId: string;
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
};

export type OnboardOfficerRequest = {
  userId?: string | null;
  badgeNumber: string;
  parish: string;
  coverageArea: string;
  isActiveOffDuty: boolean;
  isRetired: boolean;
  verificationMetadata?: string | null;
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
  brand: string;
  last4: string;
  expMonth: number;
  expYear: number;
  isDefault: boolean;
  createdAt: string;
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
  fileName: string;
  contentType: string;
  sizeBytes: number;
  url: string;
  status: string;
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
};

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
};

export type AuthFlowResult = {
  id: string;
  userId?: string | null;
  flowType: string;
  destination: string;
  status: string;
  code: string;
  token: string;
  expiresAt: string;
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

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers);

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (options.token) {
    headers.set("Authorization", `Bearer ${options.token}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    try {
      const problem = (await response.json()) as { title?: string; detail?: string; message?: string };
      message = problem.title ?? problem.detail ?? problem.message ?? message;
    } catch {
      message = response.statusText || message;
    }
    throw new ApiError(message, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function withQuery(path: string, params: Record<string, string | undefined>) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value) {
      search.set(key, value);
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
  register: (body: RegisterUserRequest) =>
    request<RegisterUserResponse>("/auth/register", { method: "POST", body }),
  login: (body: LoginRequest) => request<LoginResponse>("/auth/login", { method: "POST", body }),
  googleSignIn: (body: GoogleSignInRequest) =>
    request<GoogleSignInResponse>("/auth/google", { method: "POST", body }),
  verifyTwoFactor: (challengeId: string, code: string) =>
    request<VerifyTwoFactorResponse>("/auth/2fa/verify", {
      method: "POST",
      body: { challengeId, code },
    }),
  getProperties: () => request<PropertyListing[]>("/properties"),
  getProperty: (id: string) => request<PropertyListing>(`/properties/${id}`),
  createProperty: (body: CreatePropertyRequest) =>
    request<PropertyListing>("/properties", { method: "POST", body }),
  getBookings: (token?: string) =>
    request<Booking[]>("/bookings", { token }),
  getBooking: (id: string, token?: string) => request<Booking>(`/bookings/${id}`, { token }),
  quoteBooking: (body: BookingQuoteRequest) =>
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
  getPlatformModules: () => request<unknown[]>("/platform/modules"),
  getPlatformPortals: () => request<unknown[]>("/platform/portals"),
  getPlatformVendors: () => request<unknown[]>("/platform/vendors"),
  getBookingWorkflow: () => request<unknown>("/platform/booking-workflow"),
  getPricebook: () => request<unknown[]>("/platform/pricebook"),
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
  getBadgeEligibility: (body: PurchaseBadgeRequest) =>
    request<BadgeEligibility>("/badges-pricing/badges/eligibility", { method: "POST", body }),
  purchaseBadge: (body: PurchaseBadgeRequest) =>
    request<BadgeAssignment>("/badges-pricing/badges/purchase", { method: "POST", body }),
  getBadgeAssignments: (subjectType?: string, subjectId?: string) =>
    request<BadgeAssignment[]>(
      withQuery("/badges-pricing/badges/assignments", { subjectType, subjectId }),
    ),
  getBadgeFeatureAccess: (subjectType: string, subjectId: string) =>
    request<BadgeFeatureAccess>(
      `/badges-pricing/badges/features/${encodeURIComponent(subjectType)}/${encodeURIComponent(subjectId)}`,
    ),
  expireBadgeAssignment: (assignmentId: string, token: string) =>
    request<BadgeAssignment>(`/badges-pricing/badges/assignments/${assignmentId}/expire`, {
      method: "POST",
      token,
    }),
  suspendBadgeAssignment: (assignmentId: string, token: string) =>
    request<BadgeAssignment>(`/badges-pricing/badges/assignments/${assignmentId}/suspend`, {
      method: "POST",
      token,
    }),
  getBadgeRenewals: (assignmentId?: string) =>
    request<BadgeRenewal[]>(withQuery("/badges-pricing/renewals", { assignmentId })),
  payBadgeRenewal: (assignmentId: string) =>
    request<BadgeRenewal>(`/badges-pricing/renewals/${assignmentId}/pay`, { method: "POST" }),
  getCampaigns: () => request<Campaign[]>("/badges-pricing/campaigns"),
  createCampaign: (body: CreateCampaignRequest, token: string) =>
    request<Campaign>("/badges-pricing/campaigns", { method: "POST", body, token }),
  enrollCampaign: (campaignKey: string, subjectType: string, subjectId: string) =>
    request<CampaignEnrollment>(`/badges-pricing/campaigns/${encodeURIComponent(campaignKey)}/enroll`, {
      method: "POST",
      body: { subjectType, subjectId },
    }),
  upsertFoundingBenefit: (body: FoundingBenefitRequest, token: string) =>
    request<FoundingBenefit>("/badges-pricing/founding-benefits", { method: "POST", body, token }),
  getFoundingBenefit: (propertyId: string) =>
    request<FoundingBenefit>(`/badges-pricing/founding-benefits/${propertyId}`),
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
  createWellnessVisit: (body: CreateWellnessVisitRequest) =>
    request<WellnessVisit>("/wellness/visits", { method: "POST", body }),
  getWellnessVisits: (params: { hostUserId?: string; propertyId?: string; officerId?: string } = {}) =>
    request<WellnessVisit[]>("/wellness/visits" + withQuery("", params)),
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
  submitWellnessReport: (visitId: string, body: { officerBadgeNumber: string; notes: string; photos?: string[] }) =>
    request<WellnessVisit>(`/wellness/visits/${visitId}/report`, { method: "POST", body }),
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
  addPaymentMethod: (userId: string, token: string, body: { brand: string; last4: string; expMonth: number; expYear: number; isDefault?: boolean }) =>
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
  getInbox: (userId: string, token: string) =>
    request<MessagingInbox>(withQuery("/spec/messages/inbox", { userId }), { token }),
  getConversation: (conversationId: string, userId: string, token: string) =>
    request<Conversation>(withQuery(`/spec/messages/conversations/${conversationId}`, { userId }), { token }),
  createConversation: (userId: string, token: string, body: { subject: string; bookingId?: string | null; isSupportThread: boolean; participants: { userId: string; displayName: string; role: string }[]; initialMessage: string }) =>
    request<Conversation>(withQuery("/spec/messages/conversations", { userId }), { method: "POST", token, body }),
  sendMessage: (conversationId: string, userId: string, token: string, body: { body: string; attachments?: MessageAttachment[] }) =>
    request<Message>(withQuery(`/spec/messages/conversations/${conversationId}/messages`, { userId }), { method: "POST", token, body }),
  markConversationRead: (conversationId: string, userId: string, token: string) =>
    request<void>(withQuery(`/spec/messages/conversations/${conversationId}/read`, { userId }), { method: "POST", token }),
  getHostOperations: (hostUserId: string, token: string) =>
    request<HostOperations>(`/spec/host/${hostUserId}/operations`, { token }),
  saveHostPricingRule: (hostUserId: string, token: string, body: Omit<HostPricingRule, "id" | "hostUserId">) =>
    request<HostPricingRule>(`/spec/host/${hostUserId}/pricing-rules`, { method: "POST", token, body }),
  saveHostPromotion: (hostUserId: string, token: string, body: Omit<HostPromotion, "id" | "hostUserId">) =>
    request<HostPromotion>(`/spec/host/${hostUserId}/promotions`, { method: "POST", token, body }),
  getAdminOperations: (token: string) => request<AdminOperations>("/spec/admin/operations", { token }),
  createAdminCase: (token: string, body: { caseType: string; subjectType: string; subjectId?: string | null; priority: string; reason: string; assignedTo?: string }) =>
    request<AdminCase>("/spec/admin/cases", { method: "POST", token, body }),
  resolveAdminCase: (token: string, caseId: string, body: { resolutionNotes: string; status?: string }) =>
    request<AdminCase>(`/spec/admin/cases/${caseId}/resolve`, { method: "POST", token, body }),
  getAuditLog: (token: string) => request<AuditEvent[]>("/spec/admin/audit-log", { token }),
  startAuthFlow: (body: { userId?: string | null; flowType: string; destination: string }) =>
    request<AuthFlowResult>("/spec/auth/flows", { method: "POST", body }),
  completeAuthFlow: (body: { flowId: string; code: string }) =>
    request<AuthFlowResult>("/spec/auth/flows/complete", { method: "POST", body }),
  generateRecoveryCodes: (userId: string, token: string) =>
    request<{ code: string; used: boolean }[]>(`/spec/auth/${userId}/recovery-codes`, { method: "POST", token }),
  getSocialAuthConfig: () => request<SocialAuthConfig>("/spec/auth/social-config"),
};

export function formatMoney(amount: number, currency = "USD") {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
    maximumFractionDigits: amount % 1 === 0 ? 0 : 2,
  }).format(amount);
}
