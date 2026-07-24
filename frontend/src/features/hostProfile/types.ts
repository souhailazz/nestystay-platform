export interface HostProfileItem {
  id: string;
  hostUserId: string;
  displayName: string;
  parish: string;
  bio: string;
  avatarUrl?: string;
  coverPhotoUrl?: string;
  responseTime: string;
  badges: string[];
  listingIds: string[];
  totalBookings: number;
  ratingAverage: number;
  isPublic: boolean;
  linkMiUrl?: string;
}
