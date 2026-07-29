import { PillLink } from "../../design/PillButton";
import { SiteFooter } from "../../design/SiteFooter";

const fireflies = [
  { cx: 98, cy: 300, r: 2, halo: 6, dur: "3.2s", delay: "0s" },
  { cx: 210, cy: 238, r: 1.8, halo: 6, dur: "4.1s", delay: "0.7s" },
  { cx: 352, cy: 252, r: 1.8, halo: 6, dur: "5.3s", delay: "1.4s" },
  { cx: 470, cy: 316, r: 2, halo: 6, dur: "3.8s", delay: "2.1s" },
  { cx: 260, cy: 398, r: 1.6, halo: 5, dur: "6s", delay: "0.4s" },
  { cx: 430, cy: 380, r: 1.6, halo: 5, dur: "4.6s", delay: "1.9s" },
];

/**
 * 05 · NIGHTFALL — the logo comes alive. Night illustration reprising the
 * official emblem's vocabulary (hammock between two coconut palms), fireflies
 * and moon, with the footer fused to the scene's ground.
 */
export function NightfallScene() {
  return (
    <section className="ns-night-sec">
      <div className="ns-night-sec__inner">
        {/* night scene: the logo, alive */}
        <div className="ns-night-sec__scene">
          <svg
            viewBox="0 0 560 470"
            role="img"
            aria-label="Night scene of the NestyStay logo — a hammock between two coconut palms, fireflies glowing"
          >
            <defs>
              <radialGradient id="nsGlow" cx="50%" cy="62%" r="55%">
                <stop offset="0%" stopColor="#FFD21F" stopOpacity="0.15" />
                <stop offset="55%" stopColor="#FFD21F" stopOpacity="0.05" />
                <stop offset="100%" stopColor="#FFD21F" stopOpacity="0" />
              </radialGradient>
            </defs>
            <ellipse cx="280" cy="300" rx="262" ry="215" fill="url(#nsGlow)" />
            {/* moon (parallax: distant plane) */}
            <g data-depth="0.1">
              <circle cx="470" cy="64" r="22" fill="#F3EAC8" opacity="0.85" />
              <circle cx="479" cy="58" r="19" fill="#041F1F" />
            </g>
            {/* ground */}
            <ellipse cx="280" cy="424" rx="238" ry="34" fill="#0B332C" />
            <path d="M120 404 q6 -16 10 -2 q6 -14 10 0 Z" fill="#14503F" />
            <path d="M420 406 q6 -16 10 -2 q6 -14 10 0 Z" fill="#14503F" />
            <path d="M268 430 q5 -14 9 -1 q5 -12 9 1 Z" fill="#14503F" />
            {/* left palm */}
            <path d="M132 408 C120 330 118 250 138 178 L162 182 C148 254 152 330 164 404 Z" fill="#0C332C" />
            <path d="M150 180 C120 168 88 168 62 186 C96 186 122 192 146 202 Z" fill="#145040" />
            <path d="M150 178 C128 150 100 136 66 138 C98 152 124 166 142 188 Z" fill="#0F4436" />
            <path d="M152 176 C148 144 158 116 182 98 C172 128 168 156 168 184 Z" fill="#145040" />
            <path d="M154 180 C176 154 206 142 240 146 C210 158 186 172 168 192 Z" fill="#0F4436" />
            <path d="M154 184 C184 176 214 180 238 196 C208 194 182 196 162 202 Z" fill="#145040" />
            <path d="M152 182 C142 156 118 140 92 140 C118 154 138 166 148 186 Z" fill="#145040" />
            <path d="M156 178 C168 150 194 132 224 130 C196 146 176 160 166 184 Z" fill="#145040" />
            <path d="M150 186 C124 190 100 202 84 220 C110 208 134 200 156 198 Z" fill="#0F4436" />
            <circle cx="150" cy="196" r="9" fill="#FFD21F" />
            <circle cx="164" cy="202" r="8" fill="#E8BD12" />
            <circle cx="140" cy="206" r="8" fill="#E8BD12" />
            <circle cx="154" cy="210" r="7.5" fill="#FFD21F" />
            {/* right palm */}
            <path d="M428 408 C440 330 442 250 422 178 L398 182 C412 254 408 330 396 404 Z" fill="#0C332C" />
            <path d="M410 180 C440 168 472 168 498 186 C464 186 438 192 414 202 Z" fill="#145040" />
            <path d="M410 178 C432 150 460 136 494 138 C462 152 436 166 418 188 Z" fill="#0F4436" />
            <path d="M408 176 C412 144 402 116 378 98 C388 128 392 156 392 184 Z" fill="#145040" />
            <path d="M406 180 C384 154 354 142 320 146 C350 158 374 172 392 192 Z" fill="#0F4436" />
            <path d="M406 184 C376 176 346 180 322 196 C352 194 378 196 398 202 Z" fill="#145040" />
            <path d="M408 182 C418 156 442 140 468 140 C442 154 422 166 412 186 Z" fill="#145040" />
            <path d="M404 178 C392 150 366 132 336 130 C364 146 384 160 394 184 Z" fill="#145040" />
            <path d="M410 186 C436 190 460 202 476 220 C450 208 426 200 404 198 Z" fill="#0F4436" />
            <circle cx="410" cy="196" r="9" fill="#FFD21F" />
            <circle cx="396" cy="202" r="8" fill="#E8BD12" />
            <circle cx="420" cy="206" r="8" fill="#E8BD12" />
            <circle cx="406" cy="210" r="7.5" fill="#FFD21F" />
            {/* anim: hammock sway, rotate ±2.5° about the attachment line, 4s ease-in-out infinite */}
            <g className="ns-sway">
              {/* hammock ropes */}
              <path d="M158 296 L196 318 M402 296 L364 318" stroke="#0C332C" strokeWidth="5" strokeLinecap="round" />
              {/* hammock */}
              <path d="M196 318 C240 366 320 366 364 318 C330 396 230 396 196 318 Z" fill="#FFD21F" />
              <path d="M204 326 C244 364 316 364 356 326 C322 380 238 380 204 326 Z" fill="#0E4A45" />
              {/* hammock rests empty — the yard is waiting */}
            </g>
            {/* anim: fireflies — flicker + slow drift, per-firefly duration 3–6s with phase offsets */}
            {fireflies.map((firefly) => (
              <g key={`${firefly.cx}-${firefly.cy}`} data-depth="0.4">
                <g
                  className="ns-ff"
                  style={{ "--ffd": firefly.dur, "--ffdel": firefly.delay } as React.CSSProperties}
                >
                  <circle cx={firefly.cx} cy={firefly.cy} r={firefly.halo} fill="#FFD21F" opacity="0.16" />
                  <circle cx={firefly.cx} cy={firefly.cy} r={firefly.r} fill="#FFD21F" />
                </g>
              </g>
            ))}
          </svg>
        </div>

        <div className="ns-night-sec__copy">
          <div className="ns-eyebrow">05 · NIGHTFALL</div>
          <h2 className="ns-display ns-night-sec__title">
            Every day ends
            <br />
            at your <em>yard.</em>
          </h2>
          <p className="ns-night-sec__lede">Hammock hung, coconuts heavy, night warm. Your spot is waiting.</p>
          <PillLink variant="sun" href="/explore" arrow>
            Explore Stays
          </PillLink>
        </div>
      </div>

      {/* footer — the illustration's ground extends full width */}
      <div className="ns-night-sec__footerwrap">
        <SiteFooter />
      </div>
    </section>
  );
}
