/**
 * Official NestyStay emblem (design-system/uploads) cropped to the hammock
 * scene inside a cream circle, as specified by Design System v2.
 */
export function Emblem({ size = 44, className = "" }: { size?: number; className?: string }) {
  // Crop ratios derived from the approved PUB-01 markup
  // (44px circle → img width 118px, margin -6px 0 0 -37px).
  const imgWidth = Math.round(size * 2.68);
  // PUB-01's 64px footer crop is -10px, not a uniform scale of the 44px crop.
  const marginTop = size === 64 ? -10 : -Math.round(size * 0.136);
  const marginLeft = -Math.round(size * 0.84);

  return (
    <span className={`ns-emblem ${className}`} style={{ width: size, height: size }}>
      <img
        src="/assets/nesty/emblem.png"
        alt="NestyStay emblem — man in a hammock between two coconut palms"
        style={{ width: imgWidth, margin: `${marginTop}px 0 0 ${marginLeft}px` }}
      />
    </span>
  );
}
