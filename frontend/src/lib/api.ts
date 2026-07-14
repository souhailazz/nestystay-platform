export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "/api").replace(/\/+$/, "");

export type UserRole = "Guest" | "Host" | "Admin" | "Officer" | "PropertyManager";

export type RegisterUserRequest = {
  email: string;
  password: string;
  displayName: string;
  phone?: string;
};

export type RegisterUserResponse = {
  userId: string;
  email: string;
  displayName: string;
  requiresTwoFactor: boolean;
  twoFactorCode: string;
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
  twoFactorCode: string;
};

export type VerifyTwoFactorResponse = {
  userId: string;
  accessToken: string;
  expiresAt: string;
  roles: UserRole[];
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
  verifyTwoFactor: (challengeId: string, code: string) =>
    request<VerifyTwoFactorResponse>("/auth/2fa/verify", {
      method: "POST",
      body: { challengeId, code },
    }),
  getProperties: () => request<PropertyListing[]>("/properties"),
  getProperty: (id: string) => request<PropertyListing>(`/properties/${id}`),
  createProperty: (body: CreatePropertyRequest) =>
    request<PropertyListing>("/properties", { method: "POST", body }),
  getBookings: (guestUserId?: string) =>
    request<Booking[]>(guestUserId ? `/bookings?guestUserId=${guestUserId}` : "/bookings"),
  getBooking: (id: string) => request<Booking>(`/bookings/${id}`),
  quoteBooking: (body: BookingQuoteRequest) =>
    request<BookingQuote>("/bookings/quote", { method: "POST", body }),
  createBooking: (body: CreateBookingRequest) =>
    request<Booking>("/bookings", { method: "POST", body }),
  resolveVerification: (bookingId: string, passed: boolean, providerReference: string) =>
    request<Booking>(`/bookings/${bookingId}/verification-result`, {
      method: "POST",
      body: { passed, providerReference },
    }),
  capturePayment: (bookingId: string) =>
    request<Booking>(`/bookings/${bookingId}/capture-payment`, { method: "POST" }),
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
};

export function formatMoney(amount: number, currency = "USD") {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
    maximumFractionDigits: amount % 1 === 0 ? 0 : 2,
  }).format(amount);
}
