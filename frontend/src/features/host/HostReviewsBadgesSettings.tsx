import { useEffect, useMemo, useState } from "react";
import { Star } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { StatusChip } from "../../components/ui/StatusChip";
import {
  api,
  type BadgeAssignment,
  type BadgeDefinition,
  type BadgeFeatureAccess,
  type BadgeLevel,
  type BadgeRenewal,
} from "../../lib/api";

interface HostReviewsBadgesSettingsProps {
  view: string;
  token: string;
  hostUserId: string;
}

const badgeOrder: BadgeLevel[] = ["Free", "Verified", "Trusted", "Wellness"];

export function HostReviewsBadgesSettings({ view, token, hostUserId }: HostReviewsBadgesSettingsProps) {
  const [replies, setReplies] = useState<Record<string, string>>({
    "rev-1": "Thank you for staying at Ocho Rios Verified Villa!",
  });
  const [replyInput, setReplyInput] = useState("");
  const [definitions, setDefinitions] = useState<BadgeDefinition[]>([]);
  const [assignments, setAssignments] = useState<BadgeAssignment[]>([]);
  const [renewals, setRenewals] = useState<BadgeRenewal[]>([]);
  const [featureAccess, setFeatureAccess] = useState<BadgeFeatureAccess | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    if (view !== "badges" && view !== "settings") return;
    if (!token || !hostUserId) {
      setLoadError("A signed-in host session is required to load badge access.");
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setLoadError(null);
    void Promise.all([
      api.getBadgeDefinitions(),
      api.getBadgeAssignments(token, "Host", hostUserId),
      api.getBadgeFeatureAccess("Host", hostUserId, token),
    ])
      .then(async ([nextDefinitions, nextAssignments, nextFeatureAccess]) => {
        const renewalGroups = await Promise.all(
          nextAssignments.map((assignment) => api.getBadgeRenewals(token, assignment.id)),
        );
        if (cancelled) return;
        setDefinitions(nextDefinitions);
        setAssignments(nextAssignments);
        setFeatureAccess(nextFeatureAccess);
        setRenewals(renewalGroups.flat());
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setLoadError(error instanceof Error ? error.message : "Badge access could not be loaded.");
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [hostUserId, token, view]);

  const activeAssignment = useMemo(() => {
    if (!featureAccess) return undefined;
    return assignments.find(
      (assignment) => assignment.status.toLowerCase() === "active" && assignment.level === featureAccess.activeLevel,
    );
  }, [assignments, featureAccess]);
  const pendingRenewal = renewals.find(
    (renewal) => renewal.badgeAssignmentId === activeAssignment?.id && renewal.paymentStatus.toLowerCase() === "pending",
  );

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
              {[0, 1, 2, 3, 4].map((i) => <Star fill="currentColor" key={i} size={15} />)}
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
                onChange={(event) => setReplyInput(event.target.value)}
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

  const activeLevel = featureAccess?.activeLevel ?? "Free";
  const currentLevelIndex = badgeOrder.indexOf(activeLevel);

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="host-13-page" id="HOST-BADGE">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Your <em className="italic text-deep-hover">badge</em>
      </h1>

      {isLoading && <div className="rounded-card border border-sand-border bg-cream p-5">Loading your badge access…</div>}
      {loadError && <div className="rounded-card border border-coral bg-coral-tint p-5 text-coral-text" role="alert">{loadError}</div>}

      {!isLoading && !loadError && featureAccess && (
        <>
          <div className="flex flex-wrap items-center justify-between gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
            <div className="flex flex-wrap items-center gap-3.5">
              <span className="inline-flex items-center gap-1.5 rounded-pill bg-success-tint px-[18px] py-2 text-xs font-bold tracking-[0.1em] text-success-text">
                ✓ {activeLevel.toUpperCase()} HOST
              </span>
              <span className="text-[13px] text-gray-600">
                {activeAssignment ? `earned ${new Date(activeAssignment.earnedAt).toLocaleDateString()}` : "base access"}
              </span>
            </div>
            <StatusChip
              value={pendingRenewal
                ? `Renewal reminder ${new Date(pendingRenewal.reminderDueAt).toLocaleDateString()}`
                : activeAssignment ? `Paid through ${new Date(activeAssignment.paidThrough).toLocaleDateString()}` : "No renewal required"}
            />
          </div>

          <div className="grid grid-cols-[repeat(auto-fit,minmax(280px,1fr))] gap-4">
            {definitions.map((definition) => {
              const levelIndex = badgeOrder.indexOf(definition.level);
              const isCurrent = definition.level === activeLevel;
              const isUnlocked = levelIndex <= currentLevelIndex;
              return (
                <div className={`flex flex-col gap-3 rounded-card p-6 ${definition.level === "Trusted" ? "bg-deep text-on-dark-heading" : "border border-sand-border bg-cream"}`} key={definition.id}>
                  <span className={`self-start rounded-pill px-3.5 py-1.5 text-[11px] font-bold tracking-[0.1em] ${definition.level === "Trusted" ? "bg-yellow/15 text-yellow" : "border border-mint bg-mint-tint text-mint-text"}`}>
                    {definition.level.toUpperCase()} HOST
                  </span>
                  <div className="font-display text-[22px] font-medium">
                    {definition.annualPrice === 0
                      ? "Included"
                      : `${new Intl.NumberFormat("en-US", { style: "currency", currency: definition.currency }).format(definition.annualPrice)}${definition.priceCadence ? ` · ${definition.priceCadence}` : "/year"}`}
                  </div>
                  <div className={`text-[13px] ${definition.level === "Trusted" ? "text-on-dark-muted" : "text-gray-600"}`}>
                    {definition.unlocks.join(" · ")}
                  </div>
                  <StatusChip value={isCurrent ? "Current" : isUnlocked ? "Unlocked" : "Admin verification required"} />
                </div>
              );
            })}
          </div>

          <div className="grid grid-cols-[repeat(auto-fit,minmax(260px,1fr))] gap-3 rounded-card border border-sand-border bg-cream p-5 text-[13px]">
            <div><strong>Unlocked</strong><div className="mt-1 text-gray-600">{featureAccess.unlockedFeatures.join(" · ") || "None"}</div></div>
            <div><strong>Locked</strong><div className="mt-1 text-gray-600">{featureAccess.lockedFeatures.join(" · ") || "None"}</div></div>
          </div>
        </>
      )}

      <AppLink
        className="inline-flex min-h-[46px] items-center gap-2 self-start rounded-pill bg-deep px-[22px] text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
        href="/host/wellness"
      >
        Review wellness requirements
      </AppLink>
      <div className="text-[12.5px] text-sand-500">Badge eligibility and payment confirmation are verified by an authorized administrator; this page never accepts self-declared approval.</div>
    </div>
  );
}
