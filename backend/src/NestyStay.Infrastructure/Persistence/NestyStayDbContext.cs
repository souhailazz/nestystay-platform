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
    private void ProtectPostedLedger()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<MilestoneManagerLedgerEntry>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Posted owner ledger entries are immutable. Post a reversal or adjustment instead.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectPostedLedger();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProtectPostedLedger();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

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
    public DbSet<MilestoneUserProfilePhoto> MilestoneUserProfilePhotos => Set<MilestoneUserProfilePhoto>();
    public DbSet<MilestoneTwoFactorChallenge> MilestoneTwoFactorChallenges => Set<MilestoneTwoFactorChallenge>();
    public DbSet<MilestoneUserSession> MilestoneUserSessions => Set<MilestoneUserSession>();
    public DbSet<MilestonePasskeyCredential> MilestonePasskeyCredentials => Set<MilestonePasskeyCredential>();
    public DbSet<MilestonePasskeyChallenge> MilestonePasskeyChallenges => Set<MilestonePasskeyChallenge>();
    public DbSet<MilestoneProperty> MilestoneProperties => Set<MilestoneProperty>();
    public DbSet<MilestonePropertyRevision> MilestonePropertyRevisions => Set<MilestonePropertyRevision>();
    public DbSet<MilestoneCalendarFeed> MilestoneCalendarFeeds => Set<MilestoneCalendarFeed>();
    public DbSet<MilestoneCalendarBlock> MilestoneCalendarBlocks => Set<MilestoneCalendarBlock>();
    public DbSet<MilestoneCalendarSyncEvent> MilestoneCalendarSyncEvents => Set<MilestoneCalendarSyncEvent>();
    public DbSet<MilestonePropertyPhoto> MilestonePropertyPhotos => Set<MilestonePropertyPhoto>();
    public DbSet<MilestoneBooking> MilestoneBookings => Set<MilestoneBooking>();
    public DbSet<MilestonePaymentAttempt> MilestonePaymentAttempts => Set<MilestonePaymentAttempt>();
    public DbSet<MilestoneBookingCreationRateLimit> MilestoneBookingCreationRateLimits => Set<MilestoneBookingCreationRateLimit>();
    public DbSet<MilestonePricebookEntry> MilestonePricebookEntries => Set<MilestonePricebookEntry>();
    public DbSet<MilestoneBadgeDefinition> MilestoneBadgeDefinitions => Set<MilestoneBadgeDefinition>();
    public DbSet<MilestoneBadgeAssignment> MilestoneBadgeAssignments => Set<MilestoneBadgeAssignment>();
    public DbSet<MilestoneBadgeRenewal> MilestoneBadgeRenewals => Set<MilestoneBadgeRenewal>();
    public DbSet<MilestoneCampaign> MilestoneCampaigns => Set<MilestoneCampaign>();
    public DbSet<MilestoneCampaignEnrollment> MilestoneCampaignEnrollments => Set<MilestoneCampaignEnrollment>();
    public DbSet<MilestoneFoundingBenefit> MilestoneFoundingBenefits => Set<MilestoneFoundingBenefit>();
    public DbSet<MilestoneWellnessOfficer> MilestoneWellnessOfficers => Set<MilestoneWellnessOfficer>();
    public DbSet<MilestoneWellnessSubscription> MilestoneWellnessSubscriptions => Set<MilestoneWellnessSubscription>();
    public DbSet<MilestoneWellnessVisit> MilestoneWellnessVisits => Set<MilestoneWellnessVisit>();
    public DbSet<MilestoneWellnessReport> MilestoneWellnessReports => Set<MilestoneWellnessReport>();
    public DbSet<MilestoneWellnessReportPhoto> MilestoneWellnessReportPhotos => Set<MilestoneWellnessReportPhoto>();
    public DbSet<MilestoneWellnessOfficerDocument> MilestoneWellnessOfficerDocuments => Set<MilestoneWellnessOfficerDocument>();
    public DbSet<MilestoneWellnessReportTemplate> MilestoneWellnessReportTemplates => Set<MilestoneWellnessReportTemplate>();
    public DbSet<MilestoneWellnessReportComment> MilestoneWellnessReportComments => Set<MilestoneWellnessReportComment>();
    public DbSet<MilestoneWellnessReportAcknowledgement> MilestoneWellnessReportAcknowledgements => Set<MilestoneWellnessReportAcknowledgement>();
    public DbSet<MilestoneWellnessFollowUpTask> MilestoneWellnessFollowUpTasks => Set<MilestoneWellnessFollowUpTask>();
    public DbSet<MilestoneWellnessPayout> MilestoneWellnessPayouts => Set<MilestoneWellnessPayout>();
    public DbSet<MilestoneWellnessPayoutDispute> MilestoneWellnessPayoutDisputes => Set<MilestoneWellnessPayoutDispute>();
    public DbSet<MilestoneAuthFlow> MilestoneAuthFlows => Set<MilestoneAuthFlow>();
    public DbSet<MilestoneRecoveryCode> MilestoneRecoveryCodes => Set<MilestoneRecoveryCode>();
    public DbSet<MilestonePublicContentPage> MilestonePublicContentPages => Set<MilestonePublicContentPage>();
    public DbSet<MilestoneContactRequest> MilestoneContactRequests => Set<MilestoneContactRequest>();
    public DbSet<MilestoneExperience> MilestoneExperiences => Set<MilestoneExperience>();
    public DbSet<MilestoneJournalArticle> MilestoneJournalArticles => Set<MilestoneJournalArticle>();
    public DbSet<MilestoneHostProfile> MilestoneHostProfiles => Set<MilestoneHostProfile>();
    public DbSet<MilestoneWishlistCollection> MilestoneWishlistCollections => Set<MilestoneWishlistCollection>();
    public DbSet<MilestoneWishlistItem> MilestoneWishlistItems => Set<MilestoneWishlistItem>();
    public DbSet<MilestoneTravelerRecommendationInteraction> MilestoneTravelerRecommendationInteractions => Set<MilestoneTravelerRecommendationInteraction>();
    public DbSet<MilestoneTravelerPreference> MilestoneTravelerPreferences => Set<MilestoneTravelerPreference>();
    public DbSet<MilestoneHostPayout> MilestoneHostPayouts => Set<MilestoneHostPayout>();
    public DbSet<MilestoneTravelerPaymentMethod> MilestoneTravelerPaymentMethods => Set<MilestoneTravelerPaymentMethod>();
    public DbSet<MilestoneReview> MilestoneReviews => Set<MilestoneReview>();
    public DbSet<MilestoneTravelerNotification> MilestoneTravelerNotifications => Set<MilestoneTravelerNotification>();
    public DbSet<MilestoneIdentityDocumentUpload> MilestoneIdentityDocumentUploads => Set<MilestoneIdentityDocumentUpload>();
    public DbSet<MilestoneConversation> MilestoneConversations => Set<MilestoneConversation>();
    public DbSet<MilestoneConversationParticipant> MilestoneConversationParticipants => Set<MilestoneConversationParticipant>();
    public DbSet<MilestoneMessage> MilestoneMessages => Set<MilestoneMessage>();
    public DbSet<MilestoneMessageAttachment> MilestoneMessageAttachments => Set<MilestoneMessageAttachment>();
    public DbSet<MilestoneDirectoryProvider> MilestoneDirectoryProviders => Set<MilestoneDirectoryProvider>();
    public DbSet<MilestoneDirectoryRecentView> MilestoneDirectoryRecentViews => Set<MilestoneDirectoryRecentView>();
    public DbSet<MilestoneDirectoryProviderDocument> MilestoneDirectoryProviderDocuments => Set<MilestoneDirectoryProviderDocument>();
    public DbSet<MilestoneDirectoryQuote> MilestoneDirectoryQuotes => Set<MilestoneDirectoryQuote>();
    public DbSet<MilestoneDirectoryReview> MilestoneDirectoryReviews => Set<MilestoneDirectoryReview>();
    public DbSet<MilestoneHostPricingRule> MilestoneHostPricingRules => Set<MilestoneHostPricingRule>();
    public DbSet<MilestoneHostPromotion> MilestoneHostPromotions => Set<MilestoneHostPromotion>();
    public DbSet<MilestoneAdminCase> MilestoneAdminCases => Set<MilestoneAdminCase>();
    public DbSet<MilestoneAdminCaseEvidence> MilestoneAdminCaseEvidenceUploads => Set<MilestoneAdminCaseEvidence>();
    public DbSet<MilestoneAuditEvent> MilestoneAuditEvents => Set<MilestoneAuditEvent>();
    public DbSet<MilestonePropertyManager> MilestonePropertyManagers => Set<MilestonePropertyManager>();
    public DbSet<MilestoneManagerOwner> MilestoneManagerOwners => Set<MilestoneManagerOwner>();
    public DbSet<MilestoneManagerProperty> MilestoneManagerProperties => Set<MilestoneManagerProperty>();
    public DbSet<MilestoneManagerPropertyAssignmentHistory> MilestoneManagerPropertyAssignmentHistory => Set<MilestoneManagerPropertyAssignmentHistory>();
    public DbSet<MilestoneManagerInvoice> MilestoneManagerInvoices => Set<MilestoneManagerInvoice>();
    public DbSet<MilestoneManagerInvoiceLine> MilestoneManagerInvoiceLines => Set<MilestoneManagerInvoiceLine>();
    public DbSet<MilestoneManagerPayment> MilestoneManagerPayments => Set<MilestoneManagerPayment>();
    public DbSet<MilestoneManagerPaymentMethod> MilestoneManagerPaymentMethods => Set<MilestoneManagerPaymentMethod>();
    public DbSet<MilestoneManagerPaymentAttempt> MilestoneManagerPaymentAttempts => Set<MilestoneManagerPaymentAttempt>();
    public DbSet<MilestoneManagerLedgerEntry> MilestoneManagerLedgerEntries => Set<MilestoneManagerLedgerEntry>();
    public DbSet<MilestoneManagerUtilityCharge> MilestoneManagerUtilityCharges => Set<MilestoneManagerUtilityCharge>();
    public DbSet<MilestoneManagerMeterReading> MilestoneManagerMeterReadings => Set<MilestoneManagerMeterReading>();
    public DbSet<MilestoneManagerUtilitySchedule> MilestoneManagerUtilitySchedules => Set<MilestoneManagerUtilitySchedule>();
    public DbSet<MilestoneManagerUtilityDispute> MilestoneManagerUtilityDisputes => Set<MilestoneManagerUtilityDispute>();
    public DbSet<MilestoneManagerVendor> MilestoneManagerVendors => Set<MilestoneManagerVendor>();
    public DbSet<MilestoneManagerVendorDocument> MilestoneManagerVendorDocuments => Set<MilestoneManagerVendorDocument>();
    public DbSet<MilestoneManagerMaintenance> MilestoneManagerMaintenances => Set<MilestoneManagerMaintenance>();
    public DbSet<MilestoneManagerMaintenanceActivity> MilestoneManagerMaintenanceActivities => Set<MilestoneManagerMaintenanceActivity>();
    public DbSet<MilestoneManagerMaintenanceAttachment> MilestoneManagerMaintenanceAttachments => Set<MilestoneManagerMaintenanceAttachment>();
    public DbSet<MilestoneManagerNotice> MilestoneManagerNotices => Set<MilestoneManagerNotice>();
    public DbSet<MilestoneManagerNoticeComment> MilestoneManagerNoticeComments => Set<MilestoneManagerNoticeComment>();
    public DbSet<MilestoneManagerNoticeAcknowledgement> MilestoneManagerNoticeAcknowledgements => Set<MilestoneManagerNoticeAcknowledgement>();
    public DbSet<MilestoneManagerProposal> MilestoneManagerProposals => Set<MilestoneManagerProposal>();
    public DbSet<MilestoneManagerProposalAttachment> MilestoneManagerProposalAttachments => Set<MilestoneManagerProposalAttachment>();
    public DbSet<MilestoneManagerProposalDiscussion> MilestoneManagerProposalDiscussions => Set<MilestoneManagerProposalDiscussion>();
    public DbSet<MilestoneManagerEligibleVoter> MilestoneManagerEligibleVoters => Set<MilestoneManagerEligibleVoter>();
    public DbSet<MilestoneManagerVote> MilestoneManagerVotes => Set<MilestoneManagerVote>();
    public DbSet<MilestoneManagerProxy> MilestoneManagerProxies => Set<MilestoneManagerProxy>();
    public DbSet<MilestoneManagerDocument> MilestoneManagerDocuments => Set<MilestoneManagerDocument>();
    public DbSet<MilestoneManagerDocumentVersion> MilestoneManagerDocumentVersions => Set<MilestoneManagerDocumentVersion>();
    public DbSet<MilestoneManagerDocumentAccessEvent> MilestoneManagerDocumentAccessEvents => Set<MilestoneManagerDocumentAccessEvent>();
    public DbSet<MilestoneManagerDocumentExport> MilestoneManagerDocumentExports => Set<MilestoneManagerDocumentExport>();
    public DbSet<MilestoneManagerGateMessage> MilestoneManagerGateMessages => Set<MilestoneManagerGateMessage>();
    public DbSet<MilestoneManagerGateDeliveryAttempt> MilestoneManagerGateDeliveryAttempts => Set<MilestoneManagerGateDeliveryAttempt>();
    public DbSet<MilestoneManagerQrAccess> MilestoneManagerQrAccesses => Set<MilestoneManagerQrAccess>();
    public DbSet<MilestoneManagerQrScan> MilestoneManagerQrScans => Set<MilestoneManagerQrScan>();
    public DbSet<MilestoneManagerSubscriptionEvent> MilestoneManagerSubscriptionEvents => Set<MilestoneManagerSubscriptionEvent>();
    public DbSet<MilestoneManagerInvitationEvent> MilestoneManagerInvitationEvents => Set<MilestoneManagerInvitationEvent>();
    public DbSet<MilestoneManagerOwnerVerification> MilestoneManagerOwnerVerifications => Set<MilestoneManagerOwnerVerification>();
    public DbSet<MilestoneManagerDashboardPreference> MilestoneManagerDashboardPreferences => Set<MilestoneManagerDashboardPreference>();
    public DbSet<MilestoneManagementAgreement> MilestoneManagementAgreements => Set<MilestoneManagementAgreement>();
    public DbSet<MilestoneManagementFeeRule> MilestoneManagementFeeRules => Set<MilestoneManagementFeeRule>();
    public DbSet<MilestoneOwnerPayout> MilestoneOwnerPayouts => Set<MilestoneOwnerPayout>();
    public DbSet<MilestoneOwnerApproval> MilestoneOwnerApprovals => Set<MilestoneOwnerApproval>();
    public DbSet<MilestoneManagerStaff> MilestoneManagerStaff => Set<MilestoneManagerStaff>();
    public DbSet<MilestoneManagerCalendarEvent> MilestoneManagerCalendarEvents => Set<MilestoneManagerCalendarEvent>();
    public DbSet<MilestoneWorkOrder> MilestoneWorkOrders => Set<MilestoneWorkOrder>();
    public DbSet<MilestoneCleaningTask> MilestoneCleaningTasks => Set<MilestoneCleaningTask>();
    public DbSet<MilestoneInspection> MilestoneInspections => Set<MilestoneInspection>();

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
        modelBuilder.Entity<ProviderEvent>().HasIndex(providerEvent => new { providerEvent.Kind, providerEvent.ProviderName, providerEvent.EventId }).IsUnique();
        modelBuilder.Entity<PropertyAvailability>().HasIndex(item => new { item.PropertyId, item.StartsOn, item.EndsOn });
        modelBuilder.Entity<Booking>().HasIndex(booking => new { booking.PropertyId, booking.CheckIn, booking.CheckOut });
        modelBuilder.Entity<QrAccessCode>().HasIndex(code => code.CodeHash).IsUnique();
        modelBuilder.Entity<QrAccessCode>().HasIndex(code => new { code.BookingId, code.IsRevoked, code.ExpiresAt });
        modelBuilder.Entity<QrAccessCode>().HasIndex(code => new { code.PropertyId, code.ExpiresAt });
        modelBuilder.Entity<MilestoneWellnessSubscription>().HasIndex(item => new { item.HostUserId, item.Status, item.CurrentPeriodEnd });
        modelBuilder.Entity<QrScanLog>().HasIndex(log => new { log.QrAccessCodeId, log.ScannedAt });
        modelBuilder.Entity<Officer>().HasIndex(officer => officer.CurrentNestyStayId).IsUnique();
        modelBuilder.Entity<OfficerIdHistory>().HasIndex(item => new { item.OfficerId, item.Year }).IsUnique();
        modelBuilder.Entity<MilestoneUser>().HasIndex(user => user.NormalizedEmail).IsUnique();
        modelBuilder.Entity<MilestoneUser>().Property(user => user.IsTwoFactorEnabled).HasDefaultValue(true);
        modelBuilder.Entity<MilestoneUser>().Property(user => user.Status).HasDefaultValue("Active");
        modelBuilder.Entity<MilestoneUser>().Property(user => user.AdminPermissionsJson).HasDefaultValue("[]");
        modelBuilder.Entity<MilestoneUser>().HasIndex(user => user.Status);
        modelBuilder.Entity<MilestoneUserProfilePhoto>().HasIndex(photo => photo.ObjectKey).IsUnique();
        modelBuilder.Entity<MilestoneUserProfilePhoto>().HasIndex(photo => new { photo.UserId, photo.Status, photo.IsCurrent });
        modelBuilder.Entity<MilestoneTwoFactorChallenge>().HasIndex(challenge => challenge.ChallengeId).IsUnique();
        modelBuilder.Entity<MilestoneProperty>().HasIndex(property => property.HostUserId);
        modelBuilder.Entity<MilestonePropertyPhoto>().HasIndex(photo => photo.ObjectKey).IsUnique();
        modelBuilder.Entity<MilestonePropertyPhoto>().HasIndex(photo => new { photo.PropertyId, photo.HostUserId, photo.Status });
        modelBuilder.Entity<MilestoneBooking>().HasIndex(booking => new { booking.PropertyId, booking.CheckIn, booking.CheckOut });
        modelBuilder.Entity<MilestoneBooking>().HasIndex(booking => booking.GuestUserId);
        modelBuilder.Entity<MilestoneBooking>().HasIndex(booking => booking.HostUserId);
        modelBuilder.Entity<MilestoneBooking>().HasIndex(booking => booking.EkycTransactionId);
        modelBuilder.Entity<MilestonePaymentAttempt>().HasIndex(attempt => attempt.IdempotencyKey).IsUnique();
        modelBuilder.Entity<MilestonePaymentAttempt>().HasIndex(attempt => new { attempt.BookingId, attempt.Operation, attempt.Status });
        modelBuilder.Entity<MilestoneBookingCreationRateLimit>().HasIndex(limit => limit.GuestUserId).IsUnique();
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
        modelBuilder.Entity<MilestoneWellnessVisit>().HasIndex(visit => new { visit.HostUserId, visit.ScheduledAt });
        modelBuilder.Entity<MilestoneWellnessVisit>().HasIndex(visit => visit.VisitStatus);
        modelBuilder.Entity<MilestoneWellnessVisit>().HasIndex(visit => visit.PaymentStatus);
        modelBuilder.Entity<MilestoneWellnessReport>().HasIndex(report => report.VisitId).IsUnique();
        modelBuilder.Entity<MilestoneWellnessReportPhoto>().HasIndex(photo => photo.ObjectKey).IsUnique();
        modelBuilder.Entity<MilestoneWellnessReportPhoto>().HasIndex(photo => new { photo.VisitId, photo.OfficerId, photo.Status });
        modelBuilder.Entity<MilestoneWellnessReportPhoto>().HasIndex(photo => photo.ReportId);
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
        modelBuilder.Entity<MilestoneTravelerRecommendationInteraction>().HasIndex(item => new { item.UserId, item.PropertyId, item.Action, item.CreatedAt });
        modelBuilder.Entity<MilestoneTravelerRecommendationInteraction>().HasIndex(item => new { item.UserId, item.PropertyId, item.Action }).IsUnique(false);
        modelBuilder.Entity<MilestoneTravelerPreference>().HasIndex(item => item.UserId).IsUnique();
        modelBuilder.Entity<MilestoneHostPayout>().HasIndex(item => item.BookingId).IsUnique();
        modelBuilder.Entity<MilestoneHostPayout>().HasIndex(item => new { item.HostUserId, item.Status, item.CreatedAt });
        modelBuilder.Entity<MilestoneTravelerPaymentMethod>().HasIndex(method => new { method.UserId, method.IsDefault });
        modelBuilder.Entity<MilestoneTravelerPaymentMethod>().HasIndex(method => method.ProviderPaymentMethodReference);
        modelBuilder.Entity<MilestoneReview>().HasIndex(review => new { review.UserId, review.PropertyId, review.BookingId });
        modelBuilder.Entity<MilestoneTravelerNotification>().HasIndex(notification => new { notification.UserId, notification.IsRead });
        modelBuilder.Entity<MilestonePropertyRevision>().HasIndex(revision => new { revision.PropertyId, revision.Version }).IsUnique();
        modelBuilder.Entity<MilestonePropertyRevision>().HasIndex(revision => new { revision.PropertyId, revision.CreatedAt });
        modelBuilder.Entity<MilestoneCalendarFeed>().HasIndex(feed => new { feed.PropertyId, feed.HostUserId, feed.FeedUrl }).IsUnique();
        modelBuilder.Entity<MilestoneCalendarBlock>().HasIndex(block => new { block.FeedId, block.ExternalId }).IsUnique();
        modelBuilder.Entity<MilestoneCalendarSyncEvent>().HasIndex(item => new { item.FeedId, item.StartedAt });
        modelBuilder.Entity<MilestoneUserSession>().HasIndex(session => new { session.UserId, session.TokenIdHash }).IsUnique();
        modelBuilder.Entity<MilestoneUserSession>().HasIndex(session => new { session.UserId, session.LastUsedAt });
        modelBuilder.Entity<MilestonePasskeyCredential>().HasIndex(credential => credential.CredentialIdHash).IsUnique();
        modelBuilder.Entity<MilestonePasskeyCredential>().HasIndex(credential => new { credential.UserId, credential.RevokedAt });
        modelBuilder.Entity<MilestonePasskeyChallenge>().HasIndex(challenge => challenge.ChallengeId).IsUnique();
        modelBuilder.Entity<NotificationQueueItem>().HasIndex(item => new { item.Channel, item.DeliveryStatus, item.NextAttemptAt });
        modelBuilder.Entity<NotificationQueueItem>().HasIndex(item => item.IdempotencyKey).IsUnique();
        modelBuilder.Entity<NotificationQueueItem>().Property(item => item.Body).HasMaxLength(100_000);
        modelBuilder.Entity<NotificationQueueItem>().Property(item => item.TextBody).HasMaxLength(100_000);
        modelBuilder.Entity<NotificationQueueItem>().Property(item => item.HtmlBody).HasMaxLength(100_000);
        modelBuilder.Entity<NotificationQueueItem>().Property(item => item.ReplyToEmail).HasMaxLength(320);
        modelBuilder.Entity<NotificationQueueItem>().Property(item => item.ReplyToName).HasMaxLength(256);
        modelBuilder.Entity<MilestoneIdentityDocumentUpload>().HasIndex(document => document.ObjectKey).IsUnique();
        modelBuilder.Entity<MilestoneIdentityDocumentUpload>().HasIndex(document => new { document.UserId, document.Status });
        modelBuilder.Entity<MilestoneIdentityDocumentUpload>().HasIndex(document => document.IdentityDocumentId);
        modelBuilder.Entity<MilestoneConversationParticipant>().HasIndex(participant => new { participant.ConversationId, participant.UserId }).IsUnique();
        modelBuilder.Entity<MilestoneConversationParticipant>().HasIndex(participant => participant.UserId);
        modelBuilder.Entity<MilestoneMessage>().HasIndex(message => new { message.ConversationId, message.SentAt });
        modelBuilder.Entity<MilestoneMessageAttachment>().HasIndex(attachment => attachment.ObjectKey).IsUnique();
        modelBuilder.Entity<MilestoneMessageAttachment>().HasIndex(attachment => new { attachment.ConversationId, attachment.OwnerUserId, attachment.Status });
        modelBuilder.Entity<MilestoneMessageAttachment>().HasIndex(attachment => attachment.MessageId);
        modelBuilder.Entity<MilestoneDirectoryProvider>().HasIndex(provider => provider.Slug).IsUnique();
        modelBuilder.Entity<MilestoneDirectoryProvider>().HasIndex(provider => provider.OwnerUserId);
        modelBuilder.Entity<MilestoneDirectoryProvider>().HasIndex(provider => new { provider.Kind, provider.Category, provider.Parish });
        modelBuilder.Entity<MilestoneDirectoryProvider>().HasIndex(provider => new { provider.Status, provider.VerificationStatus, provider.IsActive });
        modelBuilder.Entity<MilestoneDirectoryRecentView>().HasIndex(view => new { view.UserId, view.ProviderId }).IsUnique();
        modelBuilder.Entity<MilestoneDirectoryRecentView>().HasIndex(view => new { view.UserId, view.ViewedAt });
        modelBuilder.Entity<MilestoneDirectoryProviderDocument>().HasIndex(document => new { document.ProviderId, document.OwnerUserId, document.Status });
        modelBuilder.Entity<MilestoneDirectoryProviderDocument>().HasIndex(document => document.ObjectKey).IsUnique();
        modelBuilder.Entity<MilestoneDirectoryQuote>().HasIndex(quote => new { quote.ProviderId, quote.Status, quote.CreatedAt });
        modelBuilder.Entity<MilestoneDirectoryQuote>().HasIndex(quote => new { quote.RequesterUserId, quote.CreatedAt });
        modelBuilder.Entity<MilestoneDirectoryReview>().HasIndex(review => new { review.ProviderId, review.ReviewerUserId }).IsUnique();
        modelBuilder.Entity<MilestoneDirectoryReview>().HasIndex(review => new { review.ProviderId, review.Status, review.CreatedAt });
        modelBuilder.Entity<MilestoneHostPricingRule>().HasIndex(rule => new { rule.HostUserId, rule.PropertyId, rule.StartsOn, rule.EndsOn });
        modelBuilder.Entity<MilestoneHostPromotion>().HasIndex(promotion => new { promotion.HostUserId, promotion.PropertyId, promotion.IsActive });
        modelBuilder.Entity<MilestoneAdminCase>().HasIndex(adminCase => new { adminCase.CaseType, adminCase.Status });
        modelBuilder.Entity<MilestoneAdminCaseEvidence>().HasIndex(evidence => evidence.ObjectKey).IsUnique();
        modelBuilder.Entity<MilestoneAdminCaseEvidence>().HasIndex(evidence => new { evidence.CaseId, evidence.Status });
        modelBuilder.Entity<MilestoneAuditEvent>().HasIndex(audit => new { audit.SubjectType, audit.SubjectId, audit.CreatedAt });
        modelBuilder.Entity<MilestonePropertyManager>().Property(item => item.AutoRenew).HasDefaultValue(true);
        modelBuilder.Entity<MilestonePropertyManager>().Property(item => item.BillingProviderStatus).HasDefaultValue("LOCAL_TEST");
        modelBuilder.Entity<MilestonePropertyManager>().HasIndex(item => item.ManagerUserId).IsUnique();
        modelBuilder.Entity<MilestoneManagerOwner>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId }).IsUnique();
        modelBuilder.Entity<MilestoneManagerProperty>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId });
        modelBuilder.Entity<MilestoneManagerPropertyAssignmentHistory>().HasIndex(item => new { item.ManagerUserId, item.PropertyId, item.ChangedAt });
        modelBuilder.Entity<MilestoneManagerInvoice>().HasIndex(item => new { item.ManagerUserId, item.InvoiceNumber }).IsUnique();
        modelBuilder.Entity<MilestoneManagerInvoice>().HasIndex(item => new { item.OwnerUserId, item.DueDate });
        modelBuilder.Entity<MilestoneManagerInvoiceLine>().HasIndex(item => item.InvoiceId);
        modelBuilder.Entity<MilestoneManagerPayment>().HasIndex(item => new { item.ManagerUserId, item.IdempotencyKey }).IsUnique();
        modelBuilder.Entity<MilestoneManagerPayment>().HasIndex(item => item.InvoiceId);
        modelBuilder.Entity<MilestoneManagerPaymentMethod>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.ProviderReference }).IsUnique();
        modelBuilder.Entity<MilestoneManagerPaymentAttempt>().HasIndex(item => new { item.PaymentId, item.AttemptNumber }).IsUnique();
        modelBuilder.Entity<MilestoneManagerLedgerEntry>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.OccurredOn });
        modelBuilder.Entity<MilestoneManagerUtilityCharge>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.PropertyId, item.BillingPeriod });
        modelBuilder.Entity<MilestoneManagerMeterReading>().HasIndex(item => new { item.ManagerUserId, item.PropertyId, item.UtilityType, item.BillingPeriod }).IsUnique();
        modelBuilder.Entity<MilestoneManagerUtilitySchedule>().HasIndex(item => new { item.ManagerUserId, item.PropertyId, item.UtilityType, item.IsActive });
        modelBuilder.Entity<MilestoneManagerUtilityDispute>().HasIndex(item => new { item.ManagerUserId, item.Status });
        modelBuilder.Entity<MilestoneManagerMaintenance>().HasIndex(item => new { item.ManagerUserId, item.Status });
        modelBuilder.Entity<MilestoneManagerMaintenanceActivity>().HasIndex(item => new { item.MaintenanceId, item.CreatedAt });
        modelBuilder.Entity<MilestoneManagerMaintenanceAttachment>().HasIndex(item => new { item.MaintenanceId, item.CreatedAt });
        modelBuilder.Entity<MilestoneManagerNotice>().HasIndex(item => new { item.ManagerUserId, item.CommunityId, item.PublishAt });
        modelBuilder.Entity<MilestoneManagerNoticeComment>().HasIndex(item => new { item.NoticeId, item.CreatedAt });
        modelBuilder.Entity<MilestoneManagerNoticeAcknowledgement>().HasIndex(item => new { item.NoticeId, item.OwnerUserId }).IsUnique();
        modelBuilder.Entity<MilestoneManagerProposal>().HasIndex(item => new { item.ManagerUserId, item.Status });
        modelBuilder.Entity<MilestoneManagerProposalAttachment>().HasIndex(item => item.ProposalId);
        modelBuilder.Entity<MilestoneManagerProposalDiscussion>().HasIndex(item => new { item.ProposalId, item.CreatedAt });
        modelBuilder.Entity<MilestoneManagerEligibleVoter>().HasIndex(item => new { item.ProposalId, item.OwnerUserId }).IsUnique();
        modelBuilder.Entity<MilestoneManagerVote>().HasIndex(item => item.ProposalId);
        modelBuilder.Entity<MilestoneManagerProxy>().HasIndex(item => new { item.ProposalId, item.OwnerUserId }).IsUnique();
        modelBuilder.Entity<MilestoneManagerDocument>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.PropertyId });
        modelBuilder.Entity<MilestoneManagerDocumentVersion>().HasIndex(item => new { item.DocumentId, item.Version }).IsUnique();
        modelBuilder.Entity<MilestoneManagerDocumentAccessEvent>().HasIndex(item => new { item.DocumentId, item.CreatedAt });
        modelBuilder.Entity<MilestoneManagerDocumentExport>().HasIndex(item => new { item.ManagerUserId, item.Status, item.CreatedAt });
        modelBuilder.Entity<MilestoneManagerGateMessage>().HasIndex(item => new { item.ManagerUserId, item.IdempotencyKey }).IsUnique();
        modelBuilder.Entity<MilestoneManagerGateDeliveryAttempt>().HasIndex(item => new { item.GateMessageId, item.AttemptNumber }).IsUnique();
        modelBuilder.Entity<MilestoneManagerQrAccess>().HasIndex(item => item.TokenHash).IsUnique();
        modelBuilder.Entity<MilestoneManagerQrScan>().HasIndex(item => new { item.QrAccessId, item.ScannedAt });
        modelBuilder.Entity<MilestoneManagerSubscriptionEvent>().HasIndex(item => new { item.ManagerUserId, item.EffectiveAt });
        modelBuilder.Entity<MilestoneManagerInvitationEvent>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.CreatedAt });
        modelBuilder.Entity<MilestoneManagerOwnerVerification>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.Requirement }).IsUnique();
        modelBuilder.Entity<MilestoneManagerDashboardPreference>().HasIndex(item => item.ManagerUserId).IsUnique();
        modelBuilder.Entity<MilestoneManagementAgreement>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.PropertyId, item.Version }).IsUnique();
        modelBuilder.Entity<MilestoneManagementFeeRule>().HasIndex(item => new { item.ManagerUserId, item.PropertyId, item.EffectiveFrom });
        modelBuilder.Entity<MilestoneOwnerPayout>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.PeriodFrom, item.PeriodTo }).IsUnique();
        modelBuilder.Entity<MilestoneOwnerApproval>().HasIndex(item => new { item.ManagerUserId, item.OwnerUserId, item.Status });
        modelBuilder.Entity<MilestoneManagerStaff>().HasIndex(item => new { item.ManagerUserId, item.StaffUserId }).IsUnique();
        modelBuilder.Entity<MilestoneManagerCalendarEvent>().HasIndex(item => new { item.ManagerUserId, item.StartsAt, item.EndsAt });
        modelBuilder.Entity<MilestoneWorkOrder>().HasIndex(item => new { item.ManagerUserId, item.WorkOrderNumber }).IsUnique();
        modelBuilder.Entity<MilestoneCleaningTask>().HasIndex(item => new { item.ManagerUserId, item.PropertyId, item.DueAt });
        modelBuilder.Entity<MilestoneInspection>().HasIndex(item => new { item.ManagerUserId, item.PropertyId, item.CreatedAt });

        // Phase 5 relationship constraints.  These are intentionally explicit
        // because the milestone entities use scalar ids (rather than navigation
        // properties) to keep the authorization surface small.  Every relation
        // was reviewed against the live database orphan inventory before being
        // added; contextual QR scan property ids are not constrained because
        // they represent the guard's supplied comparison value, not ownership.
        static void UserLink<TEntity>(ModelBuilder builder, string key) where TEntity : class => builder.Entity<TEntity>().HasOne<MilestoneUser>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.Restrict);
        static void OptionalUserLink<TEntity>(ModelBuilder builder, string key) where TEntity : class => builder.Entity<TEntity>().HasOne<MilestoneUser>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.Restrict);
        static void PropertyLink<TEntity>(ModelBuilder builder, string key) where TEntity : class => builder.Entity<TEntity>().HasOne<MilestoneManagerProperty>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.Restrict);
        static void OptionalPropertyLink<TEntity>(ModelBuilder builder, string key) where TEntity : class => builder.Entity<TEntity>().HasOne<MilestoneManagerProperty>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.Restrict);
        static void OptionalVendorLink<TEntity>(ModelBuilder builder, string key) where TEntity : class => builder.Entity<TEntity>().HasOne<MilestoneManagerVendor>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.Restrict);
        static void InvoiceLink<TEntity>(ModelBuilder builder, string key) where TEntity : class => builder.Entity<TEntity>().HasOne<MilestoneManagerInvoice>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.Restrict);
        static void OptionalInvoiceLink<TEntity>(ModelBuilder builder, string key) where TEntity : class => builder.Entity<TEntity>().HasOne<MilestoneManagerInvoice>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.Restrict);

        UserLink<MilestonePropertyManager>(modelBuilder, nameof(MilestonePropertyManager.ManagerUserId));
        UserLink<MilestoneUserSession>(modelBuilder, nameof(MilestoneUserSession.UserId));
        UserLink<MilestonePasskeyCredential>(modelBuilder, nameof(MilestonePasskeyCredential.UserId));
        UserLink<MilestoneTravelerRecommendationInteraction>(modelBuilder, nameof(MilestoneTravelerRecommendationInteraction.UserId));
        UserLink<MilestoneTravelerPreference>(modelBuilder, nameof(MilestoneTravelerPreference.UserId));
        UserLink<MilestoneHostPayout>(modelBuilder, nameof(MilestoneHostPayout.HostUserId));
        modelBuilder.Entity<MilestonePasskeyChallenge>().HasOne<MilestoneUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MilestoneCalendarSyncEvent>().HasOne<MilestoneCalendarFeed>().WithMany().HasForeignKey(x => x.FeedId).OnDelete(DeleteBehavior.Restrict);
        OptionalUserLink<MilestoneDirectoryProvider>(modelBuilder, nameof(MilestoneDirectoryProvider.OwnerUserId));
        modelBuilder.Entity<MilestoneDirectoryQuote>().HasOne<MilestoneDirectoryProvider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        UserLink<MilestoneDirectoryQuote>(modelBuilder, nameof(MilestoneDirectoryQuote.RequesterUserId));
        modelBuilder.Entity<MilestoneDirectoryReview>().HasOne<MilestoneDirectoryProvider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        UserLink<MilestoneDirectoryReview>(modelBuilder, nameof(MilestoneDirectoryReview.ReviewerUserId));
        UserLink<MilestoneManagerOwner>(modelBuilder, nameof(MilestoneManagerOwner.ManagerUserId)); UserLink<MilestoneManagerOwner>(modelBuilder, nameof(MilestoneManagerOwner.OwnerUserId));
        UserLink<MilestoneManagerProperty>(modelBuilder, nameof(MilestoneManagerProperty.ManagerUserId)); UserLink<MilestoneManagerProperty>(modelBuilder, nameof(MilestoneManagerProperty.OwnerUserId));
        UserLink<MilestoneManagerInvoice>(modelBuilder, nameof(MilestoneManagerInvoice.ManagerUserId)); UserLink<MilestoneManagerInvoice>(modelBuilder, nameof(MilestoneManagerInvoice.OwnerUserId));
        OptionalPropertyLink<MilestoneManagerInvoice>(modelBuilder, nameof(MilestoneManagerInvoice.PropertyId));
        InvoiceLink<MilestoneManagerInvoiceLine>(modelBuilder, nameof(MilestoneManagerInvoiceLine.InvoiceId));
        UserLink<MilestoneManagerPayment>(modelBuilder, nameof(MilestoneManagerPayment.ManagerUserId)); UserLink<MilestoneManagerPayment>(modelBuilder, nameof(MilestoneManagerPayment.OwnerUserId));
        InvoiceLink<MilestoneManagerPayment>(modelBuilder, nameof(MilestoneManagerPayment.InvoiceId));
        UserLink<MilestoneManagerLedgerEntry>(modelBuilder, nameof(MilestoneManagerLedgerEntry.ManagerUserId)); UserLink<MilestoneManagerLedgerEntry>(modelBuilder, nameof(MilestoneManagerLedgerEntry.OwnerUserId));
        OptionalPropertyLink<MilestoneManagerLedgerEntry>(modelBuilder, nameof(MilestoneManagerLedgerEntry.PropertyId)); OptionalInvoiceLink<MilestoneManagerLedgerEntry>(modelBuilder, nameof(MilestoneManagerLedgerEntry.InvoiceId));
        UserLink<MilestoneManagerUtilityCharge>(modelBuilder, nameof(MilestoneManagerUtilityCharge.ManagerUserId)); UserLink<MilestoneManagerUtilityCharge>(modelBuilder, nameof(MilestoneManagerUtilityCharge.OwnerUserId));
        PropertyLink<MilestoneManagerUtilityCharge>(modelBuilder, nameof(MilestoneManagerUtilityCharge.PropertyId)); OptionalInvoiceLink<MilestoneManagerUtilityCharge>(modelBuilder, nameof(MilestoneManagerUtilityCharge.InvoiceId));
        UserLink<MilestoneManagerVendor>(modelBuilder, nameof(MilestoneManagerVendor.ManagerUserId));
        UserLink<MilestoneManagerMaintenance>(modelBuilder, nameof(MilestoneManagerMaintenance.ManagerUserId)); UserLink<MilestoneManagerMaintenance>(modelBuilder, nameof(MilestoneManagerMaintenance.OwnerUserId));
        PropertyLink<MilestoneManagerMaintenance>(modelBuilder, nameof(MilestoneManagerMaintenance.PropertyId)); OptionalVendorLink<MilestoneManagerMaintenance>(modelBuilder, nameof(MilestoneManagerMaintenance.VendorId));
        UserLink<MilestoneManagerNotice>(modelBuilder, nameof(MilestoneManagerNotice.ManagerUserId)); OptionalUserLink<MilestoneManagerNotice>(modelBuilder, nameof(MilestoneManagerNotice.TargetOwnerUserId));
        UserLink<MilestoneManagerProposal>(modelBuilder, nameof(MilestoneManagerProposal.ManagerUserId));
        modelBuilder.Entity<MilestoneManagerEligibleVoter>().HasOne<MilestoneManagerProposal>().WithMany().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Restrict);
        UserLink<MilestoneManagerEligibleVoter>(modelBuilder, nameof(MilestoneManagerEligibleVoter.OwnerUserId));
        modelBuilder.Entity<MilestoneManagerVote>().HasOne<MilestoneManagerProposal>().WithMany().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MilestoneManagerVote>().HasOne<MilestoneManagerProxy>().WithMany().HasForeignKey(x => x.ProxyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MilestoneManagerProxy>().HasOne<MilestoneManagerProposal>().WithMany().HasForeignKey(x => x.ProposalId).OnDelete(DeleteBehavior.Restrict);
        UserLink<MilestoneManagerProxy>(modelBuilder, nameof(MilestoneManagerProxy.OwnerUserId)); UserLink<MilestoneManagerProxy>(modelBuilder, nameof(MilestoneManagerProxy.ProxyUserId));
        UserLink<MilestoneManagerDocument>(modelBuilder, nameof(MilestoneManagerDocument.ManagerUserId)); OptionalUserLink<MilestoneManagerDocument>(modelBuilder, nameof(MilestoneManagerDocument.OwnerUserId)); OptionalPropertyLink<MilestoneManagerDocument>(modelBuilder, nameof(MilestoneManagerDocument.PropertyId));
        UserLink<MilestoneManagerDocumentExport>(modelBuilder, nameof(MilestoneManagerDocumentExport.ManagerUserId));
        UserLink<MilestoneManagerGateMessage>(modelBuilder, nameof(MilestoneManagerGateMessage.ManagerUserId)); OptionalPropertyLink<MilestoneManagerGateMessage>(modelBuilder, nameof(MilestoneManagerGateMessage.PropertyId));
        UserLink<MilestoneManagerQrAccess>(modelBuilder, nameof(MilestoneManagerQrAccess.ManagerUserId)); OptionalUserLink<MilestoneManagerQrAccess>(modelBuilder, nameof(MilestoneManagerQrAccess.OwnerUserId)); OptionalPropertyLink<MilestoneManagerQrAccess>(modelBuilder, nameof(MilestoneManagerQrAccess.PropertyId));
        modelBuilder.Entity<MilestoneManagerQrScan>().HasOne<MilestoneManagerQrAccess>().WithMany().HasForeignKey(x => x.QrAccessId).OnDelete(DeleteBehavior.Restrict);
        OptionalUserLink<MilestoneManagerQrScan>(modelBuilder, nameof(MilestoneManagerQrScan.GateGuardUserId));

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
