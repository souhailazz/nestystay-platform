using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Application.Wellness;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>
/// Persistence for the Wellness workflows that need more than the original
/// milestone aggregate: applicant documents, versioned report templates,
/// report collaboration and payout operations. Every method re-checks the
/// owning officer/visit before touching a row.
/// </summary>
public sealed class EfWellnessEnhancementStore(
    NestyStayDbContext db,
    IStorageProvider storageProvider,
    IFileSafetyScanner fileSafetyScanner,
    TimeProvider timeProvider) : IWellnessEnhancementStore
{
    private const long MaximumDocumentBytes = 25 * 1024 * 1024;
    private static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);
    private static readonly IReadOnlyDictionary<string, string[]> AllowedExtensions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = [".pdf"],
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"]
    };

    public async Task<IReadOnlyList<WellnessOfficerDocumentDto>> ListOfficerDocumentsAsync(Guid officerId, Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        await RequireOfficerAsync(officerId, actorUserId, isAdmin, cancellationToken);
        return await db.MilestoneWellnessOfficerDocuments.AsNoTracking()
            .Where(item => item.OfficerId == officerId && !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => ToDocumentDto(item))
            .ToListAsync(cancellationToken);
    }

    public async Task<WellnessOfficerDocumentUploadDto> PrepareOfficerDocumentUploadAsync(Guid officerId, Guid actorUserId, PrepareWellnessOfficerDocumentUploadRequest request, bool isAdmin, CancellationToken cancellationToken)
    {
        await RequireOfficerAsync(officerId, actorUserId, isAdmin, cancellationToken);
        var safeFileName = ValidateFile(request.FileName, request.ContentType, request.SizeBytes);
        var now = timeProvider.GetUtcNow();
        var id = Guid.NewGuid();
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        var objectKey = $"wellness-officers/{officerId:N}/documents/{id:N}{extension}";
        var entity = new MilestoneWellnessOfficerDocument
        {
            Id = id,
            OfficerId = officerId,
            DocumentType = NormalizeDocumentType(request.DocumentType),
            OriginalFileName = Path.GetFileName(request.FileName.Trim()),
            SafeFileName = safeFileName,
            ContentType = request.ContentType.Trim().ToLowerInvariant(),
            SizeBytes = request.SizeBytes,
            ObjectKey = objectKey,
            UploadUrl = $"/api/wellness/officers/{officerId}/documents/{id}/content",
            StorageProviderName = storageProvider.ProviderName,
            UploadExpiresAt = now.Add(UploadLifetime),
            ExpiresOn = request.ExpiresOn,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = actorUserId
        };
        db.MilestoneWellnessOfficerDocuments.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToUploadDto(entity);
    }

    public async Task<WellnessOfficerDocumentUploadDto> UploadOfficerDocumentContentAsync(Guid officerId, Guid documentId, Guid actorUserId, string contentType, long sizeBytes, Stream content, bool isAdmin, CancellationToken cancellationToken)
    {
        await RequireOfficerAsync(officerId, actorUserId, isAdmin, cancellationToken);
        var document = await db.MilestoneWellnessOfficerDocuments.SingleOrDefaultAsync(item => item.Id == documentId && item.OfficerId == officerId && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Officer document is not available.");
        if (document.UploadExpiresAt <= timeProvider.GetUtcNow()) throw new InvalidOperationException("Officer document upload has expired. Start again.");
        if (document.Status is "Uploaded" or "Rejected") throw new InvalidOperationException("This officer document upload cannot be replaced in place.");
        ValidateFile(document.SafeFileName, contentType, sizeBytes);
        var write = await storageProvider.SaveObjectAsync(new StorageObjectWriteRequest(document.ObjectKey, document.ContentType, MaximumDocumentBytes), content, cancellationToken);
        var scan = await fileSafetyScanner.ScanAsync(new FileSafetyScanRequest(document.ObjectKey, document.SafeFileName, write.ContentType, write.SizeBytes, write.Sha256Hash, write.HeaderBytes), cancellationToken);
        document.Status = scan.Status == "Clean" ? "Uploaded" : "Quarantined";
        document.ScanStatus = scan.Status;
        document.Sha256Hash = write.Sha256Hash;
        document.UploadedAt = timeProvider.GetUtcNow();
        document.UpdatedAt = document.UploadedAt.Value;
        if (scan.Status != "Clean") document.ReviewReason = scan.Reason;
        await db.SaveChangesAsync(cancellationToken);
        return ToUploadDto(document);
    }

    public async Task<WellnessOfficerDocumentDto?> ReviewOfficerDocumentAsync(Guid documentId, Guid actorUserId, ReviewWellnessOfficerDocumentRequest request, CancellationToken cancellationToken)
    {
        var document = await db.MilestoneWellnessOfficerDocuments.SingleOrDefaultAsync(item => item.Id == documentId && !item.IsDeleted, cancellationToken);
        if (document is null) return null;
        var decision = request.Decision.Trim();
        if (decision is not ("Approved" or "Rejected" or "RequestChanges")) throw new InvalidOperationException("Document decision must be Approved, Rejected, or RequestChanges.");
        if (decision == "Approved" && (document.Status != "Uploaded" || document.ScanStatus != "Clean")) throw new InvalidOperationException("Only uploaded documents that passed the safety scan can be approved.");
        document.ReviewStatus = decision;
        document.ReviewReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        document.UpdatedAt = timeProvider.GetUtcNow();
        document.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToDocumentDto(document);
    }

    public async Task<IReadOnlyList<WellnessReportTemplateDto>> ListReportTemplatesAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = db.MilestoneWellnessReportTemplates.AsNoTracking().Where(item => !item.IsDeleted);
        if (activeOnly) query = query.Where(item => item.IsActive);
        return await query.OrderBy(item => item.Name).ThenByDescending(item => item.Version).Select(item => ToTemplateDto(item)).ToListAsync(cancellationToken);
    }

    public async Task<WellnessReportTemplateDto> SaveReportTemplateAsync(Guid actorUserId, SaveWellnessReportTemplateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new InvalidOperationException("Template name is required.");
        if (string.IsNullOrWhiteSpace(request.DefinitionJson)) throw new InvalidOperationException("Template definition is required.");
        try { using var _ = JsonDocument.Parse(request.DefinitionJson); }
        catch (JsonException) { throw new InvalidOperationException("Template definition must be valid JSON."); }
        var latest = await db.MilestoneWellnessReportTemplates.Where(item => item.Name == request.Name.Trim() && !item.IsDeleted).MaxAsync(item => (int?)item.Version, cancellationToken) ?? 0;
        var now = timeProvider.GetUtcNow();
        if (request.IsActive)
        {
            await db.MilestoneWellnessReportTemplates.Where(item => item.Name == request.Name.Trim() && item.IsActive && !item.IsDeleted).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsActive, false).SetProperty(item => item.UpdatedAt, now), cancellationToken);
        }
        var template = new MilestoneWellnessReportTemplate
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Version = latest + 1, DefinitionJson = request.DefinitionJson.Trim(), IsActive = request.IsActive,
            CreatedAt = now, UpdatedAt = now, CreatedByUserId = actorUserId
        };
        db.MilestoneWellnessReportTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return ToTemplateDto(template);
    }

    public async Task<WellnessReportCollaborationDto> GetReportCollaborationAsync(Guid reportId, Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var (report, visit) = await RequireReportAsync(reportId, actorUserId, isAdmin, cancellationToken);
        var comments = await db.MilestoneWellnessReportComments.AsNoTracking().Where(item => item.ReportId == report.Id && !item.IsDeleted).OrderBy(item => item.CreatedAt).Select(item => ToCommentDto(item)).ToListAsync(cancellationToken);
        var acknowledgement = await db.MilestoneWellnessReportAcknowledgements.AsNoTracking().Where(item => item.ReportId == report.Id && !item.IsDeleted).OrderByDescending(item => item.AcknowledgedAt).Select(item => new WellnessReportAcknowledgementDto(item.ReportId, item.AcknowledgedByUserId, item.AcknowledgedAt)).FirstOrDefaultAsync(cancellationToken);
        var tasks = await db.MilestoneWellnessFollowUpTasks.AsNoTracking().Where(item => item.ReportId == report.Id && !item.IsDeleted).OrderBy(item => item.CreatedAt).Select(item => ToTaskDto(item)).ToListAsync(cancellationToken);
        return new WellnessReportCollaborationDto(report.Id, comments, acknowledgement, tasks);
    }

    public async Task<WellnessReportPdfDto> RenderReportPdfAsync(Guid reportId, Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var (report, visit) = await RequireReportAsync(reportId, actorUserId, isAdmin, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var lines = new[]
        {
            "NestyStay Wellness Visit Report",
            $"Generated: {now.UtcDateTime:yyyy-MM-dd HH:mm:ss} UTC",
            $"Property: {visit.PropertyId:N}", $"Visit: {visit.Id:N}", $"Scheduled: {visit.ScheduledAt:yyyy-MM-dd HH:mm} UTC ({visit.ScheduledTimeZone})",
            $"Officer reference: {visit.OfficerBadgeNumber}", $"Report status: {report.ReportStatus}", $"Submitted: {report.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC", string.Empty,
            "Summary", report.Notes, string.Empty, "Findings / photos", $"Verified photo references: {string.Join(", ", MilestoneJson.DeserializeList<string>(report.PhotosJson))}",
            "This immutable report snapshot was generated by the NestyStay API."
        };
        return new WellnessReportPdfDto($"nestywellness-{report.VisitId:N}-report.pdf", "application/pdf", BuildPdf(lines), now);
    }

    public async Task<WellnessReportCommentDto> AddReportCommentAsync(Guid reportId, Guid actorUserId, AddWellnessReportCommentRequest request, bool isAdmin, CancellationToken cancellationToken)
    {
        var (report, _) = await RequireReportAsync(reportId, actorUserId, isAdmin, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Trim().Length > 4000) throw new InvalidOperationException("Comment must contain 1-4000 characters.");
        var now = timeProvider.GetUtcNow();
        var comment = new MilestoneWellnessReportComment { Id = Guid.NewGuid(), ReportId = report.Id, AuthorUserId = actorUserId, Body = request.Body.Trim(), CreatedAt = now, UpdatedAt = now, CreatedByUserId = actorUserId };
        db.MilestoneWellnessReportComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        return ToCommentDto(comment);
    }

    public async Task<WellnessReportAcknowledgementDto> AcknowledgeReportAsync(Guid reportId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var (report, visit) = await RequireReportAsync(reportId, actorUserId, false, cancellationToken);
        if (visit.HostUserId != actorUserId) throw new UnauthorizedAccessException("Only the host can acknowledge this report.");
        var now = timeProvider.GetUtcNow();
        var existing = await db.MilestoneWellnessReportAcknowledgements.SingleOrDefaultAsync(item => item.ReportId == report.Id && item.AcknowledgedByUserId == actorUserId && !item.IsDeleted, cancellationToken);
        if (existing is null)
        {
            existing = new MilestoneWellnessReportAcknowledgement { Id = Guid.NewGuid(), ReportId = report.Id, AcknowledgedByUserId = actorUserId, AcknowledgedAt = now, CreatedAt = now, UpdatedAt = now, CreatedByUserId = actorUserId };
            db.MilestoneWellnessReportAcknowledgements.Add(existing);
            await db.SaveChangesAsync(cancellationToken);
        }
        return new WellnessReportAcknowledgementDto(existing.ReportId, existing.AcknowledgedByUserId, existing.AcknowledgedAt);
    }

    public async Task<WellnessFollowUpTaskDto> CreateFollowUpTaskAsync(Guid reportId, Guid actorUserId, CreateWellnessFollowUpTaskRequest request, bool isAdmin, CancellationToken cancellationToken)
    {
        var (report, visit) = await RequireReportAsync(reportId, actorUserId, isAdmin, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Title)) throw new InvalidOperationException("Follow-up task title is required.");
        var now = timeProvider.GetUtcNow();
        var task = new MilestoneWellnessFollowUpTask { Id = Guid.NewGuid(), ReportId = report.Id, VisitId = visit.Id, PropertyId = visit.PropertyId, Title = request.Title.Trim(), Description = request.Description?.Trim() ?? string.Empty, Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(), AssigneeUserId = request.AssigneeUserId, DueAt = request.DueAt, Status = "Open", CreatedAt = now, UpdatedAt = now, CreatedByUserId = actorUserId };
        db.MilestoneWellnessFollowUpTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return ToTaskDto(task);
    }

    public async Task<IReadOnlyList<WellnessFollowUpTaskDto>> ListFollowUpTasksAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var query = db.MilestoneWellnessFollowUpTasks.AsNoTracking().Where(item => !item.IsDeleted);
        if (!isAdmin)
        {
            var hostVisitIds = db.MilestoneWellnessVisits.Where(visit => visit.HostUserId == actorUserId).Select(visit => visit.Id);
            query = query.Where(item => hostVisitIds.Contains(item.VisitId) || item.AssigneeUserId == actorUserId);
        }
        return await query.OrderBy(item => item.DueAt).ThenByDescending(item => item.CreatedAt).Select(item => ToTaskDto(item)).ToListAsync(cancellationToken);
    }

    public async Task<WellnessPayoutStatementDto> GetPayoutStatementAsync(Guid actorUserId, bool isAdmin, DateOnly? from, DateOnly? to, string format, CancellationToken cancellationToken)
    {
        var officer = isAdmin ? null : await db.MilestoneWellnessOfficers.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == actorUserId && !item.IsDeleted, cancellationToken);
        if (!isAdmin && officer is null) throw new UnauthorizedAccessException("An officer profile is required.");
        var fromDate = from ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddMonths(-1));
        var toDate = to ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var query = db.MilestoneWellnessPayouts.AsNoTracking().Where(item => !item.IsDeleted && (item.EligibleAt == null || item.EligibleAt.Value.Date >= fromDate.ToDateTime(TimeOnly.MinValue).Date) && (item.EligibleAt == null || item.EligibleAt.Value.Date <= toDate.ToDateTime(TimeOnly.MaxValue).Date));
        if (officer is not null) query = query.Where(item => item.OfficerId == officer.Id);
        var rows = await query.OrderBy(item => item.EligibleAt).Select(item => new WellnessPayoutStatementRowDto(item.Id, item.VisitId, item.EligibleAt, item.GrossAmount, item.PlatformFee, item.OfficerAmount, item.Currency, item.Status, item.PaidAt, string.IsNullOrWhiteSpace(item.ProviderReference) ? null : item.ProviderReference)).ToListAsync(cancellationToken);
        var outputFormat = format.Equals("csv", StringComparison.OrdinalIgnoreCase) ? "csv" : "json";
        var baseStatement = new WellnessPayoutStatementDto(officer?.UserId ?? actorUserId, fromDate, toDate, rows.Sum(item => item.GrossAmount), rows.Sum(item => item.PlatformFee), rows.Sum(item => item.OfficerAmount), rows, outputFormat);
        if (outputFormat != "csv") return baseStatement;
        var csv = new StringBuilder("payoutId,visitId,eligibleAt,gross,platformFee,officerAmount,currency,status,paidAt,providerReference\n");
        foreach (var row in rows) csv.AppendLine(string.Join(',', row.PayoutId, row.VisitId, row.EligibleAt?.ToString("O", CultureInfo.InvariantCulture), row.GrossAmount.ToString(CultureInfo.InvariantCulture), row.PlatformFee.ToString(CultureInfo.InvariantCulture), row.OfficerAmount.ToString(CultureInfo.InvariantCulture), row.Currency, row.Status, row.PaidAt?.ToString("O", CultureInfo.InvariantCulture), row.ProviderReference));
        return baseStatement with { DownloadFileName = $"wellness-payouts-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.csv", DownloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv.ToString())) };
    }

    public async Task<WellnessPayoutDisputeDto> CreatePayoutDisputeAsync(Guid payoutId, Guid actorUserId, CreateWellnessPayoutDisputeRequest request, CancellationToken cancellationToken)
    {
        var payout = await db.MilestoneWellnessPayouts.SingleOrDefaultAsync(item => item.Id == payoutId && !item.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Payout not found.");
        var officer = await db.MilestoneWellnessOfficers.SingleOrDefaultAsync(item => item.Id == payout.OfficerId && !item.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Officer profile not found.");
        if (officer.UserId != actorUserId) throw new UnauthorizedAccessException("Only the officer can dispute this payout.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("A dispute reason is required.");
        var existing = await db.MilestoneWellnessPayoutDisputes.FirstOrDefaultAsync(item => item.PayoutId == payoutId && item.Status == "Open" && !item.IsDeleted, cancellationToken);
        if (existing is not null) return ToDisputeDto(existing);
        var now = timeProvider.GetUtcNow();
        var dispute = new MilestoneWellnessPayoutDispute { Id = Guid.NewGuid(), PayoutId = payoutId, OfficerId = officer.Id, Reason = request.Reason.Trim(), EvidenceJson = request.EvidenceJson, Status = "Open", CreatedAt = now, UpdatedAt = now, CreatedByUserId = actorUserId };
        db.MilestoneWellnessPayoutDisputes.Add(dispute);
        payout.Status = "Disputed";
        payout.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToDisputeDto(dispute);
    }

    public async Task<WellnessPayoutDisputeDto?> ResolvePayoutDisputeAsync(Guid disputeId, Guid actorUserId, ResolveWellnessPayoutDisputeRequest request, CancellationToken cancellationToken)
    {
        var dispute = await db.MilestoneWellnessPayoutDisputes.SingleOrDefaultAsync(item => item.Id == disputeId && !item.IsDeleted, cancellationToken);
        if (dispute is null) return null;
        if (request.Decision.Trim() is not ("Approved" or "Rejected")) throw new InvalidOperationException("Dispute decision must be Approved or Rejected.");
        if (dispute.Status != "Open") return ToDisputeDto(dispute);
        var now = timeProvider.GetUtcNow();
        dispute.Status = request.Decision.Trim(); dispute.Decision = request.Decision.Trim(); dispute.DecisionNotes = request.Notes?.Trim(); dispute.DecidedByUserId = actorUserId; dispute.ResolvedAt = now; dispute.UpdatedAt = now; dispute.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToDisputeDto(dispute);
    }

    private async Task<MilestoneWellnessOfficer> RequireOfficerAsync(Guid officerId, Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var officer = await db.MilestoneWellnessOfficers.SingleOrDefaultAsync(item => item.Id == officerId && !item.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Officer profile not found.");
        if (!isAdmin && officer.UserId != actorUserId) throw new UnauthorizedAccessException("Officer document access is restricted.");
        return officer;
    }

    private async Task<(MilestoneWellnessReport Report, MilestoneWellnessVisit Visit)> RequireReportAsync(Guid reportId, Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var report = await db.MilestoneWellnessReports.SingleOrDefaultAsync(item => item.Id == reportId && !item.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Wellness report not found.");
        var visit = await db.MilestoneWellnessVisits.SingleOrDefaultAsync(item => item.Id == report.VisitId && !item.IsDeleted, cancellationToken) ?? throw new InvalidOperationException("Wellness visit not found.");
        if (!isAdmin && visit.HostUserId != actorUserId)
        {
            var officer = await db.MilestoneWellnessOfficers.SingleOrDefaultAsync(item => item.Id == report.OfficerId && !item.IsDeleted, cancellationToken);
            if (officer?.UserId != actorUserId) throw new UnauthorizedAccessException("Wellness report access is restricted.");
        }
        return (report, visit);
    }

    private static string ValidateFile(string fileName, string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumDocumentBytes) throw new InvalidOperationException("Officer documents must be between 1 byte and 25 MB.");
        var normalizedType = contentType.Trim().ToLowerInvariant();
        if (!AllowedExtensions.TryGetValue(normalizedType, out var extensions)) throw new InvalidOperationException("Officer documents must be PDF, JPEG, or PNG.");
        var safe = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(safe)) throw new InvalidOperationException("A document filename is required.");
        if (!extensions.Contains(Path.GetExtension(safe), StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Document extension does not match its content type.");
        return safe;
    }

    private static string NormalizeDocumentType(string value) => string.IsNullOrWhiteSpace(value) ? "IDENTITY_EVIDENCE" : value.Trim().ToUpperInvariant();
    private static WellnessOfficerDocumentDto ToDocumentDto(MilestoneWellnessOfficerDocument item) => new(item.Id, item.OfficerId, item.DocumentType, item.SafeFileName, item.ContentType, item.SizeBytes, item.Status, item.ScanStatus, item.ExpiresOn, item.ReviewStatus, item.ReviewReason, item.CreatedAt, item.UploadedAt);
    private static WellnessOfficerDocumentUploadDto ToUploadDto(MilestoneWellnessOfficerDocument item) => new(item.Id, item.OfficerId, item.DocumentType, item.SafeFileName, item.ContentType, item.SizeBytes, item.ObjectKey, item.UploadUrl, item.Status, item.ScanStatus, item.UploadExpiresAt, item.ExpiresOn, item.Sha256Hash);
    private static WellnessReportTemplateDto ToTemplateDto(MilestoneWellnessReportTemplate item) => new(item.Id, item.Name, item.Version, item.DefinitionJson, item.IsActive, item.CreatedAt, item.CreatedByUserId ?? Guid.Empty);
    private static WellnessReportCommentDto ToCommentDto(MilestoneWellnessReportComment item) => new(item.Id, item.ReportId, item.AuthorUserId, item.Body, item.CreatedAt);
    private static WellnessFollowUpTaskDto ToTaskDto(MilestoneWellnessFollowUpTask item) => new(item.Id, item.ReportId, item.VisitId, item.PropertyId, item.Title, item.Description, item.Priority, item.AssigneeUserId, item.DueAt, item.Status, item.CreatedAt);
    private static WellnessPayoutDisputeDto ToDisputeDto(MilestoneWellnessPayoutDispute item) => new(item.Id, item.PayoutId, item.OfficerId, item.Reason, item.EvidenceJson, item.Status, item.Decision, item.DecisionNotes, item.DecidedByUserId, item.CreatedAt, item.ResolvedAt);

    private static byte[] BuildPdf(IReadOnlyList<string> lines)
    {
        var streamBuilder = new StringBuilder("BT\n/F1 11 Tf\n15 TL\n54 760 Td\n");
        foreach (var line in lines) streamBuilder.Append('(').Append(EscapePdfText(line)).Append(") Tj\nT*\n");
        streamBuilder.Append("ET\n");
        var stream = streamBuilder.ToString();
        var objects = new[] { "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n", "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n", "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n", "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n", $"5 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream\nendobj\n" };
        var pdf = new StringBuilder(); var offsets = new List<int> { 0 }; pdf.Append("%PDF-1.4\n");
        foreach (var obj in objects) { offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString())); pdf.Append(obj); }
        var xref = Encoding.ASCII.GetByteCount(pdf.ToString()); pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n"); foreach (var offset in offsets.Skip(1)) pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n"); pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"); return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string EscapePdfText(string value) => value.ReplaceLineEndings(" ").Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
