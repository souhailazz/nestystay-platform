import { Emergency119Badge } from "../../design/Emergency119Badge";

/**
 * 04 · DUSK — security section. The 119 card overlaps the yellow/green
 * boundary above; doctor-bird silhouettes and an ember horizon ease the
 * scene into nightfall.
 */
export function DuskSecurity() {
  return (
    <section className="ns-dusk">
      {/* doctor birds — thin distant Vs in the dusk sky */}
      <svg
        className="ns-dusk__birds"
        viewBox="0 0 1440 220"
        aria-hidden="true"
        data-depth="0.15"
        preserveAspectRatio="xMidYMin slice"
      >
        <g stroke="#8FA8A2" strokeWidth="1.6" strokeLinecap="round" fill="none" opacity="0.35">
          <path d="M168 64 Q178 54 188 62 Q198 54 208 64" />
          <path d="M318 128 Q325 121 332 127 Q339 121 346 128" />
          <path d="M520 46 Q528 38 536 44 Q544 38 552 46" />
          <path d="M700 96 Q706 90 712 95 Q718 90 724 96" />
          <path d="M1150 70 Q1159 61 1168 68 Q1177 61 1186 70" />
        </g>
      </svg>
      {/* anim: horizon ember glow, opacity 0.7–1, 8s ease-in-out alternate infinite */}
      <div className="ns-dusk__glow ns-duskglow" aria-hidden="true" />
      <div className="ns-dusk__inner">
        <div className="ns-dusk__cols">
          <div className="ns-dusk__copy">
            <div className="ns-eyebrow">04 · DUSK</div>
            <h2 className="ns-display ns-dusk__title" data-depth="0.92">
              While the light goes, someone <em>watches.</em>
            </h2>
            <p className="ns-dusk__lede">
              Vetted security professionals keep watch through the night — every visit scheduled, logged and
              accountable on the platform.
            </p>
          </div>
          {/* 119 card — overlaps the HIGH NOON boundary above */}
          <div className="ns-dusk__card" data-depth="0.88">
            <Emergency119Badge
              variant="card"
              chip="Shown on every property page"
              note="Under the header, above the gallery, above the fold — on every Jamaican listing, without exception. Never buried in a footer."
            />
          </div>
        </div>
        <div className="ns-dusk__tiles">
          {/* anim: reveal on viewport entry, stagger 100ms */}
          <div className="ns-dusk__tile ns-rv" style={{ "--d": "0s" } as React.CSSProperties}>
            <div className="ns-dusk__tiletitle">Badge ID only</div>
            <p>Security professionals are identified by platform badge ID — never by name, photo or direct contact.</p>
          </div>
          <div className="ns-dusk__tile ns-rv" style={{ "--d": "0.1s" } as React.CSSProperties}>
            <div className="ns-dusk__tiletitle">InsuraGuest protection</div>
            <p>Participating properties carry InsuraGuest coverage for the whole stay — look for the badge on the listing.</p>
          </div>
        </div>
      </div>
      {/* dusk horizon: hill + palm silhouettes over a residual ember line */}
      <svg className="ns-dusk__horizon" viewBox="0 0 1440 130" preserveAspectRatio="none" aria-hidden="true" data-depth="0.6">
        <rect x="0" y="84" width="1440" height="8" fill="#D66E22" opacity="0.12" />
        <rect x="0" y="89" width="1440" height="3" fill="#D66E22" opacity="0.22" />
        <path
          d="M0 130 L0 104 Q180 78 380 96 Q560 110 760 92 Q980 72 1180 94 Q1320 106 1440 98 L1440 130 Z"
          fill="#041F1F"
        />
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
