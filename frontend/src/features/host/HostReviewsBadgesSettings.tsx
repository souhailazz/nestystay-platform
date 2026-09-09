import { useEffect, useMemo, useState } from "react";
import { ArrowUpRight, Check, CircleAlert, CreditCard, LockKeyhole, RefreshCcw, Star } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { StatusChip } from "../../components/ui/StatusChip";
import {
  api,
  formatMoney,
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

const badgeDescriptions: Record<BadgeLevel, string> = {
  Free: "A strong starting point for listing, calendar, messaging, QR access, and standard payouts.",
  Verified: "Identity-reviewed host status with safer guest verification and trusted local directories.",
  Trusted: "A proven host tier for hosts with an active Verified badge and an established booking history.",
  Wellness: "The complete safety tier with wellness visits, police directory access, and property patrol options.",
};

const requirements: Record<BadgeLevel, string[]> = {
  Free: ["Create your host account", "Add a property listing", "Keep your contact details current"],
  Verified: ["Complete host eKYC review", "Keep a valid identity review on file"],
  Trusted: ["Active Verified badge", "At least 3 approved bookings (NestyStay verified)"],
  Wellness: ["Active Verified badge", "Property address on an approved listing", "Wellness subscription enabled"],
};

function dateLabel(value?: string | null) {
  if (!value) return "Not available";
  return new Date(value).toLocaleDateString(undefined, { dateStyle: "medium" });
}

function daysUntil(value?: string | null) {
  if (!value) return Number.POSITIVE_INFINITY;
  return Math.ceil((new Date(value).getTime() - Date.now()) / 86_400_000);
}

function activeAssignmentFor(assignments: BadgeAssignment[], level: BadgeLevel) {
  const now = Date.now();
  return assignments.find((assignment) =>
    assignment.level === level &&
    assignment.status.toLowerCase() === "active" &&
    assignment.paymentStatus.toLowerCase() === "captured" &&
    new Date(assignment.expiresAt).getTime() > now,
  );
}

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
  const [isRenewing, setIsRenewing] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [autoRenew, setAutoRenew] = useState(false);

  useEffect(() => {
    if (view !== "badges" && view !== "settings") return;
    // Cookie-authenticated browser sessions intentionally keep the bearer
    // token empty in JavaScript. The API client still sends credentials, so
    // the user id is the only client-side readiness check required here.
    if (!hostUserId) {
      setLoadError("A signed-in host session is required to load badge access.");
      return;
    }

    const storedPreference = window.localStorage.getItem(`nestyStay.badge.autoRenew.${hostUserId}`);
    setAutoRenew(storedPreference === "true");

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
  const renewalDays = daysUntil(pendingRenewal?.reminderDueAt ?? activeAssignment?.expiresAt);
  const renewalAttention = Boolean(activeAssignment && renewalDays <= 30);
  const activeLevel = featureAccess?.activeLevel ?? "Free";
  const currentLevelIndex = badgeOrder.indexOf(activeLevel);
  const hasVerified = currentLevelIndex >= badgeOrder.indexOf("Verified");
  const nextUpgrade = definitions.find((definition) => badgeOrder.indexOf(definition.level) > currentLevelIndex);

  function checklistState(level: BadgeLevel, requirementIndex: number): "complete" | "pending" | "missing" {
    if (level === "Free") return "complete";
    if (activeAssignmentFor(assignments, level)) return "complete";
    if (level === "Verified") return requirementIndex === 0 && !hasVerified ? "pending" : "missing";
    if (level === "Trusted") {
      if (requirementIndex === 0) return hasVerified ? "complete" : "missing";
      return "pending";
    }
    if (requirementIndex === 0) return hasVerified ? "complete" : "missing";
    return "pending";
  }

  function toggleAutoRenew(enabled: boolean) {
    setAutoRenew(enabled);
    window.localStorage.setItem(`nestyStay.badge.autoRenew.${hostUserId}`, String(enabled));
    setNotice(enabled ? "Auto-renew preference saved for this browser." : "Auto-renew preference turned off.");
  }

  async function retryRenewal() {
    if (!activeAssignment) return;
    setIsRenewing(true);
    setActionError(null);
    setNotice(null);
    try {
      const updated = await api.payBadgeRenewal(activeAssignment.id, token);
      setAssignments((current) => current.map((item) => item.id === updated.id ? updated : item));
      const nextRenewals = await api.getBadgeRenewals(token, updated.id);
      setRenewals((current) => [...current.filter((item) => item.badgeAssignmentId !== updated.id), ...nextRenewals]);
      setNotice(`Renewal captured. Your ${updated.level} badge is paid through ${dateLabel(updated.paidThrough)}.`);
    } catch (error: unknown) {
      setActionError(error instanceof Error ? error.message : "Renewal payment could not be completed.");
    } finally {
      setIsRenewing(false);
    }
  }

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

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="host-13-page" id="HOST-BADGE">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="mb-1 text-[11px] font-bold uppercase tracking-[0.16em] text-sand-500">HOST-13 · Badge centre</p>
          <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
            Your <em className="italic text-deep-hover">badge plan</em>
          </h1>
          <p className="mb-0 mt-2 max-w-2xl text-[14px] leading-6 text-gray-600">
            See exactly what is active, what each upgrade unlocks, and which requirements NestyStay still needs to verify.
          </p>
        </div>
        <AppLink className="inline-flex min-h-[44px] items-center gap-2 rounded-pill border border-sand-input bg-cream px-4 text-[13px] font-semibold text-ink transition-colors hover:bg-shell" href="/host-dashboard">
          Back to dashboard <ArrowUpRight size={16} />
        </AppLink>
      </div>

      {isLoading && (
        <div className="grid gap-3 md:grid-cols-3" aria-label="Loading badge access" aria-busy="true">
          {[1, 2, 3].map((item) => <div className="h-32 animate-pulse rounded-card border border-sand-border bg-shell" key={item} />)}
        </div>
      )}
      {loadError && <div className="rounded-card border border-coral bg-coral-tint p-5 text-coral-text" role="alert">{loadError}</div>}
      {actionError && <div className="rounded-card border border-coral bg-coral-tint p-5 text-coral-text" role="alert">{actionError}</div>}
      {notice && <div className="rounded-card border border-mint bg-mint-tint p-4 text-[13px] text-mint-text" role="status">{notice}</div>}

      {!isLoading && !loadError && featureAccess && (
        <>
          <section className="grid gap-4 lg:grid-cols-[1.35fr_1fr]" aria-label="Current badge status">
            <div className="rounded-card bg-deep p-6 text-on-dark-heading">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="mb-2 text-[11px] font-bold uppercase tracking-[0.16em] text-on-dark-faint">Current access</p>
                  <div className="flex flex-wrap items-center gap-3">
                    <span className="inline-flex items-center gap-1.5 rounded-pill bg-success-tint px-[18px] py-2 text-xs font-bold tracking-[0.1em] text-success-text">
                      <Check size={14} /> {activeLevel.toUpperCase()} HOST
                    </span>
                    <StatusChip value={activeAssignment?.status ?? "Included"} />
                  </div>
                  <p className="mb-0 mt-4 max-w-xl text-[13px] leading-6 text-on-dark-muted">
                    {activeAssignment ? `Earned ${dateLabel(activeAssignment.earnedAt)} · paid through ${dateLabel(activeAssignment.paidThrough)}` : "Free access is included with your NestyStay host account."}
                  </p>
                </div>
                <div className="rounded-field bg-white/10 px-4 py-3 text-right">
                  <p className="m-0 text-[11px] uppercase tracking-[0.12em] text-on-dark-faint">Feature access</p>
                  <strong className="mt-1 block font-display text-2xl">{featureAccess.unlockedFeatures.length}</strong>
                  <span className="text-[12px] text-on-dark-muted">unlocked now</span>
                </div>
              </div>
            </div>
            <div className={`rounded-card border p-5 ${renewalAttention ? "border-amber bg-amber-tint" : "border-sand-border bg-cream"}`}>
              <div className="flex items-start gap-3">
                {renewalAttention ? <CircleAlert className="mt-0.5 text-amber-text" size={20} /> : <RefreshCcw className="mt-0.5 text-mint-text" size={20} />}
                <div className="min-w-0">
                  <h2 className="m-0 font-display text-[19px] font-medium">Renewal & payment</h2>
                  {pendingRenewal ? (
                    <p className="mb-3 mt-2 text-[13px] leading-5 text-gray-600">Reminder due {dateLabel(pendingRenewal.reminderDueAt)} · {formatMoney(pendingRenewal.amountDue, pendingRenewal.currency)}. Renew before expiry to keep gated directory access. If a payment becomes overdue, access may be paused until the retry succeeds.</p>
                  ) : activeAssignment ? (
                    <p className="mb-3 mt-2 text-[13px] leading-5 text-gray-600">No pending renewal. Your badge is paid through {dateLabel(activeAssignment.paidThrough)}.</p>
                  ) : (
                    <p className="mb-3 mt-2 text-[13px] leading-5 text-gray-600">Free badge access does not require renewal.</p>
                  )}
                  {activeAssignment && pendingRenewal && (
                    <div className="flex flex-wrap items-center gap-3">
                      <button className="inline-flex min-h-[42px] items-center gap-2 rounded-pill bg-deep px-4 text-[13px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:cursor-not-allowed disabled:opacity-60" disabled={isRenewing} onClick={() => void retryRenewal()} type="button">
                        <CreditCard size={16} /> {isRenewing ? "Retrying…" : "Pay / retry renewal"}
                      </button>
                      <label className="inline-flex items-center gap-2 text-[12px] font-semibold text-gray-700">
                        <input checked={autoRenew} onChange={(event) => toggleAutoRenew(event.target.checked)} type="checkbox" />
                        Auto-renew preference
                      </label>
                    </div>
                  )}
                  {activeAssignment && !pendingRenewal && (
                    <label className="inline-flex items-center gap-2 text-[12px] font-semibold text-gray-700">
                      <input checked={autoRenew} onChange={(event) => toggleAutoRenew(event.target.checked)} type="checkbox" />
                      Auto-renew preference
                    </label>
                  )}
                  <p className="mb-0 mt-3 text-[11px] text-gray-500">Payment status and expiry dates always come from the NestyStay API.</p>
                </div>
              </div>
            </div>
          </section>

          <section aria-labelledby="badge-comparison-heading">
            <div className="mb-3 flex flex-wrap items-end justify-between gap-2">
              <div>
                <p className="mb-1 text-[11px] font-bold uppercase tracking-[0.16em] text-sand-500">Upgrade comparison</p>
                <h2 className="m-0 font-display text-[24px] font-medium" id="badge-comparison-heading">Choose the access you need</h2>
              </div>
              <span className="text-[12px] text-gray-500">Prices are read from the active admin pricebook; final amounts are server-authoritative.</span>
            </div>
            <div className="grid gap-4 xl:grid-cols-4">
              {definitions.map((definition) => {
                const levelIndex = badgeOrder.indexOf(definition.level);
                const isCurrent = definition.level === activeLevel;
                const isUnlocked = levelIndex <= currentLevelIndex;
                const isAvailableUpgrade = levelIndex > currentLevelIndex;
                const assignment = activeAssignmentFor(assignments, definition.level);
                return (
                  <article className={`flex flex-col gap-3 rounded-card p-5 ${definition.level === "Trusted" ? "bg-deep text-on-dark-heading" : "border border-sand-border bg-cream"}`} key={definition.id}>
                    <div className="flex items-center justify-between gap-2">
                      <span className={`rounded-pill px-3 py-1.5 text-[10px] font-bold tracking-[0.1em] ${definition.level === "Trusted" ? "bg-yellow/15 text-yellow" : "border border-mint bg-mint-tint text-mint-text"}`}>{definition.level.toUpperCase()}</span>
                      <StatusChip value={isCurrent ? "Current" : isUnlocked ? "Included" : "Upgrade"} />
                    </div>
                    <div>
                      <h3 className="m-0 font-display text-[22px] font-medium">{definition.level} badge</h3>
                      <p className={`mb-0 mt-2 text-[12.5px] leading-5 ${definition.level === "Trusted" ? "text-on-dark-muted" : "text-gray-600"}`}>{badgeDescriptions[definition.level]}</p>
                      {definition.level === "Verified" && <p className="mb-0 mt-2 text-[11px] leading-5 text-gray-500">Review target: usually 1–2 business days · documents: government ID and a clear selfie.</p>}
                      {definition.level === "Trusted" && <p className={`mb-0 mt-2 text-[11px] leading-5 ${definition.level === "Trusted" ? "text-on-dark-muted" : "text-gray-500"}`}>Trust score is based on approved booking history and account standing; it is never self-declared.</p>}
                    </div>
                    <div className="font-display text-[20px] font-medium">
                      {definition.annualPrice === 0 ? "Included" : `${formatMoney(definition.annualPrice, definition.currency)}${definition.priceCadence ? ` · ${definition.priceCadence}` : " / year"}`}
                    </div>
                    <ul className={`m-0 flex flex-col gap-2 border-t pt-3 text-[12px] ${definition.level === "Trusted" ? "border-white/15 text-on-dark-muted" : "border-sand-border text-gray-600"}`}>
                      {definition.unlocks.map((feature) => <li className="flex items-start gap-2" key={feature}><Check className="mt-0.5 shrink-0 text-mint-text" size={14} /> <span>{feature}</span></li>)}
                    </ul>
                    {assignment && <p className="mb-0 text-[11px] text-gray-500">Active through {dateLabel(assignment.expiresAt)}</p>}
                    {isAvailableUpgrade && (
                      <AppLink className={`mt-auto inline-flex min-h-[42px] items-center justify-center gap-2 rounded-pill px-4 text-[12.5px] font-semibold ${definition.level === "Trusted" ? "bg-yellow text-deep hover:bg-yellow-press" : "bg-deep text-on-dark-heading hover:bg-deep-hover"}`} href={`/messages?topic=badge-${definition.level.toLowerCase()}`}>
                        Apply for {definition.level} <ArrowUpRight size={15} />
                      </AppLink>
                    )}
                  </article>
                );
              })}
            </div>
          </section>

          <section className="grid gap-4 lg:grid-cols-[1fr_1fr]" aria-label="Badge eligibility and feature restrictions">
            <div className="rounded-card border border-sand-border bg-cream p-5">
              <div className="mb-4 flex items-start justify-between gap-3">
                <div>
                  <p className="mb-1 text-[11px] font-bold uppercase tracking-[0.16em] text-sand-500">Eligibility checklist</p>
                  <h2 className="m-0 font-display text-[22px] font-medium">What is complete?</h2>
                </div>
                <StatusChip value={`${currentLevelIndex + 1} of ${badgeOrder.length} tiers`} />
              </div>
              <div className="flex flex-col gap-4">
                {badgeOrder.map((level) => (
                  <div className="rounded-field border border-sand-border bg-white p-4" key={level}>
                    <div className="mb-2 flex items-center justify-between gap-2"><strong>{level} badge</strong><StatusChip value={activeAssignmentFor(assignments, level) ? "Complete" : levelIndexLabel(level, currentLevelIndex)} /></div>
                    <ul className="m-0 flex flex-col gap-2 text-[12.5px] text-gray-600">
                      {requirements[level].map((item, index) => {
                        const state = checklistState(level, index);
                        return <li className="flex items-start gap-2" key={item}>{state === "complete" ? <Check className="mt-0.5 text-mint-text" size={15} /> : state === "pending" ? <CircleAlert className="mt-0.5 text-amber-text" size={15} /> : <LockKeyhole className="mt-0.5 text-sand-500" size={15} />}<span>{item}</span><span className="ml-auto text-[10px] font-bold uppercase tracking-[0.08em] text-sand-500">{state}</span></li>;
                      })}
                    </ul>
                  </div>
                ))}
              </div>
              <p className="mb-0 mt-4 text-[11px] leading-5 text-gray-500">The checklist is guidance only. Badge approval, booking counts, payment, and expiry are enforced by the backend and authorized administrators.</p>
            </div>
            <div className="rounded-card border border-sand-border bg-cream p-5">
              <div className="mb-4 flex items-start justify-between gap-3">
                <div>
                  <p className="mb-1 text-[11px] font-bold uppercase tracking-[0.16em] text-sand-500">Feature access</p>
                  <h2 className="m-0 font-display text-[22px] font-medium">Unlocked vs locked</h2>
                </div>
                <StatusChip value={`${featureAccess.activeLevel} access`} />
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-field border border-mint bg-mint-tint p-4"><strong className="text-[12px]">Unlocked now</strong><ul className="m-0 mt-2 flex flex-col gap-2 text-[12px] text-mint-text">{featureAccess.unlockedFeatures.map((feature) => <li className="flex items-start gap-2" key={feature}><Check size={14} />{feature}</li>)}</ul></div>
                <div className="rounded-field border border-sand-border bg-white p-4"><strong className="text-[12px]">Locked until upgrade</strong><ul className="m-0 mt-2 flex flex-col gap-2 text-[12px] text-gray-600">{featureAccess.lockedFeatures.length ? featureAccess.lockedFeatures.map((feature) => <li className="flex items-start gap-2" key={feature}><LockKeyhole className="mt-0.5 shrink-0 text-sand-500" size={14} />{feature}</li>) : <li>All current badge features are unlocked.</li>}</ul></div>
              </div>
              {featureAccess.lockedFeatures.length > 0 && <div className="mt-4 flex flex-wrap items-center gap-3"><AppLink className="inline-flex min-h-[42px] items-center gap-2 rounded-pill bg-deep px-4 text-[12.5px] font-semibold text-on-dark-heading hover:bg-deep-hover" href="/messages?topic=badge-upgrade">Ask about an upgrade <ArrowUpRight size={15} /></AppLink>{nextUpgrade && <span className="text-[11px] text-gray-500">Next tier: {nextUpgrade.level} · {nextUpgrade.annualPrice === 0 ? "included" : formatMoney(nextUpgrade.annualPrice, nextUpgrade.currency)}</span>}</div>}
            </div>
          </section>
        </>
      )}

      <div className="text-[12px] text-sand-500">Badge eligibility and payment confirmation are verified by an authorized administrator; this page never accepts self-declared approval.</div>
    </div>
  );
}

function levelIndexLabel(level: BadgeLevel, currentLevelIndex: number) {
  const index = badgeOrder.indexOf(level);
  return index <= currentLevelIndex ? "Unlocked" : "In progress";
}
