import { useEffect, useMemo, useState } from "react";
import { Heart, RefreshCw, Search } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { PublicFooter, TierBadge } from "../../components/layout/PublicShell";
import { LoadingState } from "../../components/ui/LoadingState";
import { ErrorState } from "../../components/ui/ErrorState";
import { ListControls, downloadCsv } from "../../components/ui/ListControls";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";
import { cx } from "../../lib/ui";
import { BookingModal } from "../../components/booking/BookingModal";
import type { AuthSession } from "../../lib/auth";

interface PublicSearchMapProps {
  view: string;
  session: AuthSession | null;
}

const BADGE_FILTERS = [
  ["all", "All"],
  ["free", "Free"],
  ["verified", "✓ Verified"],
  ["trusted", "★ Trusted"],
  ["wellness", "✦ Wellness"],
] as const;

type BadgeFilter = (typeof BADGE_FILTERS)[number][0];

const chipBase =
  "min-h-11 cursor-pointer rounded-pill px-5 font-sans text-[13.5px] font-semibold transition-colors";

/** PUB-02 — Explore stays (DS v2): search header, badge filter chips, results grid. */
export function PublicSearchMap({ view: _view, session }: PublicSearchMapProps) {
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const initialSearch = useMemo(() => new URLSearchParams(window.location.search).get("search") ?? "", []);
  const [query, setQuery] = useState(initialSearch);
  const [search, setSearch] = useState(initialSearch);
  const [badge, setBadge] = useState<BadgeFilter>("all");
  const [sort, setSort] = useState("relevance");
  const [page, setPage] = useState(0);
  const [saved, setSaved] = useState<Record<string, boolean>>({});
  const [bookingProp, setBookingProp] = useState<PropertyListing | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api
      .getProperties()
      .then((list) => {
        if (active) setProperties(list);
      })
      .catch((err) => { if (active) setError(err instanceof Error ? err.message : "Could not load stays."); })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [reloadKey]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    const results = properties.filter((p) => {
      const matchesBadge = badge === "all" || p.badgeLevel.toLowerCase().includes(badge);
      const matchesQuery = !q || `${p.title} ${p.location} ${p.country}`.toLowerCase().includes(q);
      return matchesBadge && matchesQuery && !p.isArchived;
    });
    return [...results].sort((left, right) => {
      if (sort === "price-low") return left.nightlyRate - right.nightlyRate;
      if (sort === "price-high") return right.nightlyRate - left.nightlyRate;
      if (sort === "name") return left.title.localeCompare(right.title);
      return 0;
    });
  }, [properties, badge, search, sort]);

  useEffect(() => setPage(0), [badge, search, sort]);
  const pageSize = 9;
  const visibleProperties = filtered.slice(page * pageSize, (page + 1) * pageSize);

  return (
    <div className="font-sans text-[15px] leading-[1.55] text-ink">
      {/* SEARCH HEADER */}
      <header className="mx-auto flex max-w-[1200px] flex-col gap-5 px-6 pb-2 pt-11">
        <div className="flex flex-wrap items-baseline justify-between gap-4">
          <h1 className="m-0 font-display text-[clamp(28px,3.4vw,38px)] font-normal tracking-[-0.015em]">
            Explore stays
          </h1>
          <AppLink
            className="inline-flex min-h-11 items-center text-[14.5px] font-semibold text-deep-hover hover:text-deep"
            href="/explore/map"
          >
            Open map view →
          </AppLink>
        </div>
        <form
          className="flex flex-wrap items-center gap-3"
          onSubmit={(event) => {
            event.preventDefault();
            setSearch(query);
          }}
        >
          <div className="flex min-h-12 flex-[1_1_320px] items-center gap-2.5 rounded-field border-[1.5px] border-sand-input bg-cream px-4 transition-colors focus-within:border-deep-hover">
            <Search aria-hidden="true" className="shrink-0 text-sand-500" size={17} />
            <input
              aria-label="Search stays"
              className="min-h-11 flex-1 border-none bg-transparent font-sans text-[15px] text-ink outline-none placeholder:text-sand-500"
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search by town, parish or stay name"
              type="text"
              value={query}
            />
          </div>
          <button
            className="min-h-12 cursor-pointer rounded-field border-none bg-deep px-6 font-sans text-[15px] font-semibold text-white transition-colors hover:bg-deep-hover"
            type="submit"
          >
            Search
          </button>
          <button
            aria-label="Refresh results"
            className="grid min-h-12 min-w-12 cursor-pointer place-items-center rounded-field border-[1.5px] border-sand-input bg-transparent text-deep-hover transition-colors hover:border-deep-hover"
            onClick={() => setReloadKey((k) => k + 1)}
            type="button"
          >
            <RefreshCw size={17} />
          </button>
        </form>
        <div className="flex flex-wrap items-center gap-2">
          <span className="mr-1 text-xs font-semibold uppercase tracking-[0.08em] text-sand-500">Host badge</span>
          {BADGE_FILTERS.map(([value, label]) => (
            <button
              className={cx(
                chipBase,
                badge === value
                  ? "border-none bg-deep text-white"
                  : "border-[1.5px] border-sand-input bg-cream text-gray-600 hover:border-deep-hover hover:text-deep-hover",
              )}
              key={value}
              onClick={() => setBadge(value)}
              type="button"
            >
              {label}
            </button>
          ))}
          {!loading && (
            <span className="ml-auto text-[13px] text-sand-500">
              {filtered.length} {filtered.length === 1 ? "stay" : "stays"}
            </span>
          )}
        </div>
        <ListControls
          className="mt-3"
          label="Filter visible stays"
          onExport={() => downloadCsv("nesty-stays.csv", ["Title", "Location", "Country", "Badge", "Nightly rate"], filtered.map((property) => [property.title, property.location, property.country, property.badgeLevel, property.nightlyRate]))}
          onPageChange={setPage}
          onQueryChange={(value) => { setQuery(value); setSearch(value); }}
          onSortChange={setSort}
          page={page}
          pageSize={pageSize}
          query={query}
          sort={sort}
          sortOptions={[{ value: "relevance", label: "Relevance" }, { value: "name", label: "Name" }, { value: "price-low", label: "Price: low to high" }, { value: "price-high", label: "Price: high to low" }]}
          total={filtered.length}
        />
      </header>

      {/* RESULTS */}
      <main className="mx-auto max-w-[1200px] px-6 pb-14 pt-7">
        {loading ? (
          <LoadingState label="Loading your stays" />
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center gap-2.5 rounded-card border border-dashed border-sand-input bg-cream px-6 py-10 text-center">
            <div className="grid size-[52px] place-items-center rounded-full bg-shell text-sand-500">
              <Search size={20} />
            </div>
            <div className="font-display text-lg font-medium">No stays match that search</div>
            <div className="max-w-[300px] text-[13.5px] text-gray-600">
              Try a different parish, or clear the badge filter to see every stay.
            </div>
            <button
              className="mt-1.5 min-h-11 cursor-pointer rounded-field border-[1.5px] border-deep-hover bg-transparent px-5 font-sans text-sm font-semibold text-deep-hover transition-colors hover:bg-shell"
              onClick={() => {
                setBadge("all");
                setQuery("");
                setSearch("");
              }}
              type="button"
            >
              Clear filters
            </button>
          </div>
        ) : error ? (
          <ErrorState message={error} onRetry={() => setReloadKey((key) => key + 1)} />
        ) : (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-[18px]">
            {visibleProperties.map((prop, index) => (
              <article
                className="flex flex-col overflow-hidden rounded-card border border-sand-border bg-cream shadow-[0_1px_2px_rgba(96,74,20,0.08)] transition-shadow hover:shadow-[0_16px_34px_rgba(96,74,20,0.16)]"
                key={prop.id}
              >
                <div className="relative aspect-[3/2]">
                  <img
                    alt={`${prop.title} — ${prop.location}`}
                    className="block h-full w-full object-cover"
                    src={prop.imageUrl ?? getStayImage(index).src}
                  />
                  <TierBadge className="absolute left-3.5 top-3.5" level={prop.badgeLevel} />
                  <button
                    aria-label={`Save ${prop.title}`}
                    aria-pressed={Boolean(saved[prop.id])}
                    className={cx(
                      "absolute right-2.5 top-2.5 grid size-11 cursor-pointer place-items-center rounded-full border-none bg-white/90 transition-colors",
                      saved[prop.id] ? "text-coral" : "text-gray-600 hover:text-coral",
                    )}
                    onClick={() => setSaved((s) => ({ ...s, [prop.id]: !s[prop.id] }))}
                    type="button"
                  >
                    <Heart fill={saved[prop.id] ? "currentColor" : "none"} size={18} />
                  </button>
                </div>
                <div className="flex flex-1 flex-col gap-2 p-4 px-[18px]">
                  <div className="flex items-baseline justify-between gap-2.5">
                    <span className="font-display text-[17px] font-medium">{prop.title}</span>
                    {/* Placeholder rating until the reviews API is wired in (deterministic per stay). */}
                    <span className="whitespace-nowrap text-[13px] text-gray-600">
                      {prop.badgeLevel.toLowerCase() === "free" ? "New" : `★ ${(4.6 + (prop.title.length % 5) / 10).toFixed(1)}`}
                    </span>
                  </div>
                  <div className="text-[13px] text-gray-600">
                    {prop.location} · {prop.country}
                  </div>
                  {prop.highlights.length > 0 && (
                    <div className="flex flex-wrap gap-1.5">
                      {prop.highlights.slice(0, 3).map((h) => (
                        <span className="rounded-pill bg-shell px-2.5 py-1 text-[11.5px] font-semibold text-gray-600" key={h}>
                          {h}
                        </span>
                      ))}
                    </div>
                  )}
                  <div className="mt-auto flex items-center justify-between gap-2.5 pt-2">
                    <span className="text-base">
                      <strong>{formatMoney(prop.nightlyRate, prop.currency)}</strong>{" "}
                      <span className="text-[13px] text-sand-500">/ night</span>
                    </span>
                    <span className="flex gap-2">
                      <AppLink
                        className="inline-flex min-h-11 items-center rounded-[12px] border-[1.5px] border-deep-hover px-4 text-[13.5px] font-semibold text-deep-hover transition-colors hover:bg-shell"
                        href={`/properties/${prop.id}`}
                      >
                        Details
                      </AppLink>
                      <button
                        className="inline-flex min-h-11 cursor-pointer items-center rounded-[12px] border-none bg-deep px-4 font-sans text-[13.5px] font-semibold text-white transition-colors hover:bg-deep-hover"
                        onClick={() => setBookingProp(prop)}
                        type="button"
                      >
                        Book
                      </button>
                    </span>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </main>

      <PublicFooter />

      <BookingModal
        open={Boolean(bookingProp)}
        onClose={() => setBookingProp(null)}
        onCreated={(created) => {
          const nextStep = ["PENDING", "PENDING_VERIFICATION", "PENDINGVERIFICATION"].includes(created.status.trim().toUpperCase()) ? "identity" : "checkout";
          window.location.href = `/booking/${created.id}/${nextStep}`;
        }}
        property={bookingProp}
        session={session}
      />
    </div>
  );
}
