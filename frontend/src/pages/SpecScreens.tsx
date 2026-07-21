import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  AlertTriangle,
  BadgeCheck,
  BarChart3,
  Bell,
  Bookmark,
  BriefcaseBusiness,
  Building2,
  CalendarDays,
  Car,
  Check,
  ChevronRight,
  Clock,
  CreditCard,
  Download,
  FileText,
  Heart,
  Home,
  KeyRound,
  Lock,
  Mail,
  Map,
  MapPin,
  MessageSquare,
  Pencil,
  ReceiptText,
  Search,
  ShieldAlert,
  ShieldCheck,
  SlidersHorizontal,
  Star,
  TimerReset,
  UserCheck,
  Wrench,
  X,
  type LucideIcon,
} from "lucide-react";
import { AppLink } from "../components/AppLink";
import { Badge } from "../components/ui/Badge";
import { Button, buttonClassName } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import { Field, InlineLabel, Input, Select, Textarea } from "../components/ui/Input";
import { PageHeader } from "../components/ui/PageHeader";
import { PatoisToast } from "../components/ui/PatoisToast";
import { useProperties } from "../hooks/useProperties";
import { formatMoney } from "../lib/api";
import { getStayImage } from "../lib/stayImages";

type SpecMetric = {
  icon: LucideIcon;
  label: string;
  value: string;
};

type SpecAction = {
  label: string;
  href?: string;
  variant?: "sun" | "outline" | "dark" | "ghost";
};

function ScreenShell({
  id,
  eyebrow,
  title,
  copy,
  actions,
  children,
  className,
}: {
  id: string;
  eyebrow: string;
  title: string;
  copy: string;
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={["product-page spec-page", className].filter(Boolean).join(" ")}>
      <PageHeader
        eyebrow={`${id} / ${eyebrow}`}
        title={title}
        copy={copy}
        actions={actions}
      />
      {children}
    </div>
  );
}

function MetricStrip({ items }: { items: SpecMetric[] }) {
  return (
    <div className="metric-grid spec-metric-grid">
      {items.map((item) => {
        const Icon = item.icon;
        return (
          <Card className="metric-card" key={item.label}>
            <span>
              <Icon size={21} />
            </span>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
          </Card>
        );
      })}
    </div>
  );
}

function SpecCard({
  icon: Icon,
  title,
  copy,
  meta,
  action,
}: {
  icon: LucideIcon;
  title: string;
  copy: string;
  meta?: string;
  action?: SpecAction;
}) {
  return (
    <Card className="spec-card">
      <span className="spec-card__icon">
        <Icon size={20} />
      </span>
      <div>
        {meta && <Badge tone="slate">{meta}</Badge>}
        <h3>{title}</h3>
        <p>{copy}</p>
      </div>
      {action?.href ? (
        <AppLink className={buttonClassName(action.variant ?? "outline")} href={action.href}>
          {action.label} <ChevronRight size={16} />
        </AppLink>
      ) : action ? (
        <Button variant={action.variant ?? "outline"}>
          {action.label} <ChevronRight size={16} />
        </Button>
      ) : null}
    </Card>
  );
}

function DataTable({
  columns,
  rows,
}: {
  columns: string[];
  rows: Array<Array<ReactNode>>;
}) {
  return (
    <div className="spec-table-wrap">
      <table className="spec-table">
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column}>{column}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={rowIndex}>
              {row.map((cell, cellIndex) => (
                <td key={cellIndex}>{cell}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function StayThumbs({ start = 0 }: { start?: number }) {
  return (
    <div className="spec-collage" aria-hidden="true">
      {[0, 1, 2].map((offset) => {
        const image = getStayImage(start + offset);
        return <img key={image.src} src={image.src} alt="" loading="lazy" />;
      })}
    </div>
  );
}

function VerificationToggle({
  enabled,
  onChange,
}: {
  enabled: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <div className="verification-toggle-card">
      <div>
        <strong>Enable guest identity verification for this property</strong>
        <p>NEVER AUTOMATIC - host enables per property.</p>
      </div>
      <InlineLabel className="switch-label">
        <input checked={enabled} type="checkbox" onChange={(event) => onChange(event.target.checked)} />
        <span className="switch-track" aria-hidden="true" />
        <span>{enabled ? "Enabled" : "Off"}</span>
      </InlineLabel>
      <div className="verification-pricing">
        <span>$0.14 per booking</span>
        <span>$1.26 / 10-pack</span>
        <span>$2.99 / month</span>
        <span>$29.99 / year</span>
      </div>
    </div>
  );
}

const staticProperties = [
  ["Azure Cove Villa", "Montego Bay", "$450", "Trusted"],
  ["Kingston Business Stay", "Kingston", "$320", "Verified"],
  ["Portland Rainforest House", "Port Antonio", "$280", "Wellness"],
  ["Negril Beach Cottage", "Negril", "$390", "Trusted"],
];

const jamaicaPins = [
  { x: 20, y: 46, price: "$320", parish: "Westmoreland" },
  { x: 30, y: 34, price: "$450", parish: "St. James" },
  { x: 44, y: 44, price: "$280", parish: "Trelawny" },
  { x: 55, y: 36, price: "$510", parish: "St. Ann" },
  { x: 66, y: 52, price: "$260", parish: "St. Mary" },
  { x: 73, y: 44, price: "$370", parish: "Portland" },
  { x: 58, y: 68, price: "$295", parish: "St. Catherine" },
  { x: 70, y: 74, price: "$340", parish: "Kingston" },
];

export function AuthPostLoginToastPage() {
  return (
    <ScreenShell
      id="AUTH-POST"
      eyebrow="Auth flow"
      title="Post-login toast."
      copy="Standalone frame showing the required login success toast over the dashboard."
    >
      <section className="product-section spec-dashboard-preview">
        <PatoisToast className="patois-toast--demo" />
        <MetricStrip
          items={[
            { icon: Home, label: "Trips", value: "4" },
            { icon: ShieldCheck, label: "Verified", value: "Ready" },
            { icon: MessageSquare, label: "Messages", value: "3" },
            { icon: CreditCard, label: "Wallet", value: "$0" },
          ]}
        />
        <div className="spec-card-grid">
          <SpecCard icon={CalendarDays} title="Next stay" copy="Coral Reef Sanctuary is approved and ready for arrival." />
          <SpecCard icon={Bell} title="Notifications" copy="Booking, payment, and host replies are grouped in the inbox." />
        </div>
      </section>
    </ScreenShell>
  );
}

export function LogoutScreenPage() {
  return (
    <div className="logout-screen">
      <div className="logout-screen__card">
        <img src="/assets/reference/nestystay-logo.png" alt="NestyStay" />
        <p className="patois-line">Likkle More</p>
        <h1>See you later!</h1>
        <p>You have been signed out safely.</p>
        <div className="button-row">
          <AppLink className={buttonClassName("sun")} href="/login">
            Log back in
          </AppLink>
          <AppLink className={buttonClassName("outline")} href="/explore">
            Browse as guest
          </AppLink>
        </div>
        <footer>nestystay.net - 754-248-2435</footer>
      </div>
    </div>
  );
}

export function MapSearchPage() {
  const { properties } = useProperties();
  const cards = properties.length
    ? properties.slice(0, 4).map((property) => [
        property.title,
        property.location,
        formatMoney(property.nightlyRate, property.currency),
        property.badgeLevel,
      ])
    : staticProperties;

  return (
    <ScreenShell
      id="PUB-MAP"
      eyebrow="Public experience"
      title="Map search view."
      copy="Interactive-style Jamaica map with price pins, parish context, and responsive card drawer."
      actions={
        <AppLink className={buttonClassName("outline")} href="/explore">
          Back to list view
        </AppLink>
      }
      className="spec-page--map"
    >
      <section className="product-section map-search-layout">
        <aside className="map-list-panel">
          <div className="search-panel">
            <Field label="Destination">
              <Input defaultValue="Jamaica" />
            </Field>
            <Field label="Guests">
              <Select defaultValue="2">
                <option value="2">2 guests</option>
                <option value="4">4 guests</option>
                <option value="6">6 guests</option>
              </Select>
            </Field>
            <Button variant="dark">
              <SlidersHorizontal size={17} /> Filters
            </Button>
          </div>
          <div className="compact-list">
            {cards.map(([title, location, price, badge], index) => (
              <Card className="compact-list__item map-property-row" key={title}>
                <img src={getStayImage(index).src} alt="" />
                <div>
                  <strong>{title}</strong>
                  <span>{location} - {price} / night</span>
                </div>
                <Badge tone={badge === "Trusted" || badge === "Wellness" ? "green" : "sun"}>{badge}</Badge>
              </Card>
            ))}
          </div>
        </aside>
        <div className="jamaica-map" role="img" aria-label="Jamaica map with price pins">
          <div className="jamaica-map__island" />
          {jamaicaPins.map((pin) => (
            <button
              className="price-pin"
              key={pin.parish}
              style={{ left: `${pin.x}%`, top: `${pin.y}%` }}
              type="button"
            >
              {pin.price}
              <span>{pin.parish}</span>
            </button>
          ))}
        </div>
      </section>
    </ScreenShell>
  );
}

export function ComingSoonPage() {
  return (
    <div className="coming-soon-page">
      <img src="/assets/reference/nestystay-logo.png" alt="NestyStay" />
      <p className="patois-line">Wi Soon Come!</p>
      <h1>Coming soon - Jamaica's own trusted stays platform.</h1>
      <div className="countdown-grid" aria-label="Launch countdown">
        {[
          ["14", "Days"],
          ["06", "Hrs"],
          ["32", "Min"],
          ["09", "Sec"],
        ].map(([value, label]) => (
          <span key={label}>
            <strong>{value}</strong>
            <small>{label}</small>
          </span>
        ))}
      </div>
      <form className="notify-form">
        <Input aria-label="Email address" placeholder="you@example.com" type="email" />
        <Button type="submit">Notify Me</Button>
      </form>
      <AppLink className="coming-soon-link" href="/admin">
        150 Platinum Founding Member spots available. See details <ChevronRight size={16} />
      </AppLink>
      <footer>nestystay.net - 754-248-2435</footer>
    </div>
  );
}

export function FavoritesCollectionsPage() {
  const collections = [
    ["Honeymoon Ideas", "6 saves"],
    ["Business Trips Jamaica", "4 saves"],
    ["Friends Weekend", "2 saves"],
  ];

  return (
    <ScreenShell
      id="TRAV-COL"
      eyebrow="Traveler portal"
      title="Favorites collections."
      copy="Named wishlist collections with thumbnail collages and direct management actions."
      actions={<Button><Bookmark size={17} /> Create new collection</Button>}
    >
      <section className="product-section spec-card-grid spec-card-grid--three">
        {collections.map(([name, saves], index) => (
          <Card className="collection-card" key={name}>
            <StayThumbs start={index} />
            <h3>{name}</h3>
            <p>{saves}</p>
            <div className="button-row">
              <Button variant="outline">View collection</Button>
              <Button variant="ghost">Rename</Button>
              <Button variant="ghost">Delete</Button>
            </div>
          </Card>
        ))}
      </section>
    </ScreenShell>
  );
}

export function InvoicesPage() {
  return (
    <ScreenShell
      id="TRAV-INV"
      eyebrow="Traveler portal"
      title="Invoices."
      copy="Downloadable payment records with year filtering and batch export."
      actions={<Button><Download size={17} /> Download all</Button>}
    >
      <section className="product-section">
        <div className="search-panel spec-filter-bar">
          <Field label="Year">
            <Select defaultValue="2026">
              <option>2026</option>
              <option>2025</option>
            </Select>
          </Field>
          <Button variant="dark">
            <Search size={17} /> Filter
          </Button>
        </div>
        <DataTable
          columns={["Invoice", "Property", "Stay dates", "Amount", "Status", "File"]}
          rows={[
            ["NST-2026-1024", "Coral Reef Sanctuary", "Jul 12-16", "$4,130", <Badge tone="green">Paid</Badge>, <a>Download</a>],
            ["NST-2026-0977", "Kingston Business Stay", "Jun 2-5", "$1,280", <Badge tone="green">Paid</Badge>, <a>Download</a>],
            ["NST-2026-0811", "Blue Mountain Retreat", "Apr 18-20", "$980", <Badge tone="green">Paid</Badge>, <a>Download</a>],
            ["NST-2026-0642", "Negril Beach Cottage", "Feb 7-10", "$1,620", <Badge tone="green">Paid</Badge>, <a>Download</a>],
          ]}
        />
      </section>
    </ScreenShell>
  );
}

export function PendingReviewsPage() {
  const reviewCards = [
    ["Coral Reef Sanctuary", "Jul 12-16", "12 days left to review", false],
    ["Kingston Business Stay", "Jun 2-5", "5 days left to review", false],
    ["Negril Beach Cottage", "Review window closed", "Closed", true],
  ] as const;

  return (
    <ScreenShell
      id="TRAV-PEND"
      eyebrow="Traveler portal"
      title="Pending reviews."
      copy="Review reminders show deadlines, closed windows, and property context."
    >
      <section className="product-section">
        <div className="notice-panel">You have 2 pending reviews.</div>
        <div className="spec-card-grid">
          {reviewCards.map(([title, dates, deadline, closed], index) => (
            <Card className={closed ? "review-card review-card--closed" : "review-card"} key={title}>
              <img src={getStayImage(index).src} alt="" />
              <div>
                <h3>{title}</h3>
                <p>{dates}</p>
                <Badge tone={closed ? "slate" : "sun"}>{deadline}</Badge>
              </div>
              <Button disabled={closed} variant={closed ? "ghost" : "sun"}>
                Write review
              </Button>
            </Card>
          ))}
        </div>
      </section>
    </ScreenShell>
  );
}

export function NotificationsCenterPage() {
  const notifications = [
    [CalendarDays, "Your booking at Coral Reef Sanctuary is confirmed", "Booking", "2m ago", true],
    [CreditCard, "Payment of $4,130 received", "Payments", "24m ago", true],
    [MessageSquare, "Marcia replied to your message", "Messages", "1h ago", false],
    [BadgeCheck, "Your Trusted Badge has been activated", "Platform", "Yesterday", false],
  ] as const;

  return (
    <ScreenShell
      id="TRAV-NOTIF"
      eyebrow="Traveler portal"
      title="Notifications center."
      copy="A full platform inbox with filters, unread markers, timestamps, and read controls."
    >
      <section className="product-section">
        <div className="segmented-control spec-tabs">
          {["All", "Unread", "Bookings", "Payments", "Messages"].map((tab, index) => (
            <button className={index === 0 ? "is-active" : ""} key={tab} type="button">
              {tab}
            </button>
          ))}
        </div>
        <div className="compact-list notification-list">
          {notifications.map(([Icon, title, type, time, unread]) => (
            <Card className={unread ? "compact-list__item is-unread" : "compact-list__item"} key={title}>
              <Icon size={20} />
              <div>
                <strong>{title}</strong>
                <span>{type} - {time}</span>
              </div>
              <Button variant="ghost">Mark as read</Button>
            </Card>
          ))}
        </div>
      </section>
    </ScreenShell>
  );
}

export function TripSuggestionsPage() {
  const suggestions = [
    ["Kingston Business Stay", "Kingston", "$320", "Near Kingston", true],
    ["Blue Mountain Retreat", "St. Andrew", "$280", "Matches your dates", true],
    ["Coral Reef Sanctuary", "Montego Bay", "$450", "Top rated", false],
  ] as const;

  return (
    <ScreenShell
      id="TRAV-SUGG"
      eyebrow="Traveler portal"
      title="Trip suggestions."
      copy="Preference-matched stays with availability, save controls, and match reason tags."
    >
      <section className="product-section">
        <div className="notice-panel">12 stays match your preferences.</div>
        <div className="search-panel spec-filter-bar">
          <Field label="Dates"><Input type="date" defaultValue="2026-08-14" /></Field>
          <Field label="Guests"><Select defaultValue="2"><option>2 guests</option><option>4 guests</option></Select></Field>
          <Field label="Price"><Select defaultValue="400"><option>Under $400</option><option>Under $600</option></Select></Field>
        </div>
        <div className="stay-result-grid">
          {suggestions.map(([title, parish, price, reason, available], index) => (
            <Card className="suggestion-card" key={title}>
              <img src={getStayImage(index).src} alt="" />
              <Badge tone={available ? "green" : "slate"}>{available ? "Available" : "Unavailable"}</Badge>
              <h3>{title}</h3>
              <p>{parish} - {price} / night</p>
              <span className="match-tag">{reason}</span>
              <div className="button-row">
                <Button variant="ghost"><Heart size={17} /> Save</Button>
                <Button variant="outline">View details</Button>
              </div>
            </Card>
          ))}
        </div>
      </section>
    </ScreenShell>
  );
}

export function HostPropertyEditPage() {
  const { properties } = useProperties();
  const property = properties[0];
  const [verificationEnabled, setVerificationEnabled] = useState(false);

  return (
    <ScreenShell
      id="HOST-EDIT"
      eyebrow="Host portal"
      title="Edit published listing."
      copy="Inline editing mode for an existing property, separate from the creation wizard."
      actions={
        property ? (
          <AppLink className={buttonClassName("outline")} href={`/properties/${property.id}`}>
            View public listing
          </AppLink>
        ) : null
      }
    >
      <section className="product-section edit-property-layout">
        <Card className="edit-property-summary">
          <img src={getStayImage(1).src} alt="" />
          <div>
            <Badge tone="green">{property?.badgeLevel ?? "Published"}</Badge>
            <h2>{property?.title ?? "Azure Cove Villa"}</h2>
            <p>Last edited July 21, 2026 - Published</p>
          </div>
        </Card>
        <div className="edit-section-list">
          {[
            ["Type", "Apartment - entire place"],
            ["Location", property?.location ?? "Montego Bay, Jamaica"],
            ["Capacity", "4 guests - 2 beds - 2 baths"],
            ["Amenities", "Workspace, Wi-Fi, air conditioning, beach access"],
            ["Photos", "8 active gallery photos"],
            ["Description", "Premium Jamaica stay with verified host support."],
            ["Pricing", `${formatMoney(property?.nightlyRate ?? 450, property?.currency ?? "USD")} per night`],
            ["Availability", "Open calendar with hold protection"],
          ].map(([label, value]) => (
            <Card className="inline-edit-row" key={label}>
              <div>
                <strong>{label}</strong>
                <span>{value}</span>
              </div>
              <Button variant="ghost">
                <Pencil size={16} /> Edit
              </Button>
            </Card>
          ))}
          <VerificationToggle enabled={verificationEnabled} onChange={setVerificationEnabled} />
        </div>
      </section>
    </ScreenShell>
  );
}

export function HostReportsPage() {
  return (
    <ScreenShell
      id="HOST-RPT"
      eyebrow="Host portal"
      title="Reports and exports."
      copy="Revenue, occupancy, cancellation, review, and tax-ready reporting for hosts."
      actions={
        <div className="button-row">
          <Button variant="outline"><Download size={17} /> Export CSV</Button>
          <Button><FileText size={17} /> Export PDF</Button>
        </div>
      }
    >
      <section className="product-section">
        <MetricStrip
          items={[
            { icon: CreditCard, label: "Revenue", value: "$24,850" },
            { icon: BarChart3, label: "Occupancy", value: "84%" },
            { icon: CalendarDays, label: "Bookings", value: "12" },
            { icon: X, label: "Cancellations", value: "1" },
          ]}
        />
        <div className="management-layout">
          <Card className="spec-panel">
            <h3>Tax-ready summary</h3>
            <p>Gross revenue is separated from platform fees and net payout totals.</p>
            <DataTable
              columns={["Line", "Amount"]}
              rows={[
                ["Gross revenue", "$24,850"],
                ["Platform fees", "$2,236.50"],
                ["Net host payout", "$22,613.50"],
              ]}
            />
            <Button variant="dark"><Download size={17} /> Download tax summary</Button>
          </Card>
          <Card className="spec-panel">
            <h3>Date range</h3>
            <div className="segmented-control spec-tabs">
              {["This month", "Last 3 months", "YTD", "Custom"].map((tab, index) => (
                <button className={index === 0 ? "is-active" : ""} key={tab} type="button">{tab}</button>
              ))}
            </div>
          </Card>
        </div>
      </section>
    </ScreenShell>
  );
}

export function PropertyManagerGatePage() {
  return (
    <ScreenShell
      id="PM-GATE"
      eyebrow="Property manager portal"
      title="Gate communications."
      copy="Dedicated PM-to-guard messaging with visitor details and security audit history."
    >
      <section className="product-section gate-layout">
        <Card className="thread-list">
          <Field label="All gates">
            <Select defaultValue="north"><option value="north">North gate</option><option value="garage">Garage gate</option></Select>
          </Field>
          {["Azure Towers", "Harbor House", "Garden Gate"].map((thread, index) => (
            <button className={index === 0 ? "thread-button is-active" : "thread-button"} key={thread} type="button">
              <Building2 size={18} />
              <span>{thread}</span>
            </button>
          ))}
        </Card>
        <Card className="message-thread">
          <h3>North gate - Guard NST-GRD-014</h3>
          <div className="message-bubble">
            <strong>Visitor: Alicia Brown</strong>
            <p>Vehicle plate 7782 HT. Reason: owner appointment. Photo attached for review.</p>
            <small>10:42 AM</small>
          </div>
          <div className="message-bubble message-bubble--reply">
            <strong>Approved</strong>
            <p>Access approved for 45 minutes. Logged for security audit.</p>
            <small>10:44 AM</small>
          </div>
          <div className="button-row">
            <Button><Check size={17} /> Approved</Button>
            <Button variant="ghost"><X size={17} /> Denied</Button>
          </div>
        </Card>
      </section>
    </ScreenShell>
  );
}

export function PropertyManagerUtilitiesPage() {
  return (
    <ScreenShell
      id="PM-UTIL"
      eyebrow="Property manager portal"
      title="Utility tracking."
      copy="Meter reading, allocation, bill-back preview, and invoice generation for managed units."
      actions={<Button><Wrench size={17} /> Add meter reading</Button>}
    >
      <section className="product-section">
        <div className="search-panel spec-filter-bar">
          <Field label="Period"><Select defaultValue="october"><option value="october">October 2026</option></Select></Field>
          <Button variant="outline">Preview bill-back</Button>
          <Button>Generate utility invoices</Button>
        </div>
        <DataTable
          columns={["Unit", "Previous", "Current", "Consumption", "Method", "Bill-back"]}
          rows={[
            ["A-101", "12,420", "12,890", "470 kWh", "Metered", "$142.80"],
            ["A-102", "8,120", "8,395", "275 kWh", "Metered", "$83.60"],
            ["B-201", "Shared", "Shared", "Equal split", "Equal split", "$91.25"],
            ["B-204", "Custom", "Custom", "35%", "Custom %", "$126.00"],
          ]}
        />
      </section>
    </ScreenShell>
  );
}

export function PropertyManagerVerificationPage() {
  return (
    <ScreenShell
      id="PM-VERIFY"
      eyebrow="Property manager portal"
      title="Tenant and owner verification."
      copy="Pending building members can be verified with document checklists and eKYC triggers."
    >
      <section className="product-section">
        <DataTable
          columns={["Name", "Unit", "Type", "Submitted", "eKYC", "Documents", "Decision"]}
          rows={[
            ["Kim Watson", "A-101", "Owner", "Jul 18", <Badge tone="sun">Pending</Badge>, "Government ID, proof of ownership", <Button variant="outline">Trigger eKYC</Button>],
            ["Marlon Reid", "B-204", "Tenant", "Jul 19", <Badge tone="green">Verified</Badge>, "Government ID, lease agreement", <Button variant="ghost">Approve</Button>],
            ["Tanya Cole", "C-301", "Owner", "Jul 20", <Badge tone="coral">Rejected</Badge>, "Reason required before reject", <Button variant="ghost">Reject</Button>],
          ]}
        />
      </section>
    </ScreenShell>
  );
}

export function PropertyManagerReportsPage() {
  return (
    <ScreenShell
      id="PM-RPT"
      eyebrow="Property manager portal"
      title="Portfolio reports and exports."
      copy="Portfolio-level owner revenue, PM fees, utility costs, maintenance costs, and tax exports."
      actions={
        <div className="button-row">
          <Button variant="outline">CSV - all owners</Button>
          <Button variant="outline">PDF - per owner statement</Button>
          <Button>Tax report</Button>
        </div>
      }
    >
      <section className="product-section">
        <MetricStrip
          items={[
            { icon: CreditCard, label: "Revenue", value: "$48,200" },
            { icon: BarChart3, label: "Occupancy", value: "82%" },
            { icon: Wrench, label: "Maintenance", value: "$4,200" },
            { icon: ReceiptText, label: "Utilities", value: "$1,800" },
          ]}
        />
        <DataTable
          columns={["Owner", "Units", "Revenue", "PM fees", "Net payout"]}
          rows={[
            ["Denise Brown", "3", "$18,400", "$1,840", "$16,560"],
            ["Island Holdings", "5", "$22,900", "$2,290", "$20,610"],
            ["C. Morgan", "1", "$6,900", "$690", "$6,210"],
          ]}
        />
      </section>
    </ScreenShell>
  );
}

export function InsuraGuestPage() {
  const plans = [
    ["Basic", "$29/mo", "$1,500", "Accidental damage"],
    ["Standard", "$49/mo", "$5,000", "Damage, trip interruption"],
    ["Premium", "$89/mo", "$10,000", "Damage, interruption, liability"],
  ];

  return (
    <ScreenShell
      id="PM-INS"
      eyebrow="Property manager portal"
      title="Property Protection - Powered by InsuraGuest."
      copy="Insurance subscription UI that can remove guest damage deposits when provider integration is live."
    >
      <section className="product-section spec-card-grid spec-card-grid--three">
        {plans.map(([name, cost, coverage, included]) => (
          <Card className="plan-card" key={name}>
            <Badge tone={name === "Standard" ? "green" : "slate"}>{name}</Badge>
            <h3>{cost}</h3>
            <p>Coverage limit: {coverage}</p>
            <p>{included}</p>
            <Button>{name === "Standard" ? "Current plan" : "Subscribe"}</Button>
          </Card>
        ))}
        <p className="spec-disclosure">Integration subject to provider agreement - screen reflects final UI.</p>
      </section>
    </ScreenShell>
  );
}

export function PoliceDirectoryPage() {
  return (
    <ScreenShell
      id="OFC-DIR"
      eyebrow="Officer wellness"
      title="Police Directory - WELLNESS ACCESS REQUIRED."
      copy="Restricted officer availability directory. Officer names, contact details, and photos are never displayed."
    >
      <section className="product-section">
        <div className="warning-banner">
          <Lock size={18} /> This directory is restricted. All communications are through NestyStay platform only.
        </div>
        <DataTable
          columns={["Parish", "Badge ID", "Availability", "Request"]}
          rows={[
            ["St. James", "NST-OFC-1042", <Badge tone="green">Available</Badge>, <AppLink className={buttonClassName("outline")} href="/host/wellness/book">Request wellness visit</AppLink>],
            ["Kingston", "NST-OFC-2210", <Badge tone="blue">On duty</Badge>, <AppLink className={buttonClassName("outline")} href="/host/wellness/book">Request wellness visit</AppLink>],
            ["Portland", "NST-OFC-3188", <Badge tone="slate">Unavailable</Badge>, <Button disabled variant="ghost">Unavailable</Button>],
          ]}
        />
      </section>
    </ScreenShell>
  );
}

export function WellnessBookingPage() {
  const { properties } = useProperties();
  const [pending, setPending] = useState(false);

  useEffect(() => {
    if (!pending) return;
    const timer = window.setTimeout(() => setPending(false), 6000);
    return () => window.clearTimeout(timer);
  }, [pending]);

  return (
    <ScreenShell
      id="OFC-BOOK"
      eyebrow="Officer wellness"
      title="Wellness visit booking."
      copy="Host-side booking screen with property selector, visit type, officer badge IDs, pricing, and pending acceptance."
    >
      <section className="product-section management-layout">
        <form className="management-form">
          <div className="form-grid form-grid--two">
            <Field label="Property">
              <Select>
                {(properties.length ? properties : []).map((property) => (
                  <option key={property.id}>{property.title}</option>
                ))}
                {properties.length === 0 && <option>Coral Reef Sanctuary</option>}
              </Select>
            </Field>
            <Field label="Visit type">
              <Select>
                <option>Drive-by patrol - $25</option>
                <option>In-person guest ID check on arrival - $25</option>
                <option>Full property inspection - $45</option>
              </Select>
            </Field>
            <Field label="Date"><Input type="date" defaultValue="2026-08-01" /></Field>
            <Field label="Time"><Input type="time" defaultValue="10:30" /></Field>
            <Field label="Available officer badge" className="form-grid__full">
              <Select>
                <option>NST-OFC-1042 - St. James - Available</option>
                <option>NST-OFC-2210 - Kingston - Available</option>
              </Select>
            </Field>
          </div>
          <Button onClick={() => setPending(true)}>
            <ShieldCheck size={17} /> Request visit
          </Button>
        </form>
        <Card className="spec-panel">
          {pending ? (
            <>
              <Badge tone="sun">Pending officer acceptance</Badge>
              <h3>Visit request sent.</h3>
              <p>Countdown active while the officer accepts through the platform.</p>
              <div className="progress-bar"><span /></div>
            </>
          ) : (
            <>
              <Badge tone="green">Confirmation state</Badge>
              <h3>Accepted by NST-OFC-1042.</h3>
              <p>All communication remains mediated by NestyStay.</p>
            </>
          )}
        </Card>
      </section>
    </ScreenShell>
  );
}

export function BusinessDirectoryPage() {
  const businesses = [
    ["Marcia's Kitchen", "Restaurants", "Kingston", "8 AM - 9 PM", "Trusted"],
    ["Blue Lagoon Tours", "Tours", "Portland", "9 AM - 5 PM", "Featured"],
    ["Island Ride Co.", "Transport", "St. James", "24 hours", "Trusted"],
    ["Reef Lens Studio", "Photography", "Negril", "10 AM - 6 PM", "Free listing"],
  ];

  return (
    <ScreenShell
      id="DIR-BIZ"
      eyebrow="Service directories"
      title="Local business directory."
      copy="Verified hosts and guests can browse Jamaican services by category and parish."
    >
      <section className="product-section">
        <div className="search-panel spec-filter-bar">
          <Field label="Category"><Select><option>All categories</option><option>Restaurants</option><option>Tours</option></Select></Field>
          <Field label="Parish"><Select><option>All parishes</option><option>Kingston</option><option>St. James</option></Select></Field>
        </div>
        <div className="spec-card-grid spec-card-grid--three">
          {businesses.map(([name, category, parish, hours, badge]) => (
            <SpecCard
              action={{ label: "View details" }}
              copy={`${parish} - ${hours}. Free to join, with featured placement for Trusted Badge holders.`}
              icon={BriefcaseBusiness}
              key={name}
              meta={badge}
              title={`${name} - ${category}`}
            />
          ))}
        </div>
      </section>
    </ScreenShell>
  );
}

export function ProviderDashboardPage() {
  return (
    <ScreenShell
      id="DIR-PROV"
      eyebrow="Service directories"
      title="Provider self-management dashboard."
      copy="Service providers can manage profile details, services, availability, requests, messages, badges, and earnings."
    >
      <section className="product-section provider-layout">
        <Card className="settings-card">
          <UserCheck size={24} />
          <h2>Profile details</h2>
          <Field label="Business name"><Input defaultValue="Island Spark Electric" /></Field>
          <Field label="Coverage parishes"><Input defaultValue="Kingston, St. Andrew, St. Catherine" /></Field>
          <Field label="Bio"><Textarea defaultValue="Licensed electrician available for host maintenance requests." /></Field>
          <Button>Update profile</Button>
        </Card>
        <div className="spec-card-grid">
          <SpecCard icon={CalendarDays} title="Availability calendar" copy="Mark available and busy days for incoming host requests." />
          <SpecCard icon={MessageSquare} title="Connection requests" copy="Hosts can request work and message through the platform only." />
          <SpecCard icon={BadgeCheck} title="Trusted Badge" copy="Trusted Badge pricing is $120/year or $12/month. Renewal: Jan 1, 2027." />
          <SpecCard icon={CreditCard} title="Earnings" copy="$1,280 earned this month across 9 completed service jobs." />
        </div>
      </section>
    </ScreenShell>
  );
}

export function AdminKpiPage() {
  return (
    <ScreenShell
      id="ADM-KPI"
      eyebrow="Admin operations"
      title="Platform KPI trend charts."
      copy="Analytics dashboard with period selection, prior-period comparison, and per-chart CSV export."
    >
      <section className="product-section">
        <div className="segmented-control spec-tabs">
          {["7 days", "30 days", "90 days", "Year to date"].map((tab, index) => (
            <button className={index === 1 ? "is-active" : ""} key={tab} type="button">{tab}</button>
          ))}
        </div>
        <div className="chart-grid">
          {[
            ["New users by week", "bar"],
            ["Booking volume by week", "line"],
            ["Platform revenue by month", "bar"],
            ["Verification success rate", "donut"],
            ["Cancellation rate by property type", "bar"],
            ["Average response time by host badge", "line"],
          ].map(([title, type], index) => (
            <Card className={`chart-card chart-card--${type}`} key={title}>
              <div>
                <h3>{title}</h3>
                <Button variant="ghost"><Download size={16} /> Export CSV</Button>
              </div>
              <div className="fake-chart" style={{ "--chart-index": index } as React.CSSProperties} />
              <InlineLabel><input type="checkbox" /> Compare to prior period</InlineLabel>
            </Card>
          ))}
        </div>
      </section>
    </ScreenShell>
  );
}

export function AdminReportsPage() {
  const reports = [
    "User growth report",
    "Booking volume report",
    "Revenue and commission report",
    "Verification audit report",
    "Cancellation and refund report",
    "Compliance report for Jamaican tax authorities",
  ];

  return (
    <ScreenShell
      id="ADM-RPT"
      eyebrow="Admin operations"
      title="Reports and compliance exports."
      copy="Admin report generation with PDF/CSV outputs and scheduled email delivery controls."
    >
      <section className="product-section">
        <DataTable
          columns={["Report", "Date range", "Generate", "Downloads", "Schedule"]}
          rows={reports.map((report, index) => [
            report,
            <Input type="date" defaultValue="2026-07-21" />,
            <Button variant="outline">Generate report</Button>,
            <div className="button-row"><Button variant="ghost">PDF</Button><Button variant="ghost">CSV</Button></div>,
            <InlineLabel><input defaultChecked={index < 2} type="checkbox" /> Weekly/monthly email</InlineLabel>,
          ])}
        />
      </section>
    </ScreenShell>
  );
}

export function OfficerIdResetPage() {
  return (
    <ScreenShell
      id="ADM-OFC-RESET"
      eyebrow="Admin operations"
      title="Annual officer ID reset."
      copy="Officer IDs reset every January 1 with strict privacy controls."
    >
      <section className="product-section">
        <MetricStrip
          items={[
            { icon: TimerReset, label: "Next reset", value: "Jan 1" },
            { icon: ShieldCheck, label: "Officers", value: "1,240" },
            { icon: Lock, label: "No Override", value: "Active" },
            { icon: KeyRound, label: "Zero Trace", value: "Active" },
          ]}
        />
        <div className="warning-banner">
          <ShieldAlert size={18} /> No admin action can link old officer IDs to new IDs. Zero Trace is enforced.
        </div>
      </section>
    </ScreenShell>
  );
}

function ErrorTemplate({
  id,
  icon: Icon,
  title,
  copy,
  children,
}: {
  id: string;
  icon: LucideIcon;
  title: string;
  copy: string;
  children?: ReactNode;
}) {
  return (
    <ScreenShell id={id} eyebrow="Error state" title={title} copy={copy}>
      <section className="product-section product-section--center">
        <Card className="error-frame">
          <Icon size={38} />
          <h2>{title}</h2>
          <p>{copy}</p>
          {children}
        </Card>
      </section>
    </ScreenShell>
  );
}

export function SignInRequiredPage() {
  return (
    <ErrorTemplate
      copy="Log in to continue to this protected NestyStay area."
      icon={Lock}
      id="ERR-401"
      title="Sign in required."
    >
      <div className="button-row">
        <AppLink className={buttonClassName("dark")} href="/login">Log in</AppLink>
        <AppLink className={buttonClassName("outline")} href="/explore">Browse as guest</AppLink>
      </div>
      <AppLink className="inline-action" href="/register">Create an account</AppLink>
    </ErrorTemplate>
  );
}

export function AccessRestrictedPage() {
  return (
    <ErrorTemplate
      copy="Your account does not have permission to view this resource."
      icon={ShieldAlert}
      id="ERR-403"
      title="Access is restricted."
    >
      <div className="button-row">
        <AppLink className={buttonClassName("dark")} href="/">Return safely</AppLink>
        <AppLink className={buttonClassName("outline")} href="/profile">Support</AppLink>
      </div>
    </ErrorTemplate>
  );
}

export function NotFoundPage() {
  return (
    <ErrorTemplate
      copy="This page has drifted away."
      icon={Map}
      id="ERR-404"
      title="Dis page gone a sea."
    >
      <div className="search-panel spec-filter-bar">
        <Field label="Search destination">
          <Input placeholder="Try Montego Bay, Kingston, Portland" />
        </Field>
        <AppLink className={buttonClassName("dark")} href="/explore">
          Return to trusted stays
        </AppLink>
      </div>
    </ErrorTemplate>
  );
}

export function ServerErrorPage() {
  return (
    <ErrorTemplate
      copy="Something went wrong on our side. The team has been notified."
      icon={AlertTriangle}
      id="ERR-500"
      title="Server error."
    >
      <a className={buttonClassName("dark")} href="https://wa.me/17542482435" rel="noreferrer" target="_blank">
        Urgent? Message us on WhatsApp: 754-248-2435
      </a>
    </ErrorTemplate>
  );
}

export function NoFavoritesPage() {
  return (
    <ScreenShell
      id="ERR-NOFAV"
      eyebrow="Empty state"
      title="No favorites saved."
      copy="Wishlist tab empty state with navigation still visible."
    >
      <section className="product-section product-section--center">
        <EmptyState
          action={<AppLink className={buttonClassName("dark")} href="/explore">Explore NestyStay</AppLink>}
          copy="Save a stay to return to it here."
          title="No favorites saved."
        />
      </section>
    </ScreenShell>
  );
}

export function NoReservationsPage() {
  return (
    <ScreenShell
      id="ERR-NORES"
      eyebrow="Empty state"
      title="No reservations found."
      copy="Reservations empty state for no bookings or filters with no matches."
    >
      <section className="product-section product-section--center">
        <EmptyState
          action={
            <div className="button-row">
              <AppLink className={buttonClassName("dark")} href="/explore">Explore NestyStay</AppLink>
              <Button variant="outline">Clear filters</Button>
            </div>
          }
          copy="Explore verified stays or adjust the selected dates."
          title="No reservations found."
        />
      </section>
    </ScreenShell>
  );
}

export function DocumentMessagePage() {
  return (
    <ScreenShell
      id="MSG-DOC"
      eyebrow="Messaging system"
      title="Document sharing in messaging."
      copy="A document attachment displayed inside a chat thread with secure download status."
    >
      <section className="product-section message-thread">
        <div className="message-bubble">
          <strong>Arrival notes are ready.</strong>
          <p>Please download this before check-in.</p>
        </div>
        <div className="message-bubble message-bubble--document">
          <FileText size={28} />
          <div>
            <strong>Arrival_Guide_Azure_Cove.pdf</strong>
            <span>1.4 MB - Secure link expires 24 hours after download</span>
            <small>Downloaded and saved</small>
          </div>
          <Button variant="outline"><Download size={17} /> Download</Button>
        </div>
      </section>
    </ScreenShell>
  );
}
