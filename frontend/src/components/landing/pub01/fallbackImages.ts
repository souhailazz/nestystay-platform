/**
 * TODO: properties in the database currently have no photos (imageUrl is
 * optional and usually empty). Until real photos are uploaded, the daylight
 * showcase falls back to the approved Unsplash imagery from the PUB-01 design.
 */
export const showcaseFallbackImages = [
  {
    src: "https://images.unsplash.com/photo-1571896349842-33c89424de2d?q=80&w=1600&auto=format&fit=crop",
    alt: "Villa terrace and pool glowing in warm evening light above the Negril cliffs",
  },
  {
    src: "https://images.unsplash.com/photo-1468413253725-0d5181091126?q=80&w=1000&auto=format&fit=crop",
    alt: "Palm-shaded sandy cove at Treasure Beach",
  },
  {
    src: "https://images.unsplash.com/photo-1536376072261-38c75010e6c9?q=80&w=1000&auto=format&fit=crop",
    alt: "Plant-filled creative loft with olive sofa in Kingston",
  },
  {
    src: "https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?q=80&w=800&auto=format&fit=crop",
    alt: "Misty ridge in the Blue Mountains",
  },
] as const;

export const heroFallbackImages = {
  valley: {
    src: "https://images.unsplash.com/photo-1552733407-5d5c46c3bb3b?q=80&w=2200&auto=format&fit=crop",
    alt: "Golden light over a lush green river valley in Jamaica",
  },
  featured: {
    src: "https://images.unsplash.com/photo-1416331108676-a22ccb276e35?q=80&w=900&auto=format&fit=crop",
    alt: "Warm-lit villa and pool at dusk above the Negril cliffs",
  },
  travelers: [
    "https://images.unsplash.com/photo-1494790108377-be9c29b29330?q=80&w=100&auto=format&fit=crop",
    "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?q=80&w=100&auto=format&fit=crop",
    "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?q=80&w=100&auto=format&fit=crop",
  ],
} as const;

export function showcaseImage(index: number, imageUrl?: string, title?: string) {
  if (imageUrl) return { src: imageUrl, alt: title ? `Photo of ${title}` : "Property photo" };
  return showcaseFallbackImages[Math.abs(index) % showcaseFallbackImages.length];
}
