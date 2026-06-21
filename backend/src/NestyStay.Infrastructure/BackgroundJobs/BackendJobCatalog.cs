namespace NestyStay.Infrastructure.BackgroundJobs;

public sealed record BackendJobDefinition(string Key, string Purpose, string DefaultCadence);

public static class BackendJobCatalog
{
    public static IReadOnlyList<BackendJobDefinition> Jobs { get; } =
    [
        new("booking-hold-expiry", "Reject expired PENDING bookings and release held dates.", "Every minute"),
        new("payment-schedule-collection", "Collect due split payments before check-in.", "Every 15 minutes"),
        new("message-retention-cleanup", "Delete messages after the 90-day retention window.", "Daily"),
        new("badge-renewal-reminders", "Queue badge renewal reminders before expiry.", "Daily"),
        new("officer-id-reset", "Reset active officer NestyStay IDs every January 1.", "Yearly"),
        new("wellness-escrow-auto-release", "Release wellness escrow 48 hours after report when owner is silent.", "Hourly"),
        new("directory-rating-enforcement", "Apply sponsorship/rating warning and removal rules.", "Daily"),
        new("document-retention-enforcement", "Apply configured document retention rules.", "Daily"),
        new("notification-retry", "Retry failed queued notifications.", "Every 5 minutes")
    ];
}
