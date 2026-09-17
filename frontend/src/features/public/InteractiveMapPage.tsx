import { useState } from "react";
import { RefreshCw } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { ErrorState } from "../../components/ui/ErrorState";
import { EmblemRoundel, TierBadge } from "../../components/layout/PublicShell";
import { useProperties } from "../../hooks/useProperties";
import { formatMoney } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";
import { cx } from "../../lib/ui";

/** Live public map view. Kept with public discovery features rather than internal screen references. */
export function InteractiveMapPage() {
  const { properties, isLoading, error, reload } = useProperties();
  const [activeId, setActiveId] = useState<string | null>(null);
  const [chip, setChip] = useState("all");
  const chips = [["all", "All badges"], ["verified", "✓ Verified"], ["trusted", "★ Trusted"], ["wellness", "✦ Wellness"]] as const;
  const visible = properties.filter((property) => chip === "all" || property.badgeLevel.toLowerCase().includes(chip));
  const active = visible.find((property) => property.id === activeId) ?? visible[0];
  const mapped = visible.filter((property) => property.latitude != null && property.longitude != null);
  const mapUrl = active?.latitude != null && active.longitude != null
    ? `https://www.openstreetmap.org/export/embed.html?bbox=${active.longitude - 0.3}%2C${active.latitude - 0.18}%2C${active.longitude + 0.3}%2C${active.latitude + 0.18}&layer=mapnik&marker=${active.latitude}%2C${active.longitude}`
    : "https://www.openstreetmap.org/export/embed.html?bbox=-78.5%2C17.6%2C-76.0%2C18.6&layer=mapnik";
  const miniCard = (property: (typeof visible)[number], extra?: string) => (
    <button className={cx("flex cursor-pointer items-center gap-3 rounded-field border bg-cream p-2.5 text-left font-sans transition-shadow hover:shadow-[0_4px_14px_rgba(6,43,43,0.1)]", property.id === active?.id ? "border-[1.5px] border-deep-hover" : "border-sand-border", extra)} key={property.id} onClick={() => setActiveId(property.id)} type="button">
<img alt={`${property.title} preview`} className="block h-[76px] w-24 shrink-0 rounded-[12px] object-cover" src={property.imageUrl ?? property.galleryUrls?.[0] ?? getStayImage(0).src} />
      <span><span className="block font-display text-[15px] font-semibold text-ink">{property.title}</span><span className="mt-px block text-[12.5px] text-gray-600">{property.location}</span><span className="mt-1 flex flex-wrap items-center gap-2 text-sm text-ink"><strong>{formatMoney(property.nightlyRate, property.currency)}</strong> / night <TierBadge className="!px-2.5 !py-[3px] !text-[9.5px]" level={property.badgeLevel} /></span></span>
    </button>
  );

  return <div className="font-sans text-[15px] leading-[1.55] text-ink"><h1 className="sr-only" data-route-heading="true">Map search</h1><div className="sticky top-0 z-40 border-b border-sand-border bg-sand/95"><div className="flex flex-wrap items-center gap-3 px-5 py-2.5"><AppLink className="inline-flex min-h-11 items-center gap-2 rounded-field border-[1.5px] border-deep-hover bg-cream px-[18px] text-sm font-semibold text-deep-hover" href="/explore">← Back to list view</AppLink><span className="flex items-center gap-2.5 text-[13.5px] font-bold tracking-[0.14em] text-deep"><EmblemRoundel size={40} /> NESTY STAY</span><div className="ml-auto flex flex-wrap items-center gap-2">{chips.map(([value, label]) => <button className={cx("min-h-11 cursor-pointer rounded-pill px-[18px] font-semibold", chip === value ? "border border-deep bg-deep text-white" : "border-[1.5px] border-sand-input bg-cream text-gray-600")} key={value} onClick={() => setChip(value)} type="button">{label}</button>)}</div></div></div><div className="grid min-h-[calc(100vh-65px)] md:grid-cols-[372px_minmax(0,1fr)]"><aside className="order-2 flex flex-col gap-3 overflow-y-auto border-r border-sand-border bg-sand p-[18px] md:order-1"><div className="flex items-center justify-between text-xs font-semibold uppercase tracking-[0.1em] text-sand-500"><span>{visible.length} live stays</span><button aria-label="Refresh map results" className="rounded-full p-2 text-deep-hover" onClick={() => void reload()} type="button"><RefreshCw size={15} /></button></div>{isLoading ? <div className="p-5 text-sm text-gray-600">Loading live listings…</div> : error ? <ErrorState message={error} onRetry={() => void reload()} /> : visible.map((property) => miniCard(property))}</aside><main className="order-1 min-w-0 bg-[#D8E9E4] md:order-2"><div className="h-[55vh] min-h-[420px] w-full md:h-[calc(100vh-65px)]"><iframe title="Interactive NestyStay property map" className="h-full w-full border-0" loading="lazy" src={mapUrl} /></div><div className="border-t border-sand-border bg-cream p-4 text-sm text-gray-600"><strong className="text-ink">{active?.title ?? "Select a stay"}</strong>{active && <span> · {active.location} · {mapped.some((property) => property.id === active.id) ? "map marker shown" : "host coordinates not published"}</span>}<span className="ml-2">Map data © OpenStreetMap contributors.</span></div></main></div><div className="flex gap-3 overflow-x-auto border-t border-sand-border bg-sand p-3 md:hidden">{visible.slice(0, 3).map((property) => miniCard(property, "shrink-0 basis-[280px]"))}</div></div>;
}
