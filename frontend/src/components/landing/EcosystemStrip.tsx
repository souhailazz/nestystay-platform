import { BriefcaseBusiness, Building2, Compass, Home, ShieldCheck, Store } from "lucide-react";
import { AppLink } from "../AppLink";
import { PUBLIC_WORKSPACE_LOGIN_PATHS, PUBLIC_WORKSPACE_OPTIONS } from "../../features/auth/workspaceOptions";

const icons = { Guest: Compass, Host: Home, PropertyManager: Building2, ServiceProvider: BriefcaseBusiness, LocalBusiness: Store, Officer: ShieldCheck } as const;

export function EcosystemStrip() {
  return (
    <section aria-labelledby="ecosystem-strip-heading" className="mx-auto max-w-[1140px] px-[clamp(18px,4vw,36px)] py-12 sm:py-16">
      <div className="rounded-card border border-sand-border bg-cream p-5 shadow-card sm:p-8">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div className="max-w-2xl">
            <span className="text-[11px] font-bold uppercase tracking-[0.2em] text-sand-600">The NestyStay ecosystem</span>
            <h2 className="m-0 mt-2 font-display text-[clamp(26px,4vw,40px)] font-medium leading-tight text-ink" id="ecosystem-strip-heading">More than a place to stay.</h2>
            <p className="m-0 mt-2 text-sm leading-relaxed text-sand-600">One platform for guests, hosts, property teams, local businesses, and wellness services.</p>
          </div>
          <AppLink className="inline-flex min-h-10 items-center rounded-pill bg-deep px-4 text-xs font-bold text-white" href="/login">Choose your workspace →</AppLink>
        </div>
        <div className="mt-6 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          {PUBLIC_WORKSPACE_OPTIONS.map((option) => {
            const Icon = icons[option.role];
            return <AppLink className="flex min-h-14 h-full items-center gap-3 rounded-field border border-sand-input bg-white px-3 py-2.5 transition-colors hover:border-deep-hover hover:bg-shell" href={`${PUBLIC_WORKSPACE_LOGIN_PATHS[option.role]}?workspace=${encodeURIComponent(option.role)}`} key={option.role}><span className="grid size-9 shrink-0 place-items-center rounded-full bg-shell text-deep-hover"><Icon aria-hidden="true" size={17} /></span><span className="min-w-0"><strong className="block text-xs leading-tight text-ink">{option.label}</strong><span className="block text-[11px] leading-snug text-sand-600">{option.description}</span></span></AppLink>;
          })}
        </div>
      </div>
    </section>
  );
}
