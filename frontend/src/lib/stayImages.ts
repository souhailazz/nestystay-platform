export type StayImage = {
  src: string;
  alt: string;
  srcSet: string;
};

export const stayImages: StayImage[] = [
  {
    src: "/assets/stays/jamaica-seaview-villa.webp",
    alt: "Jamaican seaview villa with an infinity pool above turquoise water",
    srcSet: "/assets/stays/sm/jamaica-seaview-villa.webp 768w, /assets/stays/jamaica-seaview-villa.webp 1568w",
  },
  {
    src: "/assets/stays/jamaica-kingston-townhouse.webp",
    alt: "Leafy Kingston townhouse stay with mountain views",
    srcSet: "/assets/stays/sm/jamaica-kingston-townhouse.webp 768w, /assets/stays/jamaica-kingston-townhouse.webp 1586w",
  },
  {
    src: "/assets/stays/jamaica-beach-cottage.webp",
    alt: "Jamaican beach cottage veranda beside clear Caribbean water",
    srcSet: "/assets/stays/sm/jamaica-beach-cottage.webp 768w, /assets/stays/jamaica-beach-cottage.webp 1568w",
  },
  {
    src: "/assets/stays/jamaica-blue-mountain-retreat.webp",
    alt: "Blue Mountains eco-retreat with a veranda overlooking misty hills",
    srcSet: "/assets/stays/sm/jamaica-blue-mountain-retreat.webp 768w, /assets/stays/jamaica-blue-mountain-retreat.webp 1586w",
  },
];

export function getStayImage(index = 0) {
  return stayImages[Math.abs(index) % stayImages.length];
}
