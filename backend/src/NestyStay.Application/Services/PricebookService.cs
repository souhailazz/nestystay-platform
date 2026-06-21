using NestyStay.Domain;

namespace NestyStay.Application.Services;

public interface IPricebookService
{
    IReadOnlyList<PricebookItem> GetDefaultPricebook();
}

public sealed class PricebookService : IPricebookService
{
    public IReadOnlyList<PricebookItem> GetDefaultPricebook() =>
    [
        new("host-listing", "Host listing", 0m, "USD", "Always", "Hosts", true),
        new("host-commission", "Standard host commission", 3m, "PERCENT", "Per booking", "Hosts", true),
        new("guest-standard-fee-low", "Guest fee, large or long bookings", 8m, "PERCENT", "Per booking", "Guests", true),
        new("guest-standard-fee-mid", "Guest fee, mid-range bookings", 10m, "PERCENT", "Per booking", "Guests", true),
        new("guest-standard-fee-short", "Guest fee, single-night bookings", 12m, "PERCENT", "Per booking", "Guests", true),
        new("guest-ekyc-first-html", "First guest verification", 9.99m, "USD", "Per first check", "Guests", true),
        new("guest-ekyc-return-html", "Return guest verification", 4.99m, "USD", "Per repeat check", "Guests", true),
        new("guest-ekyc-host-paid-pdf", "Alibaba eKYC host-paid vendor cost", 0.14m, "USD", "Per booking", "Hosts", true),
        new("verified-host-standard-annual", "Verified host badge", 60m, "USD", "Annual", "Hosts", true),
        new("trusted-host-standard-annual", "Trusted host badge", 120m, "USD", "Annual", "Hosts", true),
        new("trusted-host-pdf-campaign", "Trusted host PDF campaign", 49m, "USD", "One time", "Hosts", true),
        new("founding-platinum-guest-flat", "Platinum founding guest fee", 29m, "USD", "Per booking lifetime", "Founding properties", true),
        new("founding-gold-guest-flat", "Gold founding guest fee", 36m, "USD", "Per booking lifetime", "Founding properties", true),
        new("founding-silver-guest-flat", "Silver founding guest fee", 45m, "USD", "Per booking lifetime", "Founding properties", true),
        new("association-pro", "Association Manager Pro", 39m, "USD", "Monthly", "Communities", true),
        new("officer-commission", "Wellness officer visit commission", 8m, "PERCENT", "Per completed visit", "Officers", true)
    ];
}
