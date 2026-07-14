export type StayImage = {
  src: string;
  alt: string;
};

export const stayImages: StayImage[] = [
  {
    src: "/assets/stays/jamaica-seaview-villa.png",
    alt: "Jamaican seaview villa with an infinity pool above turquoise water",
  },
  {
    src: "/assets/stays/jamaica-kingston-townhouse.png",
    alt: "Leafy Kingston townhouse stay with mountain views",
  },
  {
    src: "/assets/stays/jamaica-beach-cottage.png",
    alt: "Jamaican beach cottage veranda beside clear Caribbean water",
  },
  {
    src: "/assets/stays/jamaica-blue-mountain-retreat.png",
    alt: "Blue Mountains eco-retreat with a veranda overlooking misty hills",
  },
];

export function getStayImage(index = 0) {
  return stayImages[Math.abs(index) % stayImages.length];
}
