import { useState } from "react";
import { ShieldCheck, HelpCircle, FileText, ChevronDown, ChevronUp, Search } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";

interface LegalHelpPagesProps {
  view: string;
}

export function LegalHelpPages({ view }: LegalHelpPagesProps) {
  const [openFaq, setOpenFaq] = useState<number | null>(0);
  const [searchQuery, setSearchQuery] = useState("");

  const faqs = [
    {
      q: "How does guest identity verification work?",
      a: "NestyStay integrates with Alibaba Cloud eKYC to verify official government IDs (passports, driver's licenses) securely before check-in."
    },
    {
      q: "What is the InsuraGuest damage protection plan?",
      a: "For $15 per night, InsuraGuest covers accidental property damage up to $2,500 USD and medical expense protection for guests."
    },
    {
      q: "What cancellation policies are supported?",
      a: "NestyStay hosts can select Flexible (full refund 24h prior), Moderate (full refund 5 days prior), or Strict (50% refund)."
    }
  ];

  if (view === "help" || view === "faq") {
    return (
      <div className="page-container container py-6" data-testid="pub-12-help" id="PUB-12">
        <header className="page-header mb-6 text-center">
          <span className="badge badge-sun">PUB-12</span>
          <h2>Help Center & FAQs</h2>
          <PatoisPhrase phrase="How Can We Help Yuh?" translation="Search support articles or contact our 24/7 Jamaican support team." />
          <div className="search-box max-w-md mx-auto my-4 relative">
            <Search size={18} className="absolute left-3 top-3 text-gray-400" />
            <input 
              type="text" 
              className="input-control pl-10" 
              placeholder="Search help topics..." 
              value={searchQuery} 
              onChange={(e) => setSearchQuery(e.target.value)} 
            />
          </div>
        </header>

        <div className="max-w-2xl mx-auto space-y-3">
          {faqs.map((faq, idx) => (
            <div key={idx} className="card-box cursor-pointer" onClick={() => setOpenFaq(openFaq === idx ? null : idx)}>
              <div className="flex justify-between items-center font-bold text-lg">
                <span>{faq.q}</span>
                {openFaq === idx ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
              </div>
              {openFaq === idx && <p className="mt-3 text-sm subtext">{faq.a}</p>}
            </div>
          ))}
        </div>
      </div>
    );
  }

  const titleMap: Record<string, string> = {
    "about": "About NestyStay Jamaica",
    "trust": "Trust, Safety & Security",
    "terms": "Terms of Service",
    "privacy": "Privacy Policy & GDPR Compliance"
  };

  const idMap: Record<string, string> = {
    "about": "PUB-09",
    "trust": "PUB-10",
    "terms": "PUB-13",
    "privacy": "PUB-14"
  };

  const pageId = idMap[view] || "PUB-09";

  return (
    <div className="page-container container py-6" data-testid={`${pageId.toLowerCase()}-page`} id={pageId}>
      <header className="page-header mb-6">
        <span className="badge badge-sun">{pageId}</span>
        <h2>{titleMap[view] || "Platform Document"}</h2>
        <PatoisPhrase phrase="Transparent & Trusted Governance" translation="Our commitment to guest safety, host protection, and Jamaican tourism standards." />
      </header>

      <div className="card-box max-w-3xl space-y-4 text-sm leading-relaxed">
        <p>
          NestyStay is Jamaica's premier peer-to-peer vacation rental platform, providing authenticated accommodations with verified host badges, Alibaba Cloud eKYC identity checks, and InsuraGuest coverage.
        </p>
        <p>
          All transactions are secured via Stripe Elements card processing with full 3D Secure verification and 256-bit encryption.
        </p>
      </div>
    </div>
  );
}
