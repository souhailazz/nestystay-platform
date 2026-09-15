using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;
using NestyStay.Domain.Notifications;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Infrastructure.Notifications;

public interface IEmailDeliveryTransport
{
    string ProviderName { get; }
    Task<EmailTransportResult> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailTransportResult(bool Success, string? ProviderMessageId = null, string? Error = null);

/// <summary>Safe, provider-neutral NestyStay email catalog. Values are HTML encoded before insertion.</summary>
public static class EmailTemplateCatalog
{
    private static readonly IReadOnlyDictionary<string, EmailTemplate> Templates =
        new Dictionary<string, EmailTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            // M1 — authentication, identity, booking and payment
            ["passwordless-login"] = Build("passwordless-login", "Your NestyStay sign-in link", "ACCOUNT ACCESS", "Sign in securely", "Your one-time NestyStay sign-in link is ready.", "Use the secure link below to continue. It expires at {{expiresAt}} and can only be used once.", "Hello,\n\nOpen your secure NestyStay sign-in link: {{actionUrl}}\n\nThis one-time link expires at {{expiresAt}}. If you did not request this, ignore this email.", "If you requested this sign-in, you can continue safely with the button below.", "Continue securely"),
            ["auth-code"] = Build("auth-code", "Your NestyStay verification code", "ACCOUNT ACCESS", "Verify your email", "Complete your NestyStay verification.", "Use the button or the fallback code below to finish verifying your account.", "Hello,\n\nOpen your NestyStay verification page: {{actionUrl}}\n\nFallback code: {{code}}. It expires at {{expiresAt}}.", "Your fallback verification code is <strong>{{code}}</strong>.", "Verify your email"),
            ["password-reset"] = Build("password-reset", "Reset your NestyStay password", "ACCOUNT ACCESS", "Reset your password", "A secure password reset was requested for your NestyStay account.", "Choose a new password using the secure link below. This one-time link expires at {{expiresAt}}.", "Hello,\n\nOpen your NestyStay password reset page: {{actionUrl}}\n\nThis one-time link expires at {{expiresAt}}. If you did not request this, ignore this email.", "This link can only be used once. If you did not request a reset, no action is needed.", "Reset password"),
            ["owner-invitation"] = Build("owner-invitation", "You have been invited to NestyStay", "PROPERTY MANAGEMENT", "Your owner invitation is ready", "You have been invited to join a NestyStay property-management workspace.", "Review the invitation and choose whether to join the workspace.", "Hello,\n\nOpen your NestyStay owner invitation: {{actionUrl}}\n\nIf the button does not work, copy this URL: {{actionUrl}}.", "The invitation is linked to your email address and should not be forwarded.", "Review invitation"),
            ["two-factor-enabled"] = Build("two-factor-enabled", "Two-factor authentication is now enabled", "ACCOUNT SECURITY", "Your account is protected", "Two-factor authentication was enabled on your NestyStay account.", "Your next sign-in will require the additional verification method you selected.", "Hello,\n\nTwo-factor authentication is now enabled on your NestyStay account. If you did not make this change, contact NestyStay Support immediately.", "Keep your recovery codes private and store them somewhere safe.", null),
            ["new-login-alert"] = Build("new-login-alert", "New sign-in to your NestyStay account", "ACCOUNT SECURITY", "New sign-in detected", "A new sign-in was detected on your NestyStay account.", "If this was you, no action is needed. If it was not you, secure your account immediately.", "Hello,\n\nA new sign-in was detected on your NestyStay account from {{loginLocation}} on {{loginTime}}. If this was not you, reset your password and contact NestyStay Support.", Details(("When", "{{loginTime}}"), ("Location", "{{loginLocation}}")), "Secure my account"),
            ["booking-request-received"] = Build("booking-request-received", "Your NestyStay booking request was received", "YOUR STAY", "Booking request received", "Your NestyStay booking request is now pending host review.", "We have received your request and will keep you updated as it moves through verification and approval.", "Hello,\n\nYour booking request for {{propertyName}} is now PENDING.\n\nDates: {{checkIn}} — {{checkOut}}\nGuests: {{guestCount}}\nEstimated total: {{currency}} {{total}}\n\nView your booking: {{actionUrl}}", Details(("Stay", "{{propertyName}}"), ("Dates", "{{checkIn}} — {{checkOut}}"), ("Guests", "{{guestCount}}"), ("Estimated total", "{{currency}} {{total}}")), "View booking"),
            ["booking-request-to-host"] = Build("booking-request-to-host", "New booking request for your NestyStay property", "HOST INBOX", "A guest wants to stay", "A new booking request is waiting for your review.", "Review the dates, guest details and verification requirement before choosing an action.", "Hello,\n\n{{guestName}} requested {{propertyName}} for {{checkIn}} — {{checkOut}}.\n\nGuests: {{guestCount}}\nEstimated total: {{currency}} {{total}}\n\nReview the request: {{actionUrl}}", Details(("Guest", "{{guestName}}"), ("Stay", "{{propertyName}}"), ("Dates", "{{checkIn}} — {{checkOut}}"), ("Guests", "{{guestCount}}")), "Review request"),
            ["booking-pending-verification"] = Build("booking-pending-verification", "Identity verification is needed for your NestyStay booking", "YOUR STAY", "Verification needed", "Your booking is waiting for identity verification.", "Complete the secure verification step to keep your booking moving.", "Hello,\n\nYour booking for {{propertyName}} is PENDING verification. Complete verification here: {{actionUrl}}.", Status("Booking status", "PENDING VERIFICATION"), "Start verification"),
            ["verification-processing"] = Build("verification-processing", "Your NestyStay identity verification is processing", "IDENTITY CHECK", "We are checking your details", "Your identity verification is being reviewed securely.", "Your booking remains pending while the verification provider processes the submission.", "Hello,\n\nYour identity verification for {{propertyName}} is PROCESSING. We will email you when the result is available.", Status("Verification", "PROCESSING"), null),
            ["verification-approved"] = Build("verification-approved", "Your NestyStay identity verification was approved", "IDENTITY CHECK", "Verification approved", "Your identity verification was approved and your booking can continue.", "The verification step is complete. Check your booking for the next status.", "Hello,\n\nYour identity verification for {{propertyName}} was APPROVED. View your booking here: {{actionUrl}}.", Status("Verification", "APPROVED"), "View booking"),
            ["verification-rejected"] = Build("verification-rejected", "Action needed on your NestyStay identity verification", "IDENTITY CHECK", "Verification needs attention", "Your identity verification could not be approved.", "Review the reason shown in NestyStay and follow the available next step. Your booking will not be confirmed until verification is resolved.", "Hello,\n\nYour identity verification for {{propertyName}} was not approved. Reason: {{reason}}\n\nReview the next step: {{actionUrl}}", Status("Verification", "ACTION NEEDED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "Review verification"),
            ["booking-approved"] = Build("booking-approved", "Your NestyStay booking was approved", "YOUR STAY", "Booking approved", "Your NestyStay booking is APPROVED.", "Your host has approved the request and your stay is ready for the payment or confirmation step.", "Hello,\n\nYour booking for {{propertyName}} was APPROVED.\n\nDates: {{checkIn}} — {{checkOut}}\nGuests: {{guestCount}}\nTotal: {{currency}} {{total}}\n\nView booking: {{actionUrl}}", Status("Booking status", "APPROVED") + Details(("Stay", "{{propertyName}}"), ("Dates", "{{checkIn}} — {{checkOut}}"), ("Total", "{{currency}} {{total}}")), "View booking"),
            ["booking-rejected"] = Build("booking-rejected", "Your NestyStay booking was rejected", "YOUR STAY", "Booking not approved", "Your NestyStay booking could not be approved.", "The reason below reflects the real booking decision. The dates have been released when applicable.", "Hello,\n\nYour booking for {{propertyName}} was REJECTED.\nReason: {{reason}}\n\nView your booking: {{actionUrl}}", Status("Booking status", "REJECTED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "View booking"),
            ["booking-host-declined"] = Build("booking-host-declined", "Your NestyStay host declined the booking request", "YOUR STAY", "Booking request declined", "The host declined your booking request.", "The reason shown below is the host-provided decision reason. You can now explore other available stays.", "Hello,\n\nYour booking request for {{propertyName}} was declined by the host.\nReason: {{reason}}\n\nExplore other stays: {{actionUrl}}", Status("Booking status", "DECLINED BY HOST") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "Explore other stays"),
            ["booking-cancelled"] = Build("booking-cancelled", "Your NestyStay booking was cancelled", "YOUR STAY", "Booking cancelled", "Your NestyStay booking is now cancelled.", "Review the details below for the current payment and refund status.", "Hello,\n\nYour booking for {{propertyName}} was CANCELLED.\nReason: {{reason}}\nPayment status: {{paymentStatus}}\n\nView booking: {{actionUrl}}", Status("Booking status", "CANCELLED") + Details(("Stay", "{{propertyName}}"), ("Payment", "{{paymentStatus}}")), "View booking"),
            ["payment-authorized"] = Build("payment-authorized", "Payment authorized for your NestyStay booking", "PAYMENT", "Payment authorized", "Your payment method has been authorized for the booking.", "The payment is held for the booking workflow and is not necessarily captured until the required approval step completes.", "Hello,\n\nPayment authorization for {{propertyName}} is complete.\nAmount: {{currency}} {{amount}}\nReference: {{paymentReference}}\n\nView booking: {{actionUrl}}", Status("Payment", "AUTHORIZED") + Details(("Stay", "{{propertyName}}"), ("Amount", "{{currency}} {{amount}}")), "View booking"),
            ["payment-failed"] = Build("payment-failed", "Action needed: NestyStay payment failed", "PAYMENT", "Payment could not be completed", "We could not complete the payment for your NestyStay booking.", "Review the payment status and try again with an eligible payment method.", "Hello,\n\nPayment for {{propertyName}} could not be completed. Reason: {{reason}}\n\nTry again: {{actionUrl}}", Status("Payment", "FAILED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "Try payment again"),
            ["payment-confirmed"] = Build("payment-confirmed", "Payment confirmed for your NestyStay booking", "PAYMENT", "Payment confirmed", "Your NestyStay payment was captured successfully.", "Your booking and payment record are now updated.", "Hello,\n\nPayment for {{propertyName}} was CONFIRMED.\nAmount: {{currency}} {{amount}}\nReference: {{paymentReference}}\n\nView receipt: {{actionUrl}}", Status("Payment", "CONFIRMED") + Details(("Stay", "{{propertyName}}"), ("Amount", "{{currency}} {{amount}}"), ("Reference", "{{paymentReference}}")), "View receipt"),
            ["payment-refunded"] = Build("payment-refunded", "Your NestyStay payment was refunded", "PAYMENT", "Refund issued", "A refund has been issued for your NestyStay booking.", "The refund amount and reason are shown below. Your payment provider may take additional time to post the funds.", "Hello,\n\nA refund for {{propertyName}} was issued.\nAmount: {{currency}} {{amount}}\nReason: {{reason}}\nReference: {{refundReference}}\n\nView booking: {{actionUrl}}", Status("Payment", "REFUNDED") + Details(("Stay", "{{propertyName}}"), ("Refund", "{{currency}} {{amount}}"), ("Reference", "{{refundReference}}")), "View booking"),
            ["receipt-issued"] = Build("receipt-issued", "Your NestyStay receipt is ready", "PAYMENT", "Receipt ready", "Your NestyStay receipt is ready to view.", "Keep this receipt for your records. The totals below reflect the booking ledger.", "Hello,\n\nYour receipt for {{propertyName}} is ready.\nTotal: {{currency}} {{total}}\nReceipt number: {{receiptNumber}}\n\nOpen receipt: {{actionUrl}}", Details(("Stay", "{{propertyName}}"), ("Total", "{{currency}} {{total}}"), ("Receipt", "{{receiptNumber}}")), "Open receipt"),
            ["trip-reminder"] = Build("trip-reminder", "Your NestyStay stay is coming up", "YOUR STAY", "Your stay is coming up", "A reminder for your upcoming NestyStay reservation.", "Review your dates, arrival details and host instructions before you travel.", "Hello,\n\nYour stay at {{propertyName}} begins on {{checkIn}}.\n\nView trip details: {{actionUrl}}", Details(("Stay", "{{propertyName}}"), ("Check-in", "{{checkIn}}"), ("Check-out", "{{checkOut}}")), "View trip details"),
            // M2 — badge lifecycle
            ["badge-upgrade-submitted"] = Build("badge-upgrade-submitted", "Your NestyStay badge review was submitted", "HOST BADGES", "Badge review submitted", "Your request to move to the {{badgeLevel}} badge is now under review.", "We will notify you when the review is complete or if more information is needed.", "Hello,\n\nYour {{badgeLevel}} badge request was submitted on {{submittedAt}}.\n\nReview status: {{actionUrl}}", Status("Badge review", "PENDING") + Details(("Requested level", "{{badgeLevel}}"), ("Submitted", "{{submittedAt}}")), "View badge status"),
            ["badge-upgrade-approved"] = Build("badge-upgrade-approved", "Your NestyStay badge was approved", "HOST BADGES", "Badge approved", "Your NestyStay host badge is now {{badgeLevel}}.", "Your profile and eligible listings can now show the updated badge level.", "Hello,\n\nYour NestyStay badge was approved at the {{badgeLevel}} level.\n\nView your badge status: {{actionUrl}}", Status("Badge", "{{badgeLevel}}") + "<p style=\"margin:16px 0 0;\">Approved on {{approvedAt}}.</p>", "View badge status"),
            ["badge-upgrade-rejected"] = Build("badge-upgrade-rejected", "Your NestyStay badge review needs attention", "HOST BADGES", "Badge review not approved", "Your badge request was not approved at this time.", "Review the reason and the eligibility steps available for your host profile.", "Hello,\n\nYour {{badgeLevel}} badge request was not approved.\nReason: {{reason}}\n\nReview eligibility: {{actionUrl}}", Status("Badge review", "NOT APPROVED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "Review eligibility"),
            ["badge-renewal-due"] = Build("badge-renewal-due", "Your NestyStay badge renewal is due soon", "HOST BADGES", "Badge renewal due", "Your {{badgeLevel}} badge needs renewal before {{renewalDate}}.", "Complete the renewal steps in time to keep the badge active on your profile.", "Hello,\n\nYour {{badgeLevel}} badge is due for renewal on {{renewalDate}}.\n\nRenewal details: {{actionUrl}}", Status("Badge", "RENEWAL DUE") + Details(("Level", "{{badgeLevel}}"), ("Renew by", "{{renewalDate}}")), "Review renewal"),
            // M3 — wellness, officers, subscriptions and payouts
            ["officer-application-submitted"] = Build("officer-application-submitted", "Your NestyStay officer application was submitted", "WELLNESS OPERATIONS", "Application submitted", "Your officer application is now in the NestyStay review queue.", "We will contact you if the review team needs more information.", "Hello,\n\nYour officer application was submitted on {{submittedAt}}.\n\nView application status: {{actionUrl}}", Status("Application", "PENDING REVIEW"), "View application"),
            ["officer-application-approved"] = Build("officer-application-approved", "Your NestyStay officer application was approved", "WELLNESS OPERATIONS", "Application approved", "Your NestyStay officer application was approved.", "You can now review your availability, coverage and assigned wellness work.", "Hello,\n\nYour officer application was APPROVED.\n\nOpen your wellness workspace: {{actionUrl}}", Status("Application", "APPROVED"), "Open workspace"),
            ["officer-application-changes-requested"] = Build("officer-application-changes-requested", "Changes are needed for your NestyStay officer application", "WELLNESS OPERATIONS", "Application needs changes", "The NestyStay review team requested changes to your officer application.", "Review the reason and update only the requested information.", "Hello,\n\nChanges are requested for your officer application.\nReason: {{reason}}\n\nUpdate your application: {{actionUrl}}", Status("Application", "CHANGES REQUESTED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "Update application"),
            ["wellness-assignment-confirmed"] = Build("wellness-assignment-confirmed", "A NestyStay wellness visit was assigned to you", "WELLNESS OPERATIONS", "Visit assignment confirmed", "A wellness visit has been assigned to your NestyStay workspace.", "Review the property, schedule and privacy instructions before the visit.", "Hello,\n\nA wellness visit at {{propertyName}} was assigned to you for {{visitDate}}.\n\nOpen assignment: {{actionUrl}}", Details(("Property", "{{propertyName}}"), ("Date", "{{visitDate}}"), ("Time", "{{visitTime}}")), "Open assignment"),
            ["wellness-report-ready"] = Build("wellness-report-ready", "Your NestyStay wellness report is ready", "WELLNESS OPERATIONS", "Report ready to review", "The wellness report for {{propertyName}} is ready.", "Review the report and acknowledge any follow-up tasks in your workspace.", "Hello,\n\nThe wellness report for {{propertyName}} is ready.\nVisit date: {{visitDate}}\n\nOpen report: {{actionUrl}}", Details(("Property", "{{propertyName}}"), ("Visit date", "{{visitDate}}"), ("Report", "{{reportNumber}}")), "Open report"),
            ["wellness-booking-cancelled"] = Build("wellness-booking-cancelled", "Your NestyStay wellness visit was cancelled", "WELLNESS OPERATIONS", "Visit cancelled", "The NestyStay wellness visit has been cancelled.", "Review the reason and any rescheduling action available in the workspace.", "Hello,\n\nThe wellness visit at {{propertyName}} on {{visitDate}} was cancelled.\nReason: {{reason}}\n\nView details: {{actionUrl}}", Status("Visit", "CANCELLED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "View details"),
            ["subscription-renewal-due"] = Build("subscription-renewal-due", "Your NestyStay subscription renews soon", "SUBSCRIPTION", "Renewal coming up", "Your NestyStay {{planName}} subscription renews on {{renewalDate}}.", "Review the plan, payment method and renewal amount before the renewal date.", "Hello,\n\nYour {{planName}} subscription renews on {{renewalDate}} for {{currency}} {{amount}}.\n\nManage subscription: {{actionUrl}}", Details(("Plan", "{{planName}}"), ("Renewal date", "{{renewalDate}}"), ("Amount", "{{currency}} {{amount}}")), "Manage subscription"),
            ["subscription-payment-failed"] = Build("subscription-payment-failed", "Action needed: NestyStay subscription payment failed", "SUBSCRIPTION", "Subscription payment failed", "We could not process your NestyStay subscription payment.", "Update the payment method to avoid a lapse in service.", "Hello,\n\nPayment for your {{planName}} subscription failed.\nReason: {{reason}}\n\nUpdate payment: {{actionUrl}}", Status("Subscription", "PAYMENT FAILED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "Update payment"),
            ["payout-statement-ready"] = Build("payout-statement-ready", "Your NestyStay payout statement is ready", "HOST FINANCE", "Payout statement ready", "Your NestyStay payout statement is ready to review.", "The statement includes the booking activity, fees and current payout status for the period.", "Hello,\n\nYour payout statement for {{statementPeriod}} is ready.\nAmount: {{currency}} {{amount}}\n\nOpen statement: {{actionUrl}}", Details(("Period", "{{statementPeriod}}"), ("Amount", "{{currency}} {{amount}}"), ("Status", "{{payoutStatus}}")), "Open statement"),
            // M4 — provider directories and QR access
            ["provider-application-submitted"] = Build("provider-application-submitted", "Your NestyStay provider application was submitted", "PROVIDER DIRECTORY", "Application submitted", "Your provider application is now waiting for moderation.", "You will receive another message when the review team approves it, rejects it or requests changes.", "Hello,\n\nYour provider application for {{businessName}} was submitted on {{submittedAt}}.\n\nView status: {{actionUrl}}", Status("Moderation", "PENDING"), "View application"),
            ["provider-approved"] = Build("provider-approved", "Your NestyStay provider profile was approved", "PROVIDER DIRECTORY", "Provider profile approved", "Your provider profile is approved and can appear in the NestyStay directory.", "Keep your services, coverage area and availability current.", "Hello,\n\nYour provider profile for {{businessName}} was APPROVED.\n\nOpen provider dashboard: {{actionUrl}}", Status("Moderation", "APPROVED"), "Open dashboard"),
            ["provider-rejected"] = Build("provider-rejected", "Your NestyStay provider profile was rejected", "PROVIDER DIRECTORY", "Provider profile not approved", "Your provider profile was not approved for directory publication.", "Review the decision reason and the next action available to you.", "Hello,\n\nYour provider profile for {{businessName}} was REJECTED.\nReason: {{reason}}\n\nView decision: {{actionUrl}}", Status("Moderation", "REJECTED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "View decision"),
            ["provider-changes-requested"] = Build("provider-changes-requested", "Changes are needed for your NestyStay provider profile", "PROVIDER DIRECTORY", "Profile needs changes", "The moderation team requested changes before your provider profile can be published.", "Review the requested changes and resubmit when ready.", "Hello,\n\nChanges are requested for {{businessName}}.\nReason: {{reason}}\n\nUpdate profile: {{actionUrl}}", Status("Moderation", "CHANGES REQUESTED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "Update profile"),
            ["directory-quote-received"] = Build("directory-quote-received", "You received a NestyStay service quote", "DIRECTORY", "New quote received", "A provider responded to your NestyStay service request.", "Compare the quote details and choose the next step from your workspace.", "Hello,\n\n{{providerName}} sent a quote for {{serviceName}}.\nAmount: {{currency}} {{amount}}\n\nReview quote: {{actionUrl}}", Details(("Provider", "{{providerName}}"), ("Service", "{{serviceName}}"), ("Quote", "{{currency}} {{amount}}")), "Review quote"),
            ["review-response"] = Build("review-response", "A NestyStay host responded to your review", "COMMUNITY", "Review response received", "{{hostName}} responded to your review of {{propertyName}}.", "Open the conversation in NestyStay to read the response.", "Hello,\n\n{{hostName}} responded to your review of {{propertyName}}.\n\nRead response: {{actionUrl}}", Details(("Property", "{{propertyName}}"), ("Host", "{{hostName}}")), "Read response"),
            ["qr-issued"] = Build("qr-issued", "Your NestyStay access QR is ready", "PROPERTY ACCESS", "Access QR issued", "A new NestyStay access QR was issued for {{propertyName}}.", "Keep the QR private and show it only at the intended property or gate.", "Hello,\n\nAn access QR was issued for {{propertyName}}.\nPurpose: {{purpose}}\nExpires: {{expiresAt}}\n\nView QR details: {{actionUrl}}", Details(("Property", "{{propertyName}}"), ("Purpose", "{{purpose}}"), ("Expires", "{{expiresAt}}")), "View QR details"),
            ["qr-revoked"] = Build("qr-revoked", "Your NestyStay access QR was revoked", "PROPERTY ACCESS", "Access QR revoked", "The NestyStay access QR for {{propertyName}} is no longer valid.", "Do not use the old QR. Contact the property manager if you believe this was unexpected.", "Hello,\n\nThe access QR for {{propertyName}} was REVOKED.\nReason: {{reason}}\n\nView access history: {{actionUrl}}", Status("Access", "REVOKED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "View access history"),
            // M5 — property-manager finance, documents, governance and gate operations
            ["invoice-issued"] = Build("invoice-issued", "A NestyStay invoice is ready", "PROPERTY MANAGEMENT", "Invoice ready", "A new NestyStay invoice is ready for {{propertyName}}.", "Review the line items, due date and payment options in the owner portal.", "Hello,\n\nInvoice {{invoiceNumber}} for {{propertyName}} is ready.\nDue: {{dueDate}}\nBalance: {{currency}} {{balance}}\n\nOpen invoice: {{actionUrl}}", Details(("Invoice", "{{invoiceNumber}}"), ("Property", "{{propertyName}}"), ("Due", "{{dueDate}}"), ("Balance", "{{currency}} {{balance}}")), "Open invoice"),
            ["invoice-payment-received"] = Build("invoice-payment-received", "Your NestyStay invoice payment was received", "PROPERTY MANAGEMENT", "Payment received", "Your payment for NestyStay invoice {{invoiceNumber}} was recorded.", "Keep this confirmation for your records.", "Hello,\n\nPayment received for invoice {{invoiceNumber}}.\nAmount: {{currency}} {{amount}}\nRemaining balance: {{currency}} {{balance}}\n\nView invoice: {{actionUrl}}", Status("Invoice", "PAID") + Details(("Invoice", "{{invoiceNumber}}"), ("Paid", "{{currency}} {{amount}}"), ("Remaining", "{{currency}} {{balance}}")), "View invoice"),
            ["invoice-payment-reminder"] = Build("invoice-payment-reminder", "Reminder: your NestyStay invoice is due soon", "PROPERTY MANAGEMENT", "Invoice payment reminder", "Invoice {{invoiceNumber}} for {{propertyName}} is due on {{dueDate}}.", "Review the balance and payment options before the due date.", "Hello,\n\nInvoice {{invoiceNumber}} for {{propertyName}} is due on {{dueDate}}.\nBalance: {{currency}} {{balance}}\n\nOpen invoice: {{actionUrl}}", Status("Invoice", "PAYMENT DUE") + Details(("Invoice", "{{invoiceNumber}}"), ("Due", "{{dueDate}}"), ("Balance", "{{currency}} {{balance}}")), "Open invoice"),
            ["maintenance-update"] = Build("maintenance-update", "Your NestyStay maintenance request was updated", "PROPERTY MANAGEMENT", "Maintenance update", "There is a new update on the maintenance request for {{propertyName}}.", "Review the status, assignee and next action in the property-management workspace.", "Hello,\n\nMaintenance request {{requestNumber}} for {{propertyName}} is now {{status}}.\nUpdate: {{update}}\n\nView request: {{actionUrl}}", Status("Maintenance", "{{status}}") + "<p style=\"margin:16px 0 0;\">{{update}}</p>", "View request"),
            ["community-notice"] = Build("community-notice", "New NestyStay community notice", "COMMUNITY", "New community notice", "A new notice was posted for {{propertyName}}.", "Review the notice and acknowledge it when required.", "Hello,\n\nA new community notice was posted for {{propertyName}}.\nTitle: {{noticeTitle}}\n\nRead notice: {{actionUrl}}", Details(("Property", "{{propertyName}}"), ("Notice", "{{noticeTitle}}"), ("Published", "{{publishedAt}}")), "Read notice"),
            ["governance-vote-opened"] = Build("governance-vote-opened", "A NestyStay governance vote is open", "GOVERNANCE", "Your vote is requested", "A governance vote is open for eligible NestyStay owners.", "Review the motion, deadline and eligibility information before voting.", "Hello,\n\nThe governance vote “{{voteTitle}}” is open until {{closesAt}}.\n\nReview vote: {{actionUrl}}", Details(("Motion", "{{voteTitle}}"), ("Closes", "{{closesAt}}")), "Review vote"),
            ["gate-pass-issued"] = Build("gate-pass-issued", "A NestyStay gate pass is ready", "PROPERTY ACCESS", "Gate pass issued", "A gate pass was issued for {{propertyName}}.", "Review the visitor, validity period and property before using or sharing the pass.", "Hello,\n\nA gate pass was issued for {{propertyName}}.\nVisitor: {{visitorName}}\nValid: {{validFrom}} — {{validUntil}}\n\nView pass: {{actionUrl}}", Details(("Property", "{{propertyName}}"), ("Visitor", "{{visitorName}}"), ("Valid", "{{validFrom}} — {{validUntil}}")), "View gate pass"),
            ["gate-pass-revoked"] = Build("gate-pass-revoked", "A NestyStay gate pass was revoked", "PROPERTY ACCESS", "Gate pass revoked", "The gate pass for {{propertyName}} is no longer valid.", "Do not use the revoked pass. Review the access history for the recorded reason.", "Hello,\n\nThe gate pass for {{propertyName}} was REVOKED.\nReason: {{reason}}\n\nView access history: {{actionUrl}}", Status("Gate pass", "REVOKED") + "<p style=\"margin:16px 0 0;\">Reason: <strong>{{reason}}</strong></p>", "View access history"),
            ["document-expiry"] = Build("document-expiry", "A NestyStay document needs attention", "PROPERTY MANAGEMENT", "Document expiry reminder", "The document {{documentTitle}} {{expiryMessage}}.", "Review the document in NestyStay before the deadline so the record stays current.", "Hello,\n\nThe document {{documentTitle}} in your property-management workspace {{expiryMessage}}. Review it in NestyStay before the deadline.", Details(("Document", "{{documentTitle}}"), ("Status", "{{expiryMessage}}")), null),
            ["booking-update"] = Build("booking-update", "Your NestyStay booking update", "YOUR STAY", "Booking status updated", "Your NestyStay booking status is now {{status}}.", "Keep this message for your records and open the booking for the full timeline.", "Hello,\n\nYour booking status is now {{status}}.\n\nOpen booking: {{actionUrl}}", Status("Booking status", "{{status}}"), "Open booking")
        };

    public static IReadOnlyCollection<EmailTemplate> All => Templates.Values.ToArray();

    public static EmailTemplate? Find(string? key) =>
        key is not null && Templates.TryGetValue(key, out var template) ? template : null;

    public static EmailMessage Apply(EmailMessage message, IReadOnlyDictionary<string, string>? values = null)
    {
        var template = Find(message.TemplateKey);
        if (template is null || values is null || values.Count == 0)
        {
            return message;
        }

        var subject = Replace(template.Subject, values, html: false);
        var textBody = Replace(template.TextBody, values, html: false);
        var htmlBody = template.HtmlBody is null ? null : Replace(template.HtmlBody, values, html: true);
        return message with
        {
            Subject = subject,
            Body = message.IsHtml && htmlBody is not null ? htmlBody : textBody,
            TextBody = textBody,
            HtmlBody = htmlBody
        };
    }

    private static EmailTemplate Build(
        string key,
        string subject,
        string eyebrow,
        string title,
        string preheader,
        string intro,
        string textBody,
        string contentHtml,
        string? actionLabel)
    {
        var actionHtml = string.IsNullOrWhiteSpace(actionLabel)
            ? string.Empty
            : "<div style=\"margin:28px 0 0;\"><a href=\"{{actionUrl}}\" style=\"display:inline-block;background:#ffd21a;color:#06251f;text-decoration:none;font-weight:700;padding:14px 22px;border-radius:999px;\">" + actionLabel + " &rarr;</a><p style=\"margin:14px 0 0;color:#6d746f;font-size:12px;line-height:1.55;\">If the button does not work, copy this link:<br><span style=\"overflow-wrap:anywhere;\">{{actionUrl}}</span></p></div>";

        var html = "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>NestyStay</title></head>" +
                   "<body style=\"margin:0;background:#f3f1eb;color:#10201b;font-family:Arial,Helvetica,sans-serif;\"><span style=\"display:none!important;max-height:0;overflow:hidden;opacity:0;\">" + preheader + "</span>" +
                   "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background:#f3f1eb;width:100%;\"><tr><td align=\"center\" style=\"padding:32px 16px;\">" +
                   "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:640px;background:#fffefa;border:1px solid #dedbd1;border-radius:22px;overflow:hidden;\">" +
                   "<tr><td style=\"padding:28px 36px;background:#062f28;color:#fffdf4;\"><div style=\"font-size:16px;font-weight:800;letter-spacing:3px;\">NESTY STAY</div><div style=\"margin-top:8px;color:#c7d6cc;font-size:12px;letter-spacing:.4px;\">Jamaica stays, made personal.</div></td></tr>" +
                   "<tr><td style=\"padding:38px 36px 34px;\"><div style=\"width:44px;height:3px;background:#d39a22;margin-bottom:24px;\"></div><p style=\"margin:0 0 12px;color:#a46c09;font-size:11px;font-weight:800;letter-spacing:1.8px;\">" + eyebrow + "</p><h1 style=\"margin:0;color:#10201b;font-family:Georgia,'Times New Roman',serif;font-size:34px;line-height:1.12;font-weight:700;\">" + title + "</h1><p style=\"margin:18px 0 0;color:#515b56;font-size:16px;line-height:1.6;\">" + intro + "</p>" +
                   contentHtml + actionHtml +
                   "</td></tr><tr><td style=\"padding:22px 36px 28px;border-top:1px solid #ebe7de;color:#7b817b;font-size:12px;line-height:1.6;\">Questions? Contact <a href=\"mailto:support@nestystay.net\" style=\"color:#07594b;\">NestyStay Support</a>.<br>This is a transactional message from NestyStay. Please do not reply with passwords, codes or identity documents.</td></tr></table>" +
                   "<p style=\"margin:18px 0 0;color:#8a8e88;font-size:11px;\">NestyStay &middot; Jamaica</p></td></tr></table></body></html>";

        return new EmailTemplate(key, subject, textBody, html);
    }

    private static string Details(params (string Label, string Value)[] rows)
    {
        var html = "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin:24px 0 0;background:#f5f3ed;border:1px solid #e3ded3;border-radius:14px;\">";
        foreach (var (label, value) in rows)
        {
            html += "<tr><td style=\"padding:12px 14px;color:#707771;font-size:12px;border-bottom:1px solid #e3ded3;\">" + label + "</td><td align=\"right\" style=\"padding:12px 14px;color:#10201b;font-size:13px;font-weight:700;border-bottom:1px solid #e3ded3;\">" + value + "</td></tr>";
        }

        return html + "</table>";
    }

    private static string Status(string label, string value) =>
        "<div style=\"margin:24px 0 0;padding:14px 16px;background:#e8f1ea;border-left:4px solid #0a7b63;border-radius:10px;\"><span style=\"display:block;color:#5e6c63;font-size:11px;font-weight:800;letter-spacing:1.2px;text-transform:uppercase;\">" + label + "</span><strong style=\"display:block;margin-top:5px;color:#07594b;font-size:16px;\">" + value + "</strong></div>";

    private static string Replace(string value, IReadOnlyDictionary<string, string> values, bool html)
    {
        foreach (var (key, replacement) in values)
        {
            var safe = html ? HtmlEncoder.Default.Encode(replacement) : replacement;
            value = value.Replace("{{" + key + "}}", safe, StringComparison.Ordinal);
        }

        return value;
    }
}

/// <summary>Queues email in the existing PostgreSQL notification outbox; it never performs network I/O.</summary>
public sealed class EmailOutboxSender(IServiceScopeFactory scopeFactory, TimeProvider timeProvider) : IEmailSender
{
    public string ProviderName => "NestyStay email outbox";

    public async Task<EmailDeliveryResult> QueueAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        message = EmailTemplateCatalog.Apply(message, message.TemplateValues);
        Validate(message);
        var idempotencyKey = string.IsNullOrWhiteSpace(message.IdempotencyKey)
            ? message.CorrelationId?.ToString("N")
            : message.IdempotencyKey.Trim();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var configuration = scope.ServiceProvider.GetService<IConfiguration>();
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await db.NotificationQueue.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Channel == "Email" && item.IdempotencyKey == idempotencyKey && !item.IsDeleted, cancellationToken);
            if (existing is not null)
            {
                return ToResult(existing, ProviderName);
            }
        }

        var now = timeProvider.GetUtcNow();
        var item = new NotificationQueueItem
        {
            Channel = "Email",
            Recipient = message.To.Trim(),
            Subject = message.Subject.Trim(),
            Body = message.Body,
            TextBody = message.TextBody ?? (!message.IsHtml ? message.Body : null),
            HtmlBody = message.HtmlBody ?? (message.IsHtml ? message.Body : null),
            ReplyToEmail = message.ReplyToEmail ?? configuration?["Email:Brevo:ReplyToEmail"] ?? Environment.GetEnvironmentVariable("REPLY_TO_EMAIL"),
            ReplyToName = message.ReplyToName ?? configuration?["Email:Brevo:ReplyToName"] ?? "NestyStay Support",
            Status = NotificationStatus.Queued,
            DeliveryStatus = nameof(EmailDeliveryStatus.Pending).ToUpperInvariant(),
            NextAttemptAt = now,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.NotificationQueue.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return new EmailDeliveryResult(item.Id, EmailDeliveryStatus.Pending, ProviderName, AcceptedAt: now);
    }

    private static void Validate(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.To) || !MailAddress.TryCreate(message.To.Trim(), out _))
        {
            throw new ArgumentException("A valid email recipient is required.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Subject) || message.Subject.Length > 512)
        {
            throw new ArgumentException("Email subject is required and must be at most 512 characters.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Body) && string.IsNullOrWhiteSpace(message.TextBody) && string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            throw new ArgumentException("Email body is required.", nameof(message));
        }

        if (!string.IsNullOrWhiteSpace(message.ReplyToEmail) && !MailAddress.TryCreate(message.ReplyToEmail.Trim(), out _))
        {
            throw new ArgumentException("Reply-to email must be valid when supplied.", nameof(message));
        }
    }

    private static EmailDeliveryResult ToResult(NotificationQueueItem item, string providerName) =>
        new(item.Id, Enum.TryParse<EmailDeliveryStatus>(item.DeliveryStatus, true, out var status) ? status : EmailDeliveryStatus.Pending,
            providerName, item.ProviderMessageId, item.CreatedAt, item.LastError);
}

public sealed class FileEmailDeliveryTransport(IConfiguration configuration, TimeProvider timeProvider) : IEmailDeliveryTransport
{
    public string ProviderName => "Local file email";

    public async Task<EmailTransportResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var root = configuration["Email:LocalOutboxRoot"] ?? Environment.GetEnvironmentVariable("NESTYSTAY_EMAIL_OUTBOX_ROOT") ??
                   Path.Combine(Path.GetTempPath(), "nestystay-email-outbox");
        Directory.CreateDirectory(root);
        var id = $"{timeProvider.GetUtcNow():yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.eml";
        var target = Path.Combine(Path.GetFullPath(root), id);
        var temp = target + ".tmp";
        var textBody = message.TextBody ?? (!message.IsHtml ? message.Body : null);
        var htmlBody = message.HtmlBody ?? (message.IsHtml ? message.Body : null);
        var headers = $"To: {message.To}\nSubject: {message.Subject}\n" +
                      (string.IsNullOrWhiteSpace(message.ReplyToEmail) ? string.Empty : $"Reply-To: {message.ReplyToEmail}\n");
        var content = textBody is not null && htmlBody is not null
            ? $"{headers}MIME-Version: 1.0\nContent-Type: multipart/alternative; boundary=nesty-stay-boundary\n\n--nesty-stay-boundary\nContent-Type: text/plain; charset=utf-8\n\n{textBody}\n--nesty-stay-boundary\nContent-Type: text/html; charset=utf-8\n\n{htmlBody}\n--nesty-stay-boundary--\n"
            : $"{headers}Content-Type: {(htmlBody is not null ? "text/html" : "text/plain")}; charset=utf-8\n\n{htmlBody ?? textBody ?? message.Body}\n";
        await File.WriteAllTextAsync(temp, content, cancellationToken);
        File.Move(temp, target, true);
        return new EmailTransportResult(true, id);
    }
}

public sealed class BrevoEmailDeliveryTransport(HttpClient httpClient, IConfiguration configuration) : IEmailDeliveryTransport
{
    public string ProviderName => "Brevo transactional email";

    public async Task<EmailTransportResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Email:Brevo:ApiKey"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");
        var senderEmail = configuration["Email:Brevo:SenderEmail"] ?? Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL");
        var senderName = configuration["Email:Brevo:SenderName"] ?? Environment.GetEnvironmentVariable("BREVO_SENDER_NAME") ?? "NestyStay";
        var replyToEmail = message.ReplyToEmail ?? configuration["Email:Brevo:ReplyToEmail"] ?? Environment.GetEnvironmentVariable("BREVO_REPLY_TO_EMAIL");
        var replyToName = message.ReplyToName ?? configuration["Email:Brevo:ReplyToName"] ?? Environment.GetEnvironmentVariable("BREVO_REPLY_TO_NAME");
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
        {
            return new EmailTransportResult(false, Error: "Brevo credentials are not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { email = senderEmail, name = senderName },
            to = new[] { new { email = message.To } },
            subject = message.Subject,
            textContent = message.TextBody ?? (!message.IsHtml ? message.Body : null),
            htmlContent = message.HtmlBody ?? (message.IsHtml ? message.Body : null),
            replyTo = string.IsNullOrWhiteSpace(replyToEmail) ? null : new { email = replyToEmail, name = replyToName },
            tags = new[] { "nesty-stay" }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = payload.Length > 500 ? payload[..500] : payload;
            return new EmailTransportResult(false, Error: $"Brevo returned {(int)response.StatusCode}: {detail}");
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            return new EmailTransportResult(true, document.RootElement.TryGetProperty("messageId", out var id) ? id.GetString() : null);
        }
        catch (JsonException)
        {
            return new EmailTransportResult(true);
        }
    }
}

public sealed class EmailDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IEmailDeliveryTransport transport,
    TimeProvider timeProvider,
    ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Email delivery worker failed; the next poll will retry.");
            }

            await Task.Delay(PollInterval, timeProvider, stoppingToken);
        }
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var now = timeProvider.GetUtcNow();
        var pending = await db.NotificationQueue
            .Where(item => item.Channel == "Email" && !item.IsDeleted &&
                          (item.DeliveryStatus == "PENDING" || item.DeliveryStatus == "RETRYING") &&
                          (item.NextAttemptAt == null || item.NextAttemptAt <= now))
            .OrderBy(item => item.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var item in pending)
        {
            item.DeliveryStatus = "PROCESSING";
            item.AttemptCount++;
            item.UpdatedAt = now;
        }
        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var item in pending)
        {
            var result = await transport.SendAsync(new EmailMessage(
                item.Recipient,
                item.Subject,
                item.Body,
                item.Id,
                TextBody: item.TextBody,
                HtmlBody: item.HtmlBody,
                ReplyToEmail: item.ReplyToEmail,
                ReplyToName: item.ReplyToName), cancellationToken);
            item.UpdatedAt = timeProvider.GetUtcNow();
            if (result.Success)
            {
                item.DeliveryStatus = "SENT";
                item.Status = NotificationStatus.Sent;
                item.SentAt = item.UpdatedAt;
                item.ProviderMessageId = result.ProviderMessageId;
                item.LastError = null;
                item.NextAttemptAt = null;
            }
            else if (item.AttemptCount >= MaxAttempts)
            {
                item.DeliveryStatus = "DEAD_LETTER";
                item.Status = NotificationStatus.Failed;
                item.LastError = result.Error;
                item.NextAttemptAt = null;
            }
            else
            {
                item.DeliveryStatus = "RETRYING";
                item.Status = NotificationStatus.Failed;
                item.LastError = result.Error;
                item.NextAttemptAt = item.UpdatedAt.AddMinutes(Math.Pow(2, item.AttemptCount - 1));
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
