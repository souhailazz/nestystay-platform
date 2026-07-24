export interface JamaicanExperience {
  id: string;
  title: string;
  parish: string;
  category: "Adventure" | "Culinary" | "Culture" | "Relaxation";
  pricePerPerson: number;
  currency: string;
  durationHours: number;
  imageUrl: string;
  description: string;
  includedItems: string[];
}

export interface JournalArticleItem {
  id: string;
  title: string;
  slug: string;
  category: "Travel Guide" | "Culture" | "Food" | "Safety";
  authorName: string;
  publishedAt: string;
  readTimeMinutes: number;
  summary: string;
  content: string;
  imageUrl: string;
  relatedPropertyIds: string[];
}
