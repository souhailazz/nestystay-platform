export const modules = [
  {
    key: "rental-platform",
    name: "Rental Platform",
    phase: "Phase 1",
    status: "Core",
    summary:
      "Listings, host onboarding, founding tiers, badges, search, reviews, insurance, and checkout.",
    capabilities: ["Listings", "Founding tiers", "Host verification", "InsuraGuest"]
  },
  {
    key: "booking-manager",
    name: "Booking Manager",
    phase: "Phase 1",
    status: "Core",
    summary:
      "Book Now popup with pending eKYC, split payments, cancellation presets, QR entry, and messaging.",
    capabilities: ["PENDING flow", "Stripe", "Split payments", "QR logs"]
  },
  {
    key: "badges-pricing",
    name: "Badges and Pricing",
    phase: "Phase 2",
    status: "Configurable",
    summary:
      "Admin-controlled pricebooks for badge fees, founding campaigns, commissions, and annual renewals.",
    capabilities: ["Verified", "Trusted", "Wellness", "Campaigns"]
  },
  {
    key: "police-wellness",
    name: "Police Wellness",
    phase: "Phase 3",
    status: "Private",
    summary:
      "Active JCF-only officer onboarding, private IDs, eight visit types, escrow, reports, and badges.",
    capabilities: ["Annual ID reset", "Escrow", "Reports", "Privacy labels"]
  },
  {
    key: "directories",
    name: "Directories",
    phase: "Phase 4",
    status: "Marketplace",
    summary:
      "Custodians, trades, local businesses, police directory rules, sponsorships, reviews, and commissions.",
    capabilities: ["Sponsors", "Provider dashboards", "Ratings", "Guest promos"]
  },
  {
    key: "property-association",
    name: "Property and Association",
    phase: "Phase 5",
    status: "Governance",
    summary:
      "Multi-owner management, invoices, utilities, meetings, voting, proxies, bid opening, and vault.",
    capabilities: ["Owner portal", "Voting", "Proxies", "7-year archive"]
  }
];

export const portals = [
  { role: "guest", name: "Guest Portal", purpose: "Search, verify, book, pay, message, and access QR entry." },
  { role: "host", name: "Host Portal", purpose: "Manage properties, badges, bookings, pricing, verification, and wellness." },
  { role: "owner", name: "Owner Portal", purpose: "View own unit balances, bookings, documents, notices, and payments." },
  { role: "property-manager", name: "Property Manager", purpose: "Manage communities, owners, units, invoices, utilities, vendors, and gates." },
  { role: "association-executive", name: "Association Executive", purpose: "Run meetings, votes, proxies, statements, bid openings, and archives." },
  { role: "tenant", name: "Tenant Portal", purpose: "Access owner-approved notices, documents, visitor logs, and QR permissions." },
  { role: "service-provider", name: "Service Provider", purpose: "Manage custodian or trade profile, sponsorship, jobs, reviews, and payouts." },
  { role: "local-business", name: "Local Business", purpose: "Manage listing, legal docs, promotions, reviews, and guest-facing offers." },
  { role: "officer", name: "Officer Portal", purpose: "Manage private NestyStay ID, wellness jobs, reports, rates, and payouts." },
  { role: "gate-guard", name: "Gate Guard", purpose: "Scan QR codes, log visitors, receive manager messages, and verify access." },
  { role: "admin", name: "Admin Portal", purpose: "Configure pricebooks, vendors, roles, overrides, disputes, audits, and rollout controls." }
];

export const vendors = [
  {
    kind: "eKYC",
    primary: "Alibaba Cloud",
    notes: "Jumio and Onfido are backup-ready through the same verification interface."
  },
  {
    kind: "Payments",
    primary: "Stripe",
    notes: "Designed for checkout, split payments, refunds, subscriptions, escrow, and payouts."
  },
  {
    kind: "Storage",
    primary: "Cloudflare R2",
    notes: "DigitalOcean Spaces and Amazon S3 remain compatible fallback targets."
  },
  {
    kind: "Notifications",
    primary: "SES / Twilio / Firebase",
    notes: "Email, SMS, and push notifications share one queued event boundary."
  },
  {
    kind: "Insurance",
    primary: "InsuraGuest",
    notes: "Host insurance plans are exposed as an optional dashboard add-on."
  }
];
