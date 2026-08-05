import { useState } from "react";
import { Star } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { StatusChip } from "../../components/ui/StatusChip";

interface HostReviewsBadgesSettingsProps {
  view: string;
  token: string;
}

/* HOST-BADGE (DS v2) — badge status + upsell cards. The four tiers use the exact
   DS v2 badge styles; the Trusted price copy is locked by the client contract:
   "$120/year or $12/month". Eligibility reasons render verbatim (spec data until
   the badges API lands). */
export function HostReviewsBadgesSettings({ view, token: _token }: HostReviewsBadgesSettingsProps) {
  const [replies, setReplies] = useState<Record<string, string>>({
    "rev-1": "Thank you for staying at Ocho Rios Verified Villa!",
  });
  const [replyInput, setReplyInput] = useState("");

  if (view === "reviews") {
    return (
      <div className="flex flex-col gap-4 font-sans text-ink" data-testid="host-12-page" id="HOST-12">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Guest <em className="italic text-deep-hover">reviews</em>
        </h1>
        <div className="flex flex-col gap-2.5 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex flex-wrap items-center justify-between gap-2.5">
            <div className="font-display text-[17px] font-medium">Ocho Rios Verified Villa</div>
            <span className="flex gap-0.5 text-amber">
              {[0, 1, 2, 3, 4].map((i) => (
                <Star fill="currentColor" key={i} size={15} />
              ))}
            </span>
          </div>
          <p className="m-0 text-[13.5px] text-gray-600">&quot;Amazing stay, clean, beautiful view!&quot; — Traveler Guest</p>
          {replies["rev-1"] ? (
            <div className="rounded-field border-l-4 border-deep-hover bg-shell px-4 py-3 text-[13px]">
              <strong>Host reply:</strong> <span className="text-gray-600">{replies["rev-1"]}</span>
            </div>
          ) : (
            <div className="flex flex-wrap gap-2.5">
              <input
                className="min-h-12 flex-[1_1_240px] rounded-field border-[1.5px] border-sand-input bg-white px-4 font-sans text-[14.5px] text-ink outline-none focus:border-deep-hover"
                onChange={(e) => setReplyInput(e.target.value)}
                placeholder="Write reply to guest…"
                type="text"
                value={replyInput}
              />
              <button
                className="inline-flex min-h-12 cursor-pointer items-center rounded-pill border-none bg-deep px-5 font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
                onClick={() => setReplies({ ...replies, "rev-1": replyInput })}
                type="button"
              >
                Reply
              </button>
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="host-13-page" id="HOST-BADGE">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Your <em className="italic text-deep-hover">badge</em>
      </h1>

      {/* Current tier */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex flex-wrap items-center gap-3.5">
          <span className="inline-flex items-center gap-1.5 rounded-pill bg-success-tint px-[18px] py-2 text-xs font-bold tracking-[0.1em] text-success-text">
            ✓ VERIFIED HOST
          </span>
          <span className="text-[13px] text-gray-600">since March 2026</span>
        </div>
        <StatusChip value="Renews in 30 days — Jan 29" />
      </div>

      <div className="grid grid-cols-[repeat(auto-fit,minmax(280px,1fr))] gap-4">
        {/* Trusted upsell — Deep card, locked price copy */}
        <div className="flex flex-col gap-3 rounded-card bg-deep p-6">
          <span className="self-start rounded-pill bg-yellow/15 px-3.5 py-1.5 text-[11px] font-bold tracking-[0.1em] text-yellow">
            ★ TRUSTED HOST
          </span>
          <div className="font-display text-2xl font-medium text-on-dark-heading">$120/year or $12/month</div>
          <div className="text-[13px] text-on-dark-muted">Featured placement, Trusted badge on every card, priority support.</div>
          <div className="flex flex-col gap-1.5 text-[12.5px]">
            <span className="text-[#8FE0B3]">✓ Identity verified</span>
            <span className="text-[#8FE0B3]">✓ 5+ completed bookings</span>
            <span className="text-[#F0A9A3]">✗ &quot;Host must maintain a 4.8+ rating for 90 days&quot; — 22 days remaining</span>
          </div>
          <button
            className="mt-1 inline-flex min-h-12 cursor-pointer items-center justify-center gap-2 rounded-pill border-none bg-yellow px-5 font-sans text-sm font-bold text-deep transition-colors hover:bg-yellow-press"
            type="button"
          >
            Upgrade when eligible
          </button>
        </div>

        {/* Wellness upsell — mint */}
        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-6">
          <span className="self-start rounded-pill border border-mint bg-mint-tint px-3.5 py-1.5 text-[11px] font-bold tracking-[0.1em] text-mint-text">
            ◆ WELLNESS HOST
          </span>
          <div className="font-display text-[22px] font-medium">Unlock wellness visits</div>
          <div className="text-[13px] text-gray-600">Off-duty officer visits, police directory access, the mint badge travelers trust.</div>
          <div className="flex flex-col gap-1.5 text-[12.5px]">
            <span className="text-success-text">✓ Verified badge held</span>
            <span className="text-coral-text">✗ &quot;Property must have InsuraGuest coverage enabled&quot; — enable in property settings</span>
          </div>
          <AppLink
            className="inline-flex min-h-[46px] items-center gap-2 self-start rounded-pill bg-deep px-[22px] text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
            href="/host/wellness"
          >
            Review requirements
          </AppLink>
        </div>
      </div>

      <div className="text-[12.5px] text-sand-500">Eligibility reasons render verbatim from the API — never paraphrased.</div>
    </div>
  );
}
