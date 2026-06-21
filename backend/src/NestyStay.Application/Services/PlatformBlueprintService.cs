using NestyStay.Domain;

namespace NestyStay.Application.Services;

public interface IPlatformBlueprintService
{
    IReadOnlyList<PlatformModule> GetModules();
    IReadOnlyList<PortalDefinition> GetPortals();
    IReadOnlyList<VendorAdapterDescriptor> GetVendorAdapters();
}

public sealed class PlatformBlueprintService : IPlatformBlueprintService
{
    public IReadOnlyList<PlatformModule> GetModules() =>
    [
        new(
            "rental-platform",
            "Rental Platform",
            "Phase 1",
            "Listings, host onboarding, founding tiers, badges, search, reviews, insurance, and checkout.",
            [UserRole.Guest, UserRole.Host, UserRole.Admin],
            ["Property listings", "Founding counter", "Host verification", "Guest fees", "InsuraGuest toggle"]),
        new(
            "booking-manager",
            "Booking and Reservation Manager",
            "Phase 1",
            "Book Now popup with pending eKYC, split payments, cancellation policies, QR entry, and messaging.",
            [UserRole.Guest, UserRole.Host, UserRole.GateGuard],
            ["Pending verification", "Stripe checkout", "Payment splits", "QR gate logs", "90-day messaging"]),
        new(
            "badges-pricing",
            "Badges and Pricing Engine",
            "Phase 2",
            "Admin-managed pricebooks for badge fees, founding tiers, campaigns, commissions, and renewals.",
            [UserRole.Host, UserRole.ServiceProvider, UserRole.LocalBusiness, UserRole.Admin],
            ["Verified badge", "Trusted badge", "Wellness badge", "Founding discounts", "Annual renewals"]),
        new(
            "police-wellness",
            "Off-Duty Police Wellness",
            "Phase 3",
            "Active JCF-only officer onboarding, private IDs, eight visit types, escrow, reports, and wellness badges.",
            [UserRole.Host, UserRole.Officer, UserRole.Admin],
            ["Annual ID reset", "Privacy labels", "Photo reports", "Escrow release", "Visit menu"]),
        new(
            "directories",
            "Service and Local Business Directories",
            "Phase 4",
            "Custodians, trades, local businesses, police directory rules, sponsorship, reviews, and commissions.",
            [UserRole.Host, UserRole.ServiceProvider, UserRole.LocalBusiness, UserRole.Admin],
            ["Sponsorship", "Provider dashboards", "Rating enforcement", "Guest promotion", "Commission rules"]),
        new(
            "property-association-manager",
            "Property and Association Manager",
            "Phase 5",
            "Multi-owner management, invoicing, utilities, community board, meetings, voting, proxies, and document vault.",
            [UserRole.PropertyManager, UserRole.Owner, UserRole.AssociationExecutive, UserRole.Tenant, UserRole.Admin],
            ["Owner portal", "Arrears tracker", "Anonymous voting", "Proxy system", "7-year retention"])
    ];

    public IReadOnlyList<PortalDefinition> GetPortals() =>
    [
        new(UserRole.Guest, "Guest Portal", "Search, verify, book, pay, message, and access QR entry.", ["Explore", "Trips", "Verification", "Messages", "Payments"]),
        new(UserRole.Host, "Host Portal", "Manage properties, badges, bookings, pricing, guest verification, and wellness.", ["Properties", "Bookings", "Badges", "Wellness", "Payouts"]),
        new(UserRole.Owner, "Owner Portal", "View own unit balances, bookings, documents, notices, and payments.", ["Balance", "Bookings", "Documents", "Meetings", "Payments"]),
        new(UserRole.PropertyManager, "Property Manager Portal", "Manage communities, owners, units, invoices, utilities, vendors, and gates.", ["Communities", "Owners", "Invoices", "Maintenance", "Gate"]),
        new(UserRole.AssociationExecutive, "Association Executive Portal", "Run meetings, votes, proxies, statements, bid openings, and archives.", ["Meetings", "Voting", "Proxies", "Statements", "Archive"]),
        new(UserRole.Tenant, "Tenant Portal", "Access owner-approved notices, documents, visitor logs, and QR permissions.", ["Notices", "Documents", "Visitors", "QR Access"]),
        new(UserRole.ServiceProvider, "Service Provider Portal", "Manage custodian/trade profile, sponsorship, jobs, reviews, and payouts.", ["Profile", "Sponsor", "Jobs", "Reviews", "Payouts"]),
        new(UserRole.LocalBusiness, "Local Business Portal", "Manage listing, legal docs, promotions, reviews, and guest-facing offers.", ["Listing", "Documents", "Promotions", "Reviews"]),
        new(UserRole.Officer, "Officer Portal", "Manage private NestyStay ID, wellness jobs, reports, rates, and payouts.", ["Jobs", "Reports", "Rates", "Payouts"]),
        new(UserRole.GateGuard, "Gate Guard Portal", "Scan QR codes, log visitors, receive manager messages, and verify access.", ["Scanner", "Visitors", "Messages", "Access Logs"]),
        new(UserRole.Admin, "Admin Portal", "Configure pricebooks, vendors, roles, overrides, disputes, audits, and rollout controls.", ["Pricebook", "Vendors", "Users", "Disputes", "Audit"])
    ];

    public IReadOnlyList<VendorAdapterDescriptor> GetVendorAdapters() =>
    [
        new(ProviderKind.Ekyc, "Alibaba Cloud eKYC", ["Jumio", "Onfido"], "Days", "Subject checks retain provider, status, cost, and audit history."),
        new(ProviderKind.Payment, "Stripe", ["PayPal"], "Days", "Supports checkout, split schedules, escrow, subscriptions, refunds, and payouts."),
        new(ProviderKind.Storage, "Cloudflare R2", ["DigitalOcean Spaces", "Amazon S3"], "Hours", "One storage interface supports documents, reports, and future media."),
        new(ProviderKind.Notification, "AWS SES / Twilio / Firebase", ["Provider-specific fallbacks"], "Hours", "Email, SMS, and push events are queued behind one notification boundary."),
        new(ProviderKind.Insurance, "InsuraGuest", ["Manual plan configuration"], "Days", "Host dashboard add-on, policy metadata, and plan eligibility live behind an adapter.")
    ];
}
