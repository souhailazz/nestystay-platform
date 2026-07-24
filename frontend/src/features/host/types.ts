export interface HostAnalyticsData {
  totalRevenue: number;
  occupancyRate: number;
  averageDailyRate: number;
  totalViews: number;
  searchImpressions: number;
  conversionRate: number;
  refundsTotal: number;
  currency: string;
  revenueByMonth: { month: string; revenue: number }[];
  guestOrigins: { country: string; percentage: number }[];
}

export interface PropertyWizardData {
  id?: string;
  title: string;
  location: string;
  country: string;
  propertyType: string;
  capacityAdults: number;
  capacityChildren: number;
  bedrooms: number;
  bathrooms: number;
  nightlyRate: number;
  currency: string;
  amenities: string[];
  description: string;
  houseRules: string;
  minimumNights: number;
  photos: { id: string; url: string; isCover: boolean; sortOrder: number }[];
  cancellationPolicy: string;
  verificationEnabled: boolean;
  insuraGuestEnabled: boolean;
}

export interface HostPricingRule {
  id: string;
  propertyId: string;
  seasonName: string;
  startDate: string;
  endDate: string;
  nightlyRate: number;
  minimumNights: number;
}

export interface HostPromotion {
  id: string;
  propertyId: string;
  name: string;
  discountPercent: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
}
