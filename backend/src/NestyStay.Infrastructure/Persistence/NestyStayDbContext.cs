using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NestyStay.Domain.Access;
using NestyStay.Domain.Admin;
using NestyStay.Domain.AssociationManagement;
using NestyStay.Domain.Badges;
using NestyStay.Domain.Bookings;
using NestyStay.Domain.Common;
using NestyStay.Domain.Directories;
using NestyStay.Domain.Documents;
using NestyStay.Domain.Identity;
using NestyStay.Domain.Integrations;
using NestyStay.Domain.Messaging;
using NestyStay.Domain.Notifications;
using NestyStay.Domain.Payments;
using NestyStay.Domain.Pricing;
using NestyStay.Domain.Properties;
using NestyStay.Domain.PropertyManagement;
using NestyStay.Domain.Verification;
using NestyStay.Domain.Wellness;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Infrastructure.Persistence;

public sealed class NestyStayDbContext(DbContextOptions<NestyStayDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoles => Set<UserRoleAssignment>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<VerificationCheck> VerificationChecks => Set<VerificationCheck>();
    public DbSet<VerificationEvent> VerificationEvents => Set<VerificationEvent>();
    public DbSet<IdentityDocument> IdentityDocuments => Set<IdentityDocument>();

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyUnit> PropertyUnits => Set<PropertyUnit>();
    public DbSet<PropertyMedia> PropertyMedia => Set<PropertyMedia>();
    public DbSet<PropertyAvailability> PropertyAvailability => Set<PropertyAvailability>();
    public DbSet<PropertyPricingRule> PropertyPricingRules => Set<PropertyPricingRule>();
    public DbSet<PropertyFoundingBenefit> PropertyFoundingBenefits => Set<PropertyFoundingBenefit>();
    public DbSet<PropertyTransferRequest> PropertyTransferRequests => Set<PropertyTransferRequest>();

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingGuest> BookingGuests => Set<BookingGuest>();
    public DbSet<BookingPriceLine> BookingPriceLines => Set<BookingPriceLine>();
    public DbSet<BookingPaymentSchedule> BookingPaymentSchedules => Set<BookingPaymentSchedule>();
    public DbSet<BookingStatusEvent> BookingStatusEvents => Set<BookingStatusEvent>();
    public DbSet<BookingCancellation> BookingCancellations => Set<BookingCancellation>();
    public DbSet<BookingDispute> BookingDisputes => Set<BookingDispute>();

    public DbSet<PricebookEntry> PricebookEntries => Set<PricebookEntry>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignEnrollment> CampaignEnrollments => Set<CampaignEnrollment>();
    public DbSet<BadgeDefinition> BadgeDefinitions => Set<BadgeDefinition>();
    public DbSet<BadgeAssignment> BadgeAssignments => Set<BadgeAssignment>();
    public DbSet<BadgeRenewal> BadgeRenewals => Set<BadgeRenewal>();
    public DbSet<RatingPolicy> RatingPolicies => Set<RatingPolicy>();

    public DbSet<PaymentAccount> PaymentAccounts => Set<PaymentAccount>();
    public DbSet<PaymentIntentRecord> PaymentIntents => Set<PaymentIntentRecord>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<EscrowHold> EscrowHolds => Set<EscrowHold>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<ConversationThread> ConversationThreads => Set<ConversationThread>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<QrAccessCode> QrAccessCodes => Set<QrAccessCode>();
    public DbSet<QrScanLog> QrScanLogs => Set<QrScanLog>();
    public DbSet<VisitorLog> VisitorLogs => Set<VisitorLog>();

    public DbSet<ServiceProviderProfile> ServiceProviders => Set<ServiceProviderProfile>();
    public DbSet<ServiceProviderSponsorship> ServiceProviderSponsorships => Set<ServiceProviderSponsorship>();
    public DbSet<ServiceJob> ServiceJobs => Set<ServiceJob>();
    public DbSet<LocalBusiness> LocalBusinesses => Set<LocalBusiness>();
    public DbSet<DirectoryReview> DirectoryReviews => Set<DirectoryReview>();
    public DbSet<DirectoryCommission> DirectoryCommissions => Set<DirectoryCommission>();

    public DbSet<Officer> Officers => Set<Officer>();
    public DbSet<OfficerIdHistory> OfficerIdHistory => Set<OfficerIdHistory>();
    public DbSet<WellnessVisitTypeDefinition> WellnessVisitTypes => Set<WellnessVisitTypeDefinition>();
    public DbSet<WellnessVisit> WellnessVisits => Set<WellnessVisit>();
    public DbSet<WellnessReport> WellnessReports => Set<WellnessReport>();
    public DbSet<WellnessBadge> WellnessBadges => Set<WellnessBadge>();
    public DbSet<WellnessEscrowEvent> WellnessEscrowEvents => Set<WellnessEscrowEvent>();

    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityMembership> CommunityMemberships => Set<CommunityMembership>();
    public DbSet<OwnerUnit> OwnerUnits => Set<OwnerUnit>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<UtilityBill> UtilityBills => Set<UtilityBill>();
    public DbSet<ManagerStatement> ManagerStatements => Set<ManagerStatement>();
    public DbSet<ArrearsRecord> ArrearsRecords => Set<ArrearsRecord>();
    public DbSet<CommunityAnnouncement> CommunityAnnouncements => Set<CommunityAnnouncement>();
    public DbSet<StaffAssignment> StaffAssignments => Set<StaffAssignment>();

    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingDocument> MeetingDocuments => Set<MeetingDocument>();
    public DbSet<FinancialStatementVersion> FinancialStatementVersions => Set<FinancialStatementVersion>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<VoteResult> VoteResults => Set<VoteResult>();
    public DbSet<Proxy> Proxies => Set<Proxy>();
    public DbSet<BidOpening> BidOpenings => Set<BidOpening>();
    public DbSet<AssociationStoragePlan> AssociationStoragePlans => Set<AssociationStoragePlan>();
    public DbSet<DocumentRetentionRule> DocumentRetentionRules => Set<DocumentRetentionRule>();

    public DbSet<StorageObject> StorageObjects => Set<StorageObject>();
    public DbSet<DocumentVaultItem> DocumentVaultItems => Set<DocumentVaultItem>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationQueueItem> NotificationQueue => Set<NotificationQueueItem>();
    public DbSet<ProviderConfig> ProviderConfigs => Set<ProviderConfig>();
    public DbSet<ProviderEvent> ProviderEvents => Set<ProviderEvent>();
    public DbSet<IntegrationFailover> IntegrationFailovers => Set<IntegrationFailover>();

    public DbSet<MilestoneUser> MilestoneUsers => Set<MilestoneUser>();
    public DbSet<MilestoneTwoFactorChallenge> MilestoneTwoFactorChallenges => Set<MilestoneTwoFactorChallenge>();
    public DbSet<MilestoneProperty> MilestoneProperties => Set<MilestoneProperty>();
    public DbSet<MilestoneBooking> MilestoneBookings => Set<MilestoneBooking>();
    public DbSet<MilestonePricebookEntry> MilestonePricebookEntries => Set<MilestonePricebookEntry>();
    public DbSet<MilestoneBadgeDefinition> MilestoneBadgeDefinitions => Set<MilestoneBadgeDefinition>();
    public DbSet<MilestoneBadgeAssignment> MilestoneBadgeAssignments => Set<MilestoneBadgeAssignment>();
    public DbSet<MilestoneBadgeRenewal> MilestoneBadgeRenewals => Set<MilestoneBadgeRenewal>();
    public DbSet<MilestoneCampaign> MilestoneCampaigns => Set<MilestoneCampaign>();
    public DbSet<MilestoneCampaignEnrollment> MilestoneCampaignEnrollments => Set<MilestoneCampaignEnrollment>();
    public DbSet<MilestoneFoundingBenefit> MilestoneFoundingBenefits => Set<MilestoneFoundingBenefit>();
    public DbSet<MilestoneWellnessOfficer> MilestoneWellnessOfficers => Set<MilestoneWellnessOfficer>();
    public DbSet<MilestoneWellnessVisit> MilestoneWellnessVisits => Set<MilestoneWellnessVisit>();
    public DbSet<MilestoneWellnessReport> MilestoneWellnessReports => Set<MilestoneWellnessReport>();
    public DbSet<MilestoneWellnessPayout> MilestoneWellnessPayouts => Set<MilestoneWellnessPayout>();
    public DbSet<MilestoneAuthFlow> MilestoneAuthFlows => Set<MilestoneAuthFlow>();
    public DbSet<MilestoneRecoveryCode> MilestoneRecoveryCodes => Set<MilestoneRecoveryCode>();
    public DbSet<MilestonePublicContentPage> MilestonePublicContentPages => Set<MilestonePublicContentPage>();
    public DbSet<MilestoneContactRequest> MilestoneContactRequests => Set<MilestoneContactRequest>();
    public DbSet<MilestoneExperience> MilestoneExperiences => Set<MilestoneExperience>();
    public DbSet<MilestoneJournalArticle> MilestoneJournalArticles => Set<MilestoneJournalArticle>();
    public DbSet<MilestoneHostProfile> MilestoneHostProfiles => Set<MilestoneHostProfile>();
    public DbSet<MilestoneWishlistCollection> MilestoneWishlistCollections => Set<MilestoneWishlistCollection>();
    public DbSet<MilestoneWishlistItem> MilestoneWishlistItems => Set<MilestoneWishlistItem>();
    public DbSet<MilestoneTravelerPaymentMethod> MilestoneTravelerPaymentMethods => Set<MilestoneTravelerPaymentMethod>();
    public DbSet<MilestoneReview> MilestoneReviews => Set<MilestoneReview>();
    public DbSet<MilestoneTravelerNotification> MilestoneTravelerNotifications => Set<MilestoneTravelerNotification>();
    public DbSet<MilestoneConversation> MilestoneConversations => Set<MilestoneConversation>();
    public DbSet<MilestoneConversationParticipant> MilestoneConversationParticipants => Set<MilestoneConversationParticipant>();
    public DbSet<MilestoneMessage> MilestoneMessages => Set<MilestoneMessage>();
    public DbSet<MilestoneDirectoryProvider> MilestoneDirectoryProviders => Set<MilestoneDirectoryProvider>();
    public DbSet<MilestoneHostPricingRule> MilestoneHostPricingRules => Set<MilestoneHostPricingRule>();
    public DbSet<MilestoneHostPromotion> MilestoneHostPromotions => Set<MilestoneHostPromotion>();
    public DbSet<MilestoneAdminCase> MilestoneAdminCases => Set<MilestoneAdminCase>();
    public DbSet<MilestoneAuditEvent> MilestoneAuditEvents => Set<MilestoneAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ConfigureEntity(modelBuilder, entityType);
        }

        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(role => role.Key).IsUnique();
        modelBuilder.Entity<PricebookEntry>().HasIndex(entry => entry.Key).IsUnique();
        modelBuilder.Entity<ProviderConfig>().HasIndex(config => new { config.Kind, config.ProviderName }).IsUnique();
        modelBuilder.Entity<PropertyAvailability>().HasIndex(item => new { item.PropertyId, item.StartsOn, item.EndsOn });
        modelBuilder.Entity<Booking>().HasIndex(booking => new { booking.PropertyId, booking.CheckIn, booking.CheckOut });
        modelBuilder.Entity<QrAccessCode>().HasIndex(code => code.CodeHash).IsUnique();
        modelBuilder.Entity<Officer>().HasIndex(officer => officer.CurrentNestyStayId).IsUnique();
        modelBuilder.Entity<OfficerIdHistory>().HasIndex(item => new { item.OfficerId, item.Year }).IsUnique();
        modelBuilder.Entity<MilestoneUser>().HasIndex(user => user.NormalizedEmail).IsUnique();
        modelBuilder.Entity<MilestoneTwoFactorChallenge>().HasIndex(challenge => challenge.ChallengeId).IsUnique();
        modelBuilder.Entity<MilestoneProperty>().HasIndex(property => property.HostUserId);
        modelBuilder.Entity<MilestoneBooking>().HasIndex(booking => new { booking.PropertyId, booking.CheckIn, booking.CheckOut });
        modelBuilder.Entity<MilestoneBooking>().HasIndex(booking => booking.EkycTransactionId);
        modelBuilder.Entity<MilestonePricebookEntry>().HasIndex(entry => entry.Key).IsUnique();
        modelBuilder.Entity<MilestoneBadgeDefinition>().HasIndex(definition => new { definition.Level, definition.AppliesTo }).IsUnique();
        modelBuilder.Entity<MilestoneBadgeAssignment>().HasIndex(assignment => new { assignment.SubjectType, assignment.SubjectId, assignment.Level });
        modelBuilder.Entity<MilestoneBadgeRenewal>().HasIndex(renewal => new { renewal.BadgeAssignmentId, renewal.ReminderDueAt });
        modelBuilder.Entity<MilestoneCampaign>().HasIndex(campaign => campaign.Key).IsUnique();
        modelBuilder.Entity<MilestoneCampaignEnrollment>().HasIndex(enrollment => new { enrollment.CampaignKey, enrollment.SubjectType, enrollment.SubjectId }).IsUnique();
        modelBuilder.Entity<MilestoneFoundingBenefit>().HasIndex(benefit => benefit.PropertyId).IsUnique();
        modelBuilder.Entity<MilestoneWellnessOfficer>().HasIndex(officer => officer.BadgeNumber).IsUnique();
        modelBuilder.Entity<MilestoneWellnessOfficer>().HasIndex(officer => new { officer.Parish, officer.VerificationStatus, officer.AvailabilityStatus });
        modelBuilder.Entity<MilestoneWellnessVisit>().HasIndex(visit => new { visit.PropertyId, visit.ScheduledAt });
        modelBuilder.Entity<MilestoneWellnessVisit>().HasIndex(visit => new { visit.OfficerId, visit.ScheduledAt });
        modelBuilder.Entity<MilestoneWellnessVisit>().HasIndex(visit => visit.VisitStatus);
        modelBuilder.Entity<MilestoneWellnessVisit>().HasIndex(visit => visit.PaymentStatus);
        modelBuilder.Entity<MilestoneWellnessReport>().HasIndex(report => report.VisitId).IsUnique();
        modelBuilder.Entity<MilestoneWellnessPayout>().HasIndex(payout => payout.VisitId).IsUnique();
        modelBuilder.Entity<MilestoneWellnessPayout>().HasIndex(payout => payout.Status);
        modelBuilder.Entity<MilestoneAuthFlow>().HasIndex(flow => flow.TokenHash).IsUnique();
        modelBuilder.Entity<MilestoneAuthFlow>().HasIndex(flow => new { flow.UserId, flow.FlowType, flow.NormalizedDestination, flow.Status });
        modelBuilder.Entity<MilestoneAuthFlow>().HasIndex(flow => new { flow.RequestIpHash, flow.CreatedAt });
        modelBuilder.Entity<MilestoneRecoveryCode>().HasIndex(code => new { code.UserId, code.CodeHash }).IsUnique();
        modelBuilder.Entity<MilestonePublicContentPage>().HasIndex(page => page.Slug).IsUnique();
        modelBuilder.Entity<MilestoneExperience>().HasIndex(experience => experience.Slug).IsUnique();
        modelBuilder.Entity<MilestoneExperience>().HasIndex(experience => new { experience.Category, experience.Parish });
        modelBuilder.Entity<MilestoneJournalArticle>().HasIndex(article => article.Slug).IsUnique();
        modelBuilder.Entity<MilestoneHostProfile>().HasIndex(profile => profile.Slug).IsUnique();
        modelBuilder.Entity<MilestoneHostProfile>().HasIndex(profile => profile.HostUserId).IsUnique();
        modelBuilder.Entity<MilestoneWishlistCollection>().HasIndex(collection => new { collection.UserId, collection.Name }).IsUnique();
        modelBuilder.Entity<MilestoneWishlistItem>().HasIndex(item => new { item.UserId, item.PropertyId, item.CollectionId }).IsUnique();
        modelBuilder.Entity<MilestoneTravelerPaymentMethod>().HasIndex(method => new { method.UserId, method.IsDefault });
        modelBuilder.Entity<MilestoneReview>().HasIndex(review => new { review.UserId, review.PropertyId, review.BookingId });
        modelBuilder.Entity<MilestoneTravelerNotification>().HasIndex(notification => new { notification.UserId, notification.IsRead });
        modelBuilder.Entity<MilestoneConversationParticipant>().HasIndex(participant => new { participant.ConversationId, participant.UserId }).IsUnique();
        modelBuilder.Entity<MilestoneMessage>().HasIndex(message => new { message.ConversationId, message.SentAt });
        modelBuilder.Entity<MilestoneDirectoryProvider>().HasIndex(provider => provider.Slug).IsUnique();
        modelBuilder.Entity<MilestoneDirectoryProvider>().HasIndex(provider => new { provider.Kind, provider.Category, provider.Parish });
        modelBuilder.Entity<MilestoneHostPricingRule>().HasIndex(rule => new { rule.HostUserId, rule.PropertyId, rule.StartsOn, rule.EndsOn });
        modelBuilder.Entity<MilestoneHostPromotion>().HasIndex(promotion => new { promotion.HostUserId, promotion.PropertyId, promotion.IsActive });
        modelBuilder.Entity<MilestoneAdminCase>().HasIndex(adminCase => new { adminCase.CaseType, adminCase.Status });
        modelBuilder.Entity<MilestoneAuditEvent>().HasIndex(audit => new { audit.SubjectType, audit.SubjectId, audit.CreatedAt });

        NestyStaySeed.Apply(modelBuilder);
    }

    private static void ConfigureEntity(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        entityType.SetTableName(ToSnakeCase(entityType.ClrType.Name));

        if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
        {
            modelBuilder.Entity(entityType.ClrType).HasKey(nameof(BaseEntity.Id));
        }

        foreach (var property in entityType.GetProperties())
        {
            property.SetColumnName(ToSnakeCase(property.Name));

            if (property.ClrType == typeof(string))
            {
                property.SetMaxLength(property.Name.EndsWith("Json", StringComparison.Ordinal) ? 20000 : 512);
            }

            if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            if (property.ClrType.IsEnum)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion<string>()
                    .HasMaxLength(128);
            }

            if (property.Name.EndsWith("Json", StringComparison.Ordinal))
            {
                property.SetColumnType("jsonb");
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        var chars = new List<char>(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (char.IsUpper(current) && index > 0)
            {
                chars.Add('_');
            }

            chars.Add(char.ToLowerInvariant(current));
        }

        return new string(chars.ToArray());
    }
}
