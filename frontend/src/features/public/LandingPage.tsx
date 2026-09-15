import { useRef, type CSSProperties, type ReactNode } from "react";
import { motion, useReducedMotion, useScroll, useTransform } from "framer-motion";
import { AppLink } from "../../components/AppLink";
import { PublicFooter } from "../../components/layout/PublicShell";
import { cx } from "../../lib/ui";
import "./public.css";

/**
 * PUB-01 — Design System v2 landing. A day on the island in five scenes:
 * 01 Golden Hour (hero) → 02 Broad Daylight (stays) → 03 High Noon (yellow
 * statement) → 04 Dusk (security + 119) → 05 Nightfall (logo scene + footer).
 * Photos are Unsplash placeholders from the design reference (swap for real
 * property images from the backend/R2 when available).
 */

function Eyebrow({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <div className={cx("flex items-center gap-3 font-sans text-xs font-semibold tracking-[0.28em]", className)}>
      <span aria-hidden="true" className="inline-block h-px w-[34px] bg-current" />
      {children}
    </div>
  );
}

function Reveal({
  children,
  delay = 0,
  className,
}: {
  children: ReactNode;
  delay?: number;
  className?: string;
}) {
  const reduce = useReducedMotion();
  return (
    <motion.div
      className={className}
      initial={reduce ? false : { opacity: 0, y: 24 }}
      transition={{ duration: 0.7, ease: "easeOut", delay }}
      viewport={{ once: true, amount: 0.15 }}
      whileInView={{ opacity: 1, y: 0 }}
    >
      {children}
    </motion.div>
  );
}

const arrowCircle =
  "inline-flex size-11 shrink-0 items-center justify-center rounded-full bg-deep text-[17px] text-yellow transition-colors hover:bg-deep-hover";

type Stay = {
  name: string;
  location: string;
  price: string;
  img: string;
  alt: string;
  badge: ReactNode;
  wide?: boolean;
};

const stays: Stay[] = [
  {
    name: "Cliffside Retreat",
    location: "Negril · Westmoreland",
    price: "$450",
    img: "https://images.unsplash.com/photo-1571896349842-33c89424de2d?q=80&w=1600&auto=format&fit=crop",
    alt: "Villa terrace and pool glowing in warm evening light above the Negril cliffs",
    badge: <span className="inline-flex rounded-pill bg-mint-tint px-3.5 py-1.5 text-[10.5px] font-bold tracking-[0.1em] text-mint-text">✦ WELLNESS HOST</span>,
    wide: true,
  },
  {
    name: "Sea Grape Cottage",
    location: "Treasure Beach · St. Elizabeth",
    price: "$410",
    img: "https://images.unsplash.com/photo-1468413253725-0d5181091126?q=80&w=1000&auto=format&fit=crop",
    alt: "Palm-shaded sandy cove at Treasure Beach",
    badge: <span className="inline-flex rounded-pill bg-deep-hover px-3.5 py-1.5 text-[10.5px] font-bold tracking-[0.1em] text-white">✓ VERIFIED</span>,
  },
  {
    name: "Uptown Loft",
    location: "Kingston · St. Andrew",
    price: "$195",
    img: "https://images.unsplash.com/photo-1536376072261-38c75010e6c9?q=80&w=1000&auto=format&fit=crop",
    alt: "Plant-filled creative loft with olive sofa in Kingston",
    badge: <span className="inline-flex rounded-pill bg-cream px-3.5 py-1.5 text-[10.5px] font-bold tracking-[0.1em] text-[#66705F]">FREE HOST</span>,
  },
];

function GoldenHourHero() {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLElement>(null);
  const { scrollYProgress } = useScroll({ target: ref, offset: ["start start", "end start"] });
  const imgY = useTransform(scrollYProgress, [0, 1], [0, 84]);
  const cardY = useTransform(scrollYProgress, [0, 1], [0, -18]);

  return (
    <header ref={ref} className="relative -mt-[72px] overflow-hidden bg-deep">
      <motion.img
        alt="Golden light over a lush green river valley in Jamaica"
        className="absolute -top-[60px] left-0 right-0 h-[calc(100%+120px)] w-full object-cover opacity-[0.78] [filter:saturate(1.1)_sepia(0.22)_contrast(1.04)]"
        src="https://images.unsplash.com/photo-1552733407-5d5c46c3bb3b?q=80&w=2200&auto=format&fit=crop"
        style={reduce ? undefined : { y: imgY }}
      />
      <div className="absolute inset-0 bg-[linear-gradient(178deg,rgba(92,48,10,0.42)_0%,rgba(140,86,20,0.22)_38%,rgba(6,43,43,0.72)_78%,rgba(6,43,43,0.92)_100%)]" />
      <div className="relative mx-auto flex max-w-[1160px] flex-wrap items-center gap-[clamp(28px,5vw,72px)] px-[clamp(20px,3vw,28px)] pb-[clamp(90px,10vw,130px)] pt-[clamp(150px,16vw,210px)]">
        <div className="flex flex-[1_1_480px] flex-col items-start gap-6">
          <Eyebrow className="text-on-dark-warm">01 · GOLDEN HOUR</Eyebrow>
          <h1 className="m-0 font-display text-[clamp(52px,8.6vw,116px)] font-normal leading-[0.98] tracking-[-0.015em] text-on-dark-heading [text-wrap:balance]">
            <span className="ns-line" style={{ "--d": "0s" } as CSSProperties}>Touch down.</span>
            <br />
            <span className="ns-line" style={{ "--d": "0.15s" } as CSSProperties}>Slow down.</span>
            <br />
            <span className="ns-line" style={{ "--d": "0.3s" } as CSSProperties}>
              Stay <em className="italic text-yellow">golden.</em>
            </span>
          </h1>
          <p className="m-0 max-w-[440px] text-[clamp(15px,1.3vw,17px)] text-on-dark-body">
            Jamaica&apos;s own trusted stays platform — verified hosts, wellness security visits, and doors that open
            like they know you.
          </p>
          <div className="flex flex-wrap gap-3">
            <AppLink
              className="group flex min-h-[50px] items-center gap-2.5 rounded-pill bg-yellow px-7 text-[15px] font-bold text-deep transition-colors hover:bg-yellow-press"
              href="/explore"
            >
              Explore stays{" "}
              <span aria-hidden="true" className="ns-arrow inline-block transition-transform duration-200 group-hover:translate-x-1">
                →
              </span>
            </AppLink>
            <AppLink
              className="flex min-h-[50px] items-center rounded-pill border-[1.5px] border-on-dark-heading/55 px-7 text-[15px] font-semibold text-on-dark-heading transition-colors hover:border-on-dark-heading"
              href="/host-dashboard"
            >
              Become a host
            </AppLink>
          </div>
          <div className="mt-0.5 flex items-center gap-3.5">
            <div className="flex">
              {[
                "https://images.unsplash.com/photo-1494790108377-be9c29b29330?q=80&w=100&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?q=80&w=100&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?q=80&w=100&auto=format&fit=crop",
              ].map((src, i) => (
                <img
                  alt="Traveler portrait"
                  className={cx("block size-9 rounded-full border-2 border-on-dark-heading object-cover", i > 0 && "-ml-2.5")}
                  key={src}
                  src={src}
                />
              ))}
            </div>
            <div className="text-[12.5px] text-on-dark-body">
              <span className="text-yellow">★★★★★</span> &nbsp;Loved by 2,000+ island travelers
            </div>
          </div>
        </div>

        {/* floating featured card */}
        <motion.div className="relative mx-auto flex-[0_1_330px] rotate-[2.5deg]" style={reduce ? undefined : { y: cardY }}>
          <div className="ns-breathe overflow-hidden rounded-[22px] bg-cream shadow-[0_32px_64px_rgba(4,31,31,0.45)]">
            <div className="relative aspect-[4/3]">
              <img
                alt="Warm-lit villa and pool at dusk above the Negril cliffs"
                className="block h-full w-full object-cover"
                src="https://images.unsplash.com/photo-1416331108676-a22ccb276e35?q=80&w=900&auto=format&fit=crop"
              />
              <span className="absolute left-3 top-3 rounded-pill bg-deep/85 px-3 py-1.5 text-[10px] font-bold tracking-[0.18em] text-yellow">
                FEATURED STAY
              </span>
            </div>
            <div className="flex items-end justify-between gap-2.5 p-4 pb-[18px] pl-[18px]">
              <div>
                <div className="font-display text-[19px] font-medium text-ink">Cliffside Retreat</div>
                <div className="mt-0.5 text-[12.5px] text-[#66705F]">Negril · Westmoreland parish</div>
              </div>
              <div className="whitespace-nowrap text-[13px] text-ink">★ 5.0</div>
            </div>
          </div>
          <span className="absolute -top-4 right-[-12px] block">
            <span className="ns-b1 inline-block -rotate-[4deg] rounded-pill bg-deep px-4 py-[9px] text-[11.5px] font-semibold text-on-dark-heading shadow-[0_10px_24px_rgba(4,31,31,0.4)]">
              Dec 12 – 18
            </span>
          </span>
          <span className="absolute -bottom-[26px] left-1 block">
            <span className="ns-b2 inline-block rotate-3 rounded-pill bg-deep-hover px-4 py-[9px] text-[11.5px] font-bold text-white shadow-[0_10px_24px_rgba(4,31,31,0.4)]">
              ✓ Verified host
            </span>
          </span>
        </motion.div>
      </div>
      <div className="relative flex flex-col items-center gap-2 pb-[26px] text-on-dark-nav">
        <span className="text-[10.5px] font-semibold tracking-[0.3em]">SCROLL</span>
        <span className="block h-[34px] w-px bg-gradient-to-b from-on-dark-nav to-transparent" />
      </div>
    </header>
  );
}

function BroadDaylight() {
  return (
    <section className="bg-sand px-[clamp(20px,3vw,28px)] pt-[clamp(72px,9vw,120px)]">
      <div className="mx-auto max-w-[1160px]">
        <Eyebrow className="text-sand-500">02 · BROAD DAYLIGHT</Eyebrow>
        <div className="mb-10 mt-[18px] flex flex-wrap items-end justify-between gap-5">
          <h2 className="m-0 max-w-[640px] font-display text-[clamp(38px,5.6vw,76px)] font-normal leading-[1.02] tracking-[-0.015em] text-ink [text-wrap:balance]">
            Four parishes,
            <br />
            one <em className="italic text-deep-hover">yard</em> at a time.
          </h2>
          <AppLink className="flex min-h-11 items-center gap-2.5 text-sm font-semibold text-deep-hover hover:text-deep" href="/explore">
            All stays{" "}
            <span aria-hidden="true" className="inline-flex size-[34px] items-center justify-center rounded-full bg-deep text-[15px] text-yellow">
              →
            </span>
          </AppLink>
        </div>

        <div className="flex flex-wrap items-stretch gap-[18px]">
          {stays.map((stay, i) => (
            <Reveal
              className={cx(stay.wide ? "flex-[1.7_1_420px]" : "flex-[1_1_260px]")}
              delay={i * 0.1}
              key={stay.name}
            >
              <article className="ns-zoom relative min-h-[430px] h-full overflow-hidden rounded-photo shadow-photo transition-shadow hover:shadow-[0_26px_56px_rgba(96,74,20,0.24)]">
                <img alt={stay.alt} className="ns-zoomimg absolute inset-0 h-full w-full object-cover" src={stay.img} />
                <div className="absolute inset-0 bg-[linear-gradient(185deg,rgba(6,43,43,0)_40%,rgba(6,43,43,0.82)_100%)]" />
                <span className="absolute left-[18px] top-[18px]">{stay.badge}</span>
                <div className="absolute inset-x-0 bottom-0 flex flex-wrap items-end justify-between gap-3.5 p-5 pb-[22px]">
                  <div>
                    <div className={cx("font-display font-medium text-on-dark-heading", stay.wide ? "text-[27px]" : "text-[21px]")}>
                      {stay.name}
                    </div>
                    <div className="mt-0.5 text-[13px] text-on-dark-nav">{stay.location}</div>
                    {!stay.wide && (
                      <div className="mt-2 text-[15.5px] text-on-dark-heading">
                        <strong>{stay.price}</strong> <span className="text-xs text-on-dark-nav">/ night</span>
                      </div>
                    )}
                  </div>
                  <div className="flex items-center gap-3">
                    {stay.wide && (
                      <span className="text-[17px] text-on-dark-heading">
                        <strong>{stay.price}</strong> <span className="text-[12.5px] text-on-dark-nav">/ night</span>
                      </span>
                    )}
                    <AppLink aria-label={`Open ${stay.name}`} className={arrowCircle} href="/explore">
                      →
                    </AppLink>
                  </div>
                </div>
              </article>
            </Reveal>
          ))}
        </div>

        {/* overlapping card into HIGH NOON */}
        <div className="relative z-[5] -mb-[66px] mt-[18px] flex translate-y-[84px] justify-end px-[clamp(0px,2vw,24px)]">
          <article className="flex flex-[0_1_460px] overflow-hidden rounded-[22px] bg-cream shadow-[0_26px_56px_rgba(60,45,10,0.3)]">
            <img
              alt="Misty ridge in the Blue Mountains"
              className="block min-h-[150px] w-[42%] object-cover"
              src="https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?q=80&w=800&auto=format&fit=crop"
            />
            <div className="flex flex-col justify-center gap-1 px-5 py-[18px]">
              <span className="self-start rounded-pill bg-deep px-[11px] py-1 text-[9.5px] font-bold tracking-[0.1em] text-yellow">
                ★ TRUSTED
              </span>
              <div className="mt-1 font-display text-xl font-medium text-ink">Mist Ridge Cabin</div>
              <div className="text-[12.5px] text-[#66705F]">Blue Mountains · St. Andrew</div>
              <div className="mt-1 text-[15px] text-ink">
                <strong>$230</strong> <span className="text-xs text-[#98A08C]">/ night</span>{" "}
                <AppLink className="ml-2 text-[12.5px] font-bold text-deep-hover" href="/explore">
                  View →
                </AppLink>
              </div>
            </div>
          </article>
        </div>
      </div>
    </section>
  );
}

function HighNoon() {
  return (
    <section className="bg-yellow px-[clamp(20px,3vw,28px)] pb-[clamp(88px,10vw,140px)] pt-[clamp(120px,14vw,180px)]">
      <div className="mx-auto flex max-w-[1160px] flex-col gap-6">
        <Eyebrow className="text-deep opacity-75">03 · HIGH NOON</Eyebrow>
        <Reveal>
          <h2 className="m-0 max-w-[980px] font-display text-[clamp(44px,7.4vw,104px)] font-normal leading-none tracking-[-0.02em] text-deep [text-wrap:balance]">
            Trust, said in <em className="italic">broad daylight.</em>
          </h2>
        </Reveal>
        <p className="m-0 max-w-[540px] text-[clamp(15px,1.4vw,18px)] text-deep opacity-85">
          Every host vetted, every visit logged, every message on the record. No fine print, no favors — just the
          island holding its own standard.
        </p>
      </div>
    </section>
  );
}

function SirenIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="32" viewBox="0 0 30 30" width="32">
      <path d="M15 5 L27 25.5 H3 Z" stroke="#ffffff" strokeLinejoin="round" strokeWidth="2" />
      <path d="M15 12.5 V18.5" stroke="#ffffff" strokeLinecap="round" strokeWidth="2" />
      <circle cx="15" cy="22" fill="#ffffff" r="1.3" />
      <path d="M6.5 4.5 Q4 7 4 10.5 M23.5 4.5 Q26 7 26 10.5" opacity="0.7" stroke="#ffffff" strokeLinecap="round" strokeWidth="1.6" />
    </svg>
  );
}

function Dusk() {
  return (
    <section className="relative bg-[linear-gradient(180deg,#062B2B_0%,#052731_62%,#04222B_100%)] px-[clamp(20px,3vw,28px)]">
      {/* doctor birds — thin distant Vs */}
      <svg
        aria-hidden="true"
        className="pointer-events-none absolute left-0 top-6 h-[220px] w-full"
        preserveAspectRatio="xMidYMin slice"
        viewBox="0 0 1440 220"
      >
        <g fill="none" opacity="0.35" stroke="#8FA8A2" strokeLinecap="round" strokeWidth="1.6">
          <path d="M168 64 Q178 54 188 62 Q198 54 208 64" />
          <path d="M318 128 Q325 121 332 127 Q339 121 346 128" />
          <path d="M520 46 Q528 38 536 44 Q544 38 552 46" />
          <path d="M700 96 Q706 90 712 95 Q718 90 724 96" />
          <path d="M1150 70 Q1159 61 1168 68 Q1177 61 1186 70" />
        </g>
      </svg>
      <div
        aria-hidden="true"
        className="ns-duskglow pointer-events-none absolute inset-x-0 bottom-[-120px] h-[260px] bg-[radial-gradient(ellipse_70%_100%_at_50%_100%,rgba(214,110,34,0.28)_0%,rgba(214,110,34,0)_70%)]"
      />
      <div className="relative mx-auto max-w-[1160px]">
        <div className="flex flex-wrap items-start gap-[clamp(24px,4vw,56px)]">
          <div className="flex flex-[1_1_420px] flex-col gap-[18px] pt-[clamp(80px,10vw,130px)]">
            <Eyebrow className="text-on-dark-faint">04 · DUSK</Eyebrow>
            <h2 className="m-0 max-w-[600px] font-display text-[clamp(40px,5.4vw,74px)] font-normal leading-[1.02] tracking-[-0.015em] text-on-dark-heading [text-wrap:balance]">
              While the light goes, someone <em className="italic text-yellow">watches.</em>
            </h2>
            <p className="m-0 max-w-[430px] text-[15px] text-on-dark-muted">
              Vetted security professionals keep watch through the night — every visit scheduled, logged and
              accountable on the platform.
            </p>
          </div>
          {/* 119 card — overlaps the HIGH NOON boundary above */}
          <div className="relative z-[5] ml-auto flex flex-[0_1_400px] flex-col gap-[18px] rounded-[22px] border border-white/15 bg-night p-[30px] py-[34px] shadow-deep max-[960px]:mt-8 min-[961px]:-mt-[150px]">
            <span className="inline-flex size-[60px] shrink-0 items-center justify-center rounded-[18px] bg-emergency">
              <SirenIcon />
            </span>
            <div>
              <div className="flex items-baseline gap-2.5 whitespace-nowrap font-display text-[19px] font-medium leading-[1.1] text-on-dark-heading">
                Jamaica Emergency: <span className="text-[52px] font-semibold tracking-[0.01em] text-yellow">119</span>
              </div>
              <div className="mt-2 text-xs font-semibold tracking-[0.16em] text-on-dark-warm">
                POLICE · FIRE · AMBULANCE — ISLAND-WIDE
              </div>
            </div>
            <span className="self-start rounded-pill bg-emergency px-3.5 py-[7px] text-xs font-bold tracking-[0.04em] text-white">
              Shown on every property page
            </span>
            <p className="m-0 text-[13.5px] text-on-dark-warm opacity-90">
              Under the header, above the gallery, above the fold — on every Jamaican listing, without exception.
              Never buried in a footer.
            </p>
          </div>
        </div>
        <div className="mt-5 flex flex-wrap justify-end gap-4">
          {[
            ["Badge ID only", "Security professionals are identified by platform badge ID — never by name, photo or direct contact."],
            ["InsuraGuest protection", "Participating properties carry InsuraGuest coverage for the whole stay — look for the badge on the listing."],
          ].map(([title, copy], i) => (
            <Reveal className="min-w-[250px] flex-[0_1_400px]" delay={i * 0.1} key={title}>
              <div className="flex h-full flex-col gap-2 rounded-[18px] border border-on-dark-faint/35 px-6 py-5">
                <div className="font-display text-[19px] font-medium text-on-dark-heading">{title}</div>
                <p className="m-0 text-[13px] text-on-dark-muted">{copy}</p>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
      {/* dusk horizon: hills + palm silhouettes over a residual ember line */}
      <svg
        aria-hidden="true"
        className="mx-[calc(-1*clamp(20px,3vw,28px))] mt-16 block h-[120px] w-[calc(100%+2*clamp(20px,3vw,28px))]"
        preserveAspectRatio="none"
        viewBox="0 0 1440 130"
      >
        <rect fill="#D66E22" height="8" opacity="0.12" width="1440" x="0" y="84" />
        <rect fill="#D66E22" height="3" opacity="0.22" width="1440" x="0" y="89" />
        <path d="M0 130 L0 104 Q180 78 380 96 Q560 110 760 92 Q980 72 1180 94 Q1320 106 1440 98 L1440 130 Z" fill="#041F1F" />
        <g fill="#041F1F">
          <path d="M292 96 C290 76 286 62 278 52 C288 58 296 70 299 84 C303 70 311 60 322 55 C312 66 306 80 305 96 Z" />
          <path d="M300 52 C292 44 280 40 268 42 C280 46 290 52 296 60 Z M304 52 C312 42 324 38 336 40 C324 45 314 52 308 60 Z" />
          <path d="M1088 98 C1086 80 1082 68 1075 59 C1084 64 1091 75 1094 87 C1097 75 1104 66 1113 62 C1105 71 1100 83 1099 98 Z" />
          <path d="M1096 59 C1089 52 1078 48 1067 50 C1078 53 1087 59 1092 66 Z M1099 59 C1106 51 1117 47 1128 49 C1117 53 1108 59 1103 66 Z" />
        </g>
      </svg>
    </section>
  );
}

function firefly(cx: number, cy: number, r: number, dur: string, delay: string) {
  return (
    <g className="ns-ff" style={{ "--ffd": dur, "--ffdel": delay } as CSSProperties}>
      <circle cx={cx} cy={cy} fill="#FFD21F" opacity="0.16" r={r + 4} />
      <circle cx={cx} cy={cy} fill="#FFD21F" r={r} />
    </g>
  );
}

function NightScene() {
  return (
    <svg
      aria-label="Night scene of the NestyStay logo — a hammock between two coconut palms, fireflies glowing"
      className="block h-auto w-full max-w-[530px]"
      role="img"
      viewBox="0 0 560 470"
    >
      <defs>
        <radialGradient cx="50%" cy="62%" id="nsGlow" r="55%">
          <stop offset="0%" stopColor="#FFD21F" stopOpacity="0.15" />
          <stop offset="55%" stopColor="#FFD21F" stopOpacity="0.05" />
          <stop offset="100%" stopColor="#FFD21F" stopOpacity="0" />
        </radialGradient>
      </defs>
      <ellipse cx="280" cy="300" fill="url(#nsGlow)" rx="262" ry="215" />
      <g>
        <circle cx="470" cy="64" fill="#F3EAC8" opacity="0.85" r="22" />
        <circle cx="479" cy="58" fill="#041F1F" r="19" />
      </g>
      <ellipse cx="280" cy="424" fill="#0B332C" rx="238" ry="34" />
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
      <circle cx="150" cy="196" fill="#FFD21F" r="9" />
      <circle cx="164" cy="202" fill="#E8BD12" r="8" />
      <circle cx="140" cy="206" fill="#E8BD12" r="8" />
      <circle cx="154" cy="210" fill="#FFD21F" r="7.5" />
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
      <circle cx="410" cy="196" fill="#FFD21F" r="9" />
      <circle cx="396" cy="202" fill="#E8BD12" r="8" />
      <circle cx="420" cy="206" fill="#E8BD12" r="8" />
      <circle cx="406" cy="210" fill="#FFD21F" r="7.5" />
      {/* hammock sways ±2.5° about the attachment line */}
      <g className="ns-sway">
        <path d="M158 296 L196 318 M402 296 L364 318" stroke="#0C332C" strokeLinecap="round" strokeWidth="5" />
        <path d="M196 318 C240 366 320 366 364 318 C330 396 230 396 196 318 Z" fill="#FFD21F" />
        <path d="M204 326 C244 364 316 364 356 326 C322 380 238 380 204 326 Z" fill="#0E4A45" />
        {/* hammock rests empty — the yard is waiting */}
      </g>
      {firefly(98, 300, 2, "3.2s", "0s")}
      {firefly(210, 238, 1.8, "4.1s", "0.7s")}
      {firefly(352, 252, 1.8, "5.3s", "1.4s")}
      {firefly(470, 316, 2, "3.8s", "2.1s")}
      {firefly(260, 398, 1.6, "6s", "0.4s")}
      {firefly(430, 380, 1.6, "4.6s", "1.9s")}
    </svg>
  );
}

function Nightfall() {
  return (
    <section
      className="ns-night bg-night-2 pt-[clamp(84px,10vw,140px)] [background-image:radial-gradient(circle_1.1px_at_12%_18%,rgba(255,255,255,0.55)_0_1px,transparent_2px),radial-gradient(circle_0.9px_at_34%_8%,rgba(255,255,255,0.4)_0_1px,transparent_2px),radial-gradient(circle_1px_at_58%_22%,rgba(255,255,255,0.5)_0_1px,transparent_2px),radial-gradient(circle_0.8px_at_79%_12%,rgba(255,255,255,0.4)_0_1px,transparent_2px),radial-gradient(circle_1.2px_at_91%_28%,rgba(255,255,255,0.45)_0_1px,transparent_2px)] [background-size:900px_700px]"
      id="ns-night"
    >
      <div className="mx-auto flex max-w-[1160px] flex-wrap items-center gap-[clamp(28px,5vw,72px)] px-[clamp(20px,3vw,28px)]">
        <div className="flex min-w-[300px] flex-[1_1_420px] justify-center">
          <NightScene />
        </div>
        <div className="flex flex-[1_1_380px] flex-col items-start gap-6 pb-6">
          <Eyebrow className="text-on-dark-faint">05 · NIGHTFALL</Eyebrow>
          <h2 className="m-0 font-display text-[clamp(42px,6vw,84px)] font-normal leading-none tracking-[-0.015em] text-on-dark-heading [text-wrap:balance]">
            Every day ends
            <br />
            at your <em className="italic text-yellow">yard.</em>
          </h2>
          <p className="m-0 max-w-[420px] text-[clamp(15px,1.3vw,17px)] text-on-dark-muted">
            Hammock hung, coconuts heavy, night warm. Your spot is waiting.
          </p>
          <AppLink
            className="group flex min-h-[50px] items-center gap-2.5 rounded-pill bg-yellow px-[30px] text-[15px] font-bold text-deep transition-colors hover:bg-yellow-press"
            href="/explore"
          >
            Explore Stays{" "}
            <span aria-hidden="true" className="ns-arrow inline-block transition-transform duration-200 group-hover:translate-x-1">
              →
            </span>
          </AppLink>
        </div>
      </div>
      <div className="mt-16">
        <PublicFooter variant="night" />
      </div>
    </section>
  );
}

export function PublicLanding() {
  return (
    <div className="overflow-x-clip font-sans text-[15px] leading-[1.6] text-ink">
      <GoldenHourHero />
      <BroadDaylight />
      <HighNoon />
      <Dusk />
      <Nightfall />
    </div>
  );
}
