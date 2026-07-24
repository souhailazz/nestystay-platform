import { useState } from "react";
import { Calendar, Tag, Plus, Trash2, Check, AlertCircle } from "lucide-react";
import { formatMoney } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { HostPricingRule, HostPromotion } from "./types";

interface HostPricingPromotionsProps {
  view: string;
  token: string;
}

export function HostPricingPromotions({ view, token }: HostPricingPromotionsProps) {
  const [pricingRules, setPricingRules] = useState<HostPricingRule[]>([
    {
      id: "rule-1",
      propertyId: "11111111-1111-4111-8111-111111111111",
      seasonName: "High Season (Winter)",
      startDate: "2026-12-15",
      endDate: "2027-04-15",
      nightlyRate: 250,
      minimumNights: 3
    }
  ]);

  const [promotions, setPromotions] = useState<HostPromotion[]>([
    {
      id: "promo-1",
      propertyId: "11111111-1111-4111-8111-111111111111",
      name: "Last Minute Jamaican Special",
      discountPercent: 15,
      startDate: "2026-08-01",
      endDate: "2026-08-31",
      isActive: true
    }
  ]);

  const isPricing = view === "pricing";

  return (
    <div className="page-container container py-6" data-testid={isPricing ? "host-07-page" : "host-08-page"} id={isPricing ? "HOST-07" : "HOST-08"}>
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">{isPricing ? "HOST-07" : "HOST-08"}</span>
          <h2>{isPricing ? "Seasonal Pricing & Calendar Rules" : "Promotions & Discounts"}</h2>
          <PatoisPhrase phrase="Optimize Yuh Rate Dem" translation="Manage seasonal price overrides, minimum night rules, and promotional discounts." />
        </div>
        <button type="button" className="btn btn-primary" onClick={() => alert("Rule created.")}>
          <Plus size={16} /> {isPricing ? "Add Pricing Rule" : "Add Promotion"}
        </button>
      </header>

      {isPricing ? (
        <div className="space-y-4 max-w-3xl">
          {pricingRules.map((rule) => (
            <div key={rule.id} className="card-box flex justify-between items-center">
              <div>
                <span className="badge badge-sun">{rule.seasonName}</span>
                <p className="font-bold mt-1">{rule.startDate} to {rule.endDate}</p>
                <p className="subtext">Rate: {formatMoney(rule.nightlyRate, "USD")} / night • Min stay: {rule.minimumNights} nights</p>
              </div>
              <button type="button" className="btn btn-ghost text-coral btn-sm" onClick={() => setPricingRules(pricingRules.filter(r => r.id !== rule.id))}>
                <Trash2 size={16} />
              </button>
            </div>
          ))}
        </div>
      ) : (
        <div className="space-y-4 max-w-3xl">
          {promotions.map((promo) => (
            <div key={promo.id} className="card-box flex justify-between items-center">
              <div>
                <span className="badge badge-green">{promo.discountPercent}% OFF</span>
                <h3 className="font-bold mt-1">{promo.name}</h3>
                <p className="subtext">Valid: {promo.startDate} to {promo.endDate}</p>
              </div>
              <button type="button" className="btn btn-ghost text-coral btn-sm" onClick={() => setPromotions(promotions.filter(p => p.id !== promo.id))}>
                <Trash2 size={16} />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
