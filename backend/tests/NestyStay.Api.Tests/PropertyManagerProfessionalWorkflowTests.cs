using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class PropertyManagerProfessionalWorkflowTests(NestyStayApiFactory factory) : IClassFixture<NestyStayApiFactory>
{
    [Fact]
    public async Task ManagedPropertyCanLinkOnlyItsOwnersRentalListing()
    {
        using var client = factory.CreateClient();
        var portfolio = await CreatePortfolio(client);
        using var ownerClient = factory.CreateClient();
        SignIn(ownerClient, portfolio.Owner, UserRole.Host);
        var listing = await Ok(ownerClient.PostAsJsonAsync("/api/properties", new { hostUserId = portfolio.Owner, hostName = "Owner", hostEmail = portfolio.OwnerEmail, title = "Rental listing", location = "Kingston", country = "Jamaica", nightlyRate = 100, currency = "JMD", badgeLevel = "Free", cancellationPolicy = "Flexible" }));
        var listingId = listing.GetProperty("id").GetGuid();
        var linked = await Ok(client.PatchAsJsonAsync($"/api/property-manager/properties/{portfolio.Property}/rental-listing", new { rentalListingId = listingId }));
        Assert.Equal(listingId, linked.GetProperty("rentalListingId").GetGuid());
        var second = await Ok(client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = portfolio.Owner, title = "Second managed property", unitNumber = "P-2", address = "Kingston" }));
        var rejected = await client.PatchAsJsonAsync($"/api/property-manager/properties/{second.GetProperty("id").GetGuid()}/rental-listing", new { rentalListingId = listingId });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var guestEmail = $"guest-{Guid.NewGuid():N}@nestystay.local";
        var guest = await Ok(ownerClient.PostAsJsonAsync("/api/auth/register", new { email = guestEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Guest", acceptedTerms = true, acceptedPrivacy = true, role = "Guest" }));
        var guestId = guest.GetProperty("userId").GetGuid();
        SignIn(ownerClient, guestId, UserRole.Guest);
        var booking = await Ok(ownerClient.PostAsJsonAsync("/api/bookings", new { propertyId = listingId, guestUserId = guestId, checkIn = "2099-03-10", checkOut = "2099-03-12" }));
        var bookingId = booking.GetProperty("id").GetGuid();
        SignIn(client, portfolio.Manager, UserRole.PropertyManager);
        var listed = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/reservations");
        Assert.Contains(listed!, item => item.GetProperty("bookingId").GetGuid() == bookingId);
        var preview = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/date-change-preview", new { checkIn = "2099-03-10", checkOut = "2099-03-13" }));
        Assert.False(preview.GetProperty("allowed").GetBoolean());
        Assert.Contains("cancelled and rebooked", preview.GetProperty("blockingReason").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(preview.GetProperty("requiresRebooking").GetBoolean());
        var amendPreview = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/date-change-preview", new { checkIn = "2099-03-15", checkOut = "2099-03-17" }));
        Assert.True(amendPreview.GetProperty("allowed").GetBoolean());
        var reservationBeforeAmend = listed!.Single(item => item.GetProperty("bookingId").GetGuid() == bookingId);
        var amendKey = $"amend-{bookingId:N}";
        var amended = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/amend", new { checkIn = "2099-03-15", checkOut = "2099-03-17", reason = "Guest shifted travel", idempotencyKey = amendKey, expectedUpdatedAt = reservationBeforeAmend.GetProperty("updatedAt").GetDateTimeOffset() }));
        Assert.Equal("2099-03-15", amended.GetProperty("checkIn").GetDateTimeOffset().ToString("yyyy-MM-dd"));
        var repeatedAmendment = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/amend", new { checkIn = "2099-03-15", checkOut = "2099-03-17", reason = "Guest shifted travel", idempotencyKey = amendKey, expectedUpdatedAt = reservationBeforeAmend.GetProperty("updatedAt").GetDateTimeOffset() }));
        Assert.Equal(bookingId, repeatedAmendment.GetProperty("bookingId").GetGuid());
        var changedAmendmentReplay = await client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/amend", new { checkIn = "2099-03-16", checkOut = "2099-03-18", reason = "Different payload", idempotencyKey = amendKey });
        Assert.Equal(HttpStatusCode.BadRequest, changedAmendmentReplay.StatusCode);
        var directCancel = await client.PatchAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}", new { status = "CANCELLED" });
        Assert.Equal(HttpStatusCode.BadRequest, directCancel.StatusCode);
        var cancelled = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/cancel", new { reason = "Guest requested cancellation", idempotencyKey = $"cancel-{bookingId:N}", expectedUpdatedAt = amended.GetProperty("updatedAt").GetDateTimeOffset() }));
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
        var repeated = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/cancel", new { reason = "Guest requested cancellation", idempotencyKey = $"cancel-{bookingId:N}" }));
        Assert.Equal(bookingId, repeated.GetProperty("bookingId").GetGuid());
        var rebookKey = $"rebook-{bookingId:N}";
        var rebooked = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/rebook", new { checkIn = "2099-03-20", checkOut = "2099-03-22", reason = "Replacement dates confirmed", idempotencyKey = rebookKey }));
        var replacementId = rebooked.GetProperty("replacement").GetProperty("bookingId").GetGuid();
        Assert.NotEqual(bookingId, replacementId);
        Assert.False(rebooked.GetProperty("replayed").GetBoolean());
        var repeatedRebook = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/rebook", new { checkIn = "2099-03-20", checkOut = "2099-03-22", reason = "Replacement dates confirmed", idempotencyKey = rebookKey }));
        Assert.Equal(replacementId, repeatedRebook.GetProperty("replacement").GetProperty("bookingId").GetGuid());
        Assert.True(repeatedRebook.GetProperty("replayed").GetBoolean());
        using var otherManagerClient = factory.CreateClient();
        _ = await CreatePortfolio(otherManagerClient);
        Assert.Equal(HttpStatusCode.NotFound, (await otherManagerClient.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/rebook", new { checkIn = "2099-04-01", checkOut = "2099-04-03", reason = "Forged operation", idempotencyKey = Guid.NewGuid().ToString("N") })).StatusCode);
        var history = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/reservations/{bookingId}/history");
        Assert.Contains(history!, item => item.GetProperty("eventType").GetString() == "AMENDED");
        Assert.Contains(history!, item => item.GetProperty("eventType").GetString() == "CANCELLED");
        Assert.Contains(history!, item => item.GetProperty("eventType").GetString() == "REBOOKED" && item.GetProperty("relatedBookingId").GetGuid() == replacementId);
    }

    [Fact]
    public async Task MasterCalendarFiltersWorkOrdersAndExplainsOperationalOverlaps()
    {
        using var client = factory.CreateClient();
        var portfolio = await CreatePortfolio(client);
        var starts = new DateTimeOffset(2099, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var block = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/owner-blocks", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, startsAt = starts, endsAt = starts.AddDays(2), reason = "Owner visit" }));
        var workOrder = await Ok(client.PostAsJsonAsync("/api/property-manager/work-orders", new { propertyId = portfolio.Property, ownerUserId = portfolio.Owner, scope = "Repair balcony rail" }));
        _ = await Ok(client.PatchAsJsonAsync($"/api/property-manager/work-orders/{workOrder.GetProperty("id").GetGuid()}", new { status = "SCHEDULED", laborAmount = 0, partsAmount = 0, scheduledAt = starts.AddHours(2) }));

        var from = Uri.EscapeDataString(starts.AddDays(-1).ToString("O"));
        var to = Uri.EscapeDataString(starts.AddDays(3).ToString("O"));
        var rows = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/calendar?from={from}&to={to}&ownerUserId={portfolio.Owner}");
        var ownerBlock = Assert.Single(rows!, item => item.GetProperty("sourceId").GetGuid() == block.GetProperty("id").GetGuid());
        Assert.Equal("WARNING", ownerBlock.GetProperty("conflictLevel").GetString());
        Assert.Contains(ownerBlock.GetProperty("conflicts").EnumerateArray(), item => item.GetProperty("type").GetString() == "WORK_ORDER");
        var filtered = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/calendar?from={from}&to={to}&propertyId={portfolio.Property}&eventType=WORK_ORDER");
        Assert.Single(filtered!);
        Assert.Equal("WORK_ORDER", filtered![0].GetProperty("type").GetString());
        Assert.Contains("/pm/work-orders", filtered[0].GetProperty("relatedPath").GetString());

        using var otherManager = factory.CreateClient();
        _ = await CreatePortfolio(otherManager);
        var crossPortfolio = await otherManager.GetAsync($"/api/property-manager/professional/calendar?from={from}&to={to}&propertyId={portfolio.Property}");
        Assert.Equal(HttpStatusCode.BadRequest, crossPortfolio.StatusCode);
    }

    [Fact]
    public async Task OwnerBlockRejectsReservationOverlapAndCanBeCancelled()
    {
        using var client = factory.CreateClient();
        var portfolio = await CreatePortfolio(client);
        var starts = DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(10); var ends = starts.AddDays(2);
        var block = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/owner-blocks", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, startsAt = starts, endsAt = ends, timeZone = "America/Jamaica", reason = "Owner stay" }));
        Assert.Equal("ACTIVE", block.GetProperty("status").GetString());
        var duplicate = await client.PostAsJsonAsync("/api/property-manager/professional/owner-blocks", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, startsAt = starts.AddHours(1), endsAt = ends.AddHours(1), reason = "Overlap" });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var cancelled = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/owner-blocks/{block.GetProperty("id").GetGuid()}/cancel", new { reason = "Owner changed plans", rowVersion = block.GetProperty("rowVersion").GetInt64() }));
        Assert.Equal("CANCELLED", cancelled.GetProperty("status").GetString());
        var blockHistory = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/owner-blocks/{block.GetProperty("id").GetGuid()}/history");
        Assert.NotNull(blockHistory); Assert.Contains(blockHistory!, item => item.GetProperty("action").GetString() == "OwnerBlockCancelled");

        using var ownerClient = factory.CreateClient(); SignIn(ownerClient, portfolio.Owner, UserRole.Owner);
        var ownerBlock = await Ok(ownerClient.PostAsJsonAsync("/api/property-manager/owner/owner-blocks", new { propertyId = portfolio.Property, startsAt = starts.AddDays(5), endsAt = ends.AddDays(5), reason = "Owner weekend", category = "PERSONAL", notes = "Owner-only operational request" }));
        Assert.Equal("PERSONAL", ownerBlock.GetProperty("category").GetString());
        Assert.NotEmpty((await ownerClient.GetFromJsonAsync<JsonElement[]>("/api/property-manager/owner/owner-blocks")) ?? []);
    }

    [Fact]
    public async Task MaintenanceLifecycleAndAssetIncidentInspectionPersist()
    {
        using var client = factory.CreateClient(); var portfolio = await CreatePortfolio(client);
        var maintenance = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/maintenance", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, title = "Leaking tap", description = "Kitchen tap", priority = "HIGH" }));
        var id = maintenance.GetProperty("id").GetGuid();
        var attachment = await Ok(client.PostAsJsonAsync("/api/property-manager/maintenance/attachments", new { maintenanceId = id, fileName = "receipt.pdf", contentType = "application/pdf", contentBase64 = "JVBERi0xLjQK" }));
        var attachments = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/maintenance/{id}/attachments");
        Assert.Contains(attachments!, item => item.GetProperty("id").GetGuid() == attachment.GetProperty("id").GetGuid());
        var attachmentDownload = await Ok(client.GetAsync($"/api/property-manager/maintenance/{id}/attachments/{attachment.GetProperty("id").GetGuid()}/download"));
        Assert.Equal("receipt.pdf", attachmentDownload.GetProperty("fileName").GetString());
        var invalidAttachment = await client.PostAsJsonAsync("/api/property-manager/maintenance/attachments", new { maintenanceId = id, fileName = "payload.exe", contentType = "application/octet-stream", contentBase64 = "TVqQ" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidAttachment.StatusCode);
        using var otherManager = factory.CreateClient();
        _ = await CreatePortfolio(otherManager);
        var crossManagerRead = await otherManager.GetAsync($"/api/property-manager/maintenance/{id}/attachments");
        Assert.False(crossManagerRead.IsSuccessStatusCode);
        var next = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "TRIAGED", rowVersion = maintenance.GetProperty("rowVersion").GetInt64(), details = "Reviewed by manager" }));
        Assert.Equal("TRIAGED", next.GetProperty("status").GetString());
        var vendor = await Ok(client.PostAsJsonAsync("/api/property-manager/vendors", new { name = "Trusted Plumbing", category = "PLUMBING", contact = "ops@trusted.example", notes = "Preferred vendor" }));
        var quoting = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "QUOTING", rowVersion = next.GetProperty("rowVersion").GetInt64(), details = "Solicit comparable bids" }));
        var quote = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/maintenance/{id}/quotes", new { vendorId = vendor.GetProperty("id").GetGuid(), amount = 180.00m, scope = "Replace tap", currency = "JMD" }));
        var quotes = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/maintenance/{id}/quotes");
        Assert.NotNull(quotes); Assert.Contains(quotes!, item => item.GetProperty("id").GetGuid() == quote.GetProperty("id").GetGuid());
        var approvalEvidence = await Ok(client.PostAsJsonAsync("/api/property-manager/documents", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, title = "Vendor estimate", category = "MAINTENANCE", fileName = "estimate.pdf", contentType = "application/pdf", sizeBytes = 9, contentBase64 = "JVBERi0xLjQK" }));
        var approvalKey = $"approval-{id:N}";
        var approval = await Ok(client.PostAsJsonAsync("/api/property-manager/p0/approvals", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, approvalType = "MAINTENANCE", description = "Replace leaking tap", amount = 180.00m, currency = "JMD", evidenceDocumentIds = new[] { approvalEvidence.GetProperty("id").GetGuid() }, sourceType = "MAINTENANCE", sourceId = id, expiresAt = DateTimeOffset.UtcNow.AddDays(7), idempotencyKey = approvalKey }));
        Assert.Equal(id, approval.GetProperty("sourceId").GetGuid());
        Assert.Single(approval.GetProperty("evidence").EnumerateArray());
        var repeatedApproval = await Ok(client.PostAsJsonAsync("/api/property-manager/p0/approvals", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, approvalType = "MAINTENANCE", description = "Replace leaking tap", amount = 180.00m, currency = "JMD", evidenceDocumentIds = new[] { approvalEvidence.GetProperty("id").GetGuid() }, sourceType = "MAINTENANCE", sourceId = id, expiresAt = approval.GetProperty("expiresAt").GetDateTimeOffset(), idempotencyKey = approvalKey }));
        Assert.Equal(approval.GetProperty("id").GetGuid(), repeatedApproval.GetProperty("id").GetGuid());
        var managerDecision = await client.PostAsJsonAsync($"/api/property-manager/p0/approvals/{approval.GetProperty("id").GetGuid()}/decision", new { status = "APPROVED", reason = "Manager cannot self-approve", rowVersion = approval.GetProperty("rowVersion").GetInt64(), idempotencyKey = "forged-manager-decision" });
        Assert.Equal(HttpStatusCode.Unauthorized, managerDecision.StatusCode);
        using var ownerClient = factory.CreateClient();
        var ownerLogin = await ownerClient.PostAsJsonAsync("/api/auth/login", new { email = portfolio.OwnerEmail, password = portfolio.Password });
        Assert.True(ownerLogin.IsSuccessStatusCode, await ownerLogin.Content.ReadAsStringAsync());
        var ownerToken = (await ownerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var decisionKey = $"decision-{approval.GetProperty("id").GetGuid():N}";
        var decided = await Ok(ownerClient.PostAsJsonAsync($"/api/property-manager/p0/approvals/{approval.GetProperty("id").GetGuid()}/decision", new { status = "APPROVED", reason = "Approved for repair", rowVersion = approval.GetProperty("rowVersion").GetInt64(), idempotencyKey = decisionKey }));
        var repeatedDecision = await Ok(ownerClient.PostAsJsonAsync($"/api/property-manager/p0/approvals/{approval.GetProperty("id").GetGuid()}/decision", new { status = "APPROVED", reason = "Approved for repair", rowVersion = approval.GetProperty("rowVersion").GetInt64(), idempotencyKey = decisionKey }));
        Assert.Equal(decided.GetProperty("id").GetGuid(), repeatedDecision.GetProperty("id").GetGuid());
        var assigned = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "ASSIGNED", vendorId = vendor.GetProperty("id").GetGuid(), approvedAmount = 180.00m, ownerApprovalId = decided.GetProperty("id").GetGuid(), rowVersion = quoting.GetProperty("rowVersion").GetInt64(), details = "Selected lowest valid quote" }));
        Assert.Equal("ASSIGNED", assigned.GetProperty("status").GetString());
        Assert.Equal(quote.GetProperty("id").GetGuid(), assigned.GetProperty("selectedQuoteId").GetGuid());
        var scheduled = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "SCHEDULED", rowVersion = assigned.GetProperty("rowVersion").GetInt64(), ownerApprovalId = decided.GetProperty("id").GetGuid(), scheduledAt = DateTimeOffset.UtcNow.AddDays(2) }));
        var inProgress = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "IN_PROGRESS", rowVersion = scheduled.GetProperty("rowVersion").GetInt64(), ownerApprovalId = decided.GetProperty("id").GetGuid() }));
        var completed = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "COMPLETED", rowVersion = inProgress.GetProperty("rowVersion").GetInt64(), ownerApprovalId = decided.GetProperty("id").GetGuid(), expenseAmount = 180.00m, ownerCharge = 180.00m }));
        Assert.True(completed.GetProperty("financiallyPosted").GetBoolean());
        Assert.NotEqual(Guid.Empty, completed.GetProperty("financialJournalId").GetGuid());
        var asset = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/assets", new { propertyId = portfolio.Property, assetTag = "K-001", name = "Kitchen tap", description = "Chrome replacement tap", serialReference = "TAP-001", purchaseDate = "2026-01-10", purchaseCost = 125.50m, warrantyExpiry = "2027-01-10", condition = "GOOD", category = "FIXTURE", quantity = 1 }));
        Assert.Equal("TAP-001", asset.GetProperty("serialReference").GetString()); Assert.Equal(125.50m, asset.GetProperty("purchaseCost").GetDecimal());
        var assetUpdate = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/assets/{asset.GetProperty("id").GetGuid()}", new { status = "DAMAGED", quantity = 1, location = "Kitchen", metadataJson = "{}", photosJson = "[]", rowVersion = asset.GetProperty("rowVersion").GetInt64() }));
        Assert.Equal("DAMAGED", assetUpdate.GetProperty("status").GetString());
        using (var staleAsset = await client.PatchAsJsonAsync($"/api/property-manager/professional/assets/{asset.GetProperty("id").GetGuid()}", new { status = "RETIRED", quantity = 1, location = "Kitchen", metadataJson = "{}", photosJson = "[]", rowVersion = asset.GetProperty("rowVersion").GetInt64() })) Assert.Equal(HttpStatusCode.Conflict, staleAsset.StatusCode);
        var incident = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/incidents", new { propertyId = portfolio.Property, incidentType = "DAMAGE", severity = "HIGH", occurredAt = DateTimeOffset.UtcNow, description = "Water leak" }));
        var incompleteInspection = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/inspections", new { propertyId = portfolio.Property, inspectionType = "SAFETY", scheduledAt = DateTimeOffset.UtcNow.AddDays(2), checklistJson = "[{\"id\":\"fire\",\"required\":true,\"completed\":false}]" }));
        var incompleteSignOff = await client.PatchAsJsonAsync($"/api/property-manager/professional/inspections/{incompleteInspection.GetProperty("id").GetGuid()}", new { status = "SIGNED_OFF", evidenceJson = "[]", findingsJson = "[]", rowVersion = incompleteInspection.GetProperty("rowVersion").GetInt64() });
        Assert.Equal(HttpStatusCode.BadRequest, incompleteSignOff.StatusCode);
        var inspection = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/inspections", new { propertyId = portfolio.Property, inspectionType = "MOVE_OUT", scheduledAt = DateTimeOffset.UtcNow.AddDays(1), checklistJson = "[]" }));
        await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/incidents/{incident.GetProperty("id").GetGuid()}", new { status = "RESOLVED", actionTaken = "Valve closed", followUp = "Replace tap", rowVersion = incident.GetProperty("rowVersion").GetInt64() }));
        _ = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/inspections/{inspection.GetProperty("id").GetGuid()}", new { status = "SIGNED_OFF", evidenceJson = "[]", findingsJson = "[{\"severity\":\"HIGH\",\"finding\":\"Tap leak\"}]", rowVersion = inspection.GetProperty("rowVersion").GetInt64() }));
        var correctiveWorkOrder = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/inspections/{inspection.GetProperty("id").GetGuid()}/work-order", new { scope = "Replace leaking tap" }));
        Assert.Equal(portfolio.Property, correctiveWorkOrder.GetProperty("propertyId").GetGuid());
        var repeatedCorrectiveWorkOrder = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/inspections/{inspection.GetProperty("id").GetGuid()}/work-order", new { scope = "Replace leaking tap" }));
        Assert.Equal(correctiveWorkOrder.GetProperty("id").GetGuid(), repeatedCorrectiveWorkOrder.GetProperty("id").GetGuid());
        var refreshedInspection = (await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/inspections"))!.Single(item => item.GetProperty("id").GetGuid() == inspection.GetProperty("id").GetGuid());
        Assert.Equal(correctiveWorkOrder.GetProperty("id").GetGuid(), refreshedInspection.GetProperty("correctiveWorkOrderId").GetGuid());
        Assert.NotEmpty((await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/assets"))!);
        Assert.NotEmpty((await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/incidents"))!);
        Assert.NotEmpty((await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/inspections"))!);
        var dashboard = await client.GetFromJsonAsync<JsonElement>("/api/property-manager/professional/dashboard");
        Assert.True(dashboard.GetProperty("openMaintenance").GetInt32() >= 1);
        Assert.True(dashboard.GetProperty("openWorkOrders").GetInt32() >= 1);
        Assert.True(dashboard.GetProperty("openIncidents").GetInt32() >= 0);
        Assert.True(dashboard.GetProperty("upcomingInspections").GetInt32() >= 0);
        var timeline = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/timeline");
        Assert.NotNull(timeline);
        Assert.Contains(timeline!, item => item.GetProperty("action").GetString() == "MaintenanceCreated");
    }

    [Fact]
    public async Task ReadinessCannotBeMarkedReadyUntilRequiredItemsAreComplete()
    {
        using var client = factory.CreateClient(); var portfolio = await CreatePortfolio(client);
        var created = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/cleaning", new { propertyId = portfolio.Property, dueAt = DateTimeOffset.UtcNow.AddDays(1), checklistJson = "[{\"id\":\"bathroom\",\"label\":\"Bathroom\",\"required\":true,\"completed\":false}]" }));
        var id = created.GetProperty("id").GetGuid();
        var rejected = await client.PatchAsJsonAsync($"/api/property-manager/professional/cleaning/{id}", new { status = "READY", checklistJson = created.GetProperty("checklistJson").GetString(), photosJson = "[]", issues = "", rowVersion = created.GetProperty("rowVersion").GetInt64() });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        var ready = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/cleaning/{id}", new { status = "READY", checklistJson = "[{\"id\":\"bathroom\",\"label\":\"Bathroom\",\"required\":true,\"completed\":true}]", photosJson = "[]", issues = "", rowVersion = created.GetProperty("rowVersion").GetInt64() }));
        Assert.Equal("READY", ready.GetProperty("status").GetString());
    }

    [Fact]
    public async Task MaintenanceAndCorrectiveWorkFinancialsUseReceiptsApprovalsReversalsAndRetest()
    {
        using var client = factory.CreateClient(); var portfolio = await CreatePortfolio(client);
        var maintenance = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/maintenance", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, title = "Repair balcony door", description = "Latch failed" }));
        var maintenanceId = maintenance.GetProperty("id").GetGuid();
        var receipt = await Ok(client.PostAsJsonAsync("/api/property-manager/maintenance/attachments", new { maintenanceId, fileName = "vendor-receipt.pdf", contentType = "application/pdf", contentBase64 = "JVBERi0xLjQK" }));
        var costKey = $"maintenance-cost-{maintenanceId:N}";
        var cost = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}/cost-lines", new { lineType = "LABOR", responsibility = "OWNER", description = "Door technician", amount = 120m, currency = "JMD", receiptAttachmentId = receipt.GetProperty("id").GetGuid(), idempotencyKey = costKey }));
        var repeatedCost = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}/cost-lines", new { lineType = "LABOR", responsibility = "OWNER", description = "Door technician", amount = 120m, currency = "JMD", receiptAttachmentId = receipt.GetProperty("id").GetGuid(), idempotencyKey = costKey }));
        Assert.Equal(cost.GetProperty("id").GetGuid(), repeatedCost.GetProperty("id").GetGuid()); Assert.Equal("vendor-receipt.pdf", cost.GetProperty("receiptFileName").GetString());
        maintenance = (await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/maintenance"))!.Single(x => x.GetProperty("id").GetGuid() == maintenanceId);
        var triaged = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status = "TRIAGED", rowVersion = maintenance.GetProperty("rowVersion").GetInt64(), details = "Triage complete" }));
        var quoting = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status = "OWNER_APPROVAL", rowVersion = triaged.GetProperty("rowVersion").GetInt64(), details = "Owner approval required" }));
        var approval = await CreateAndApprove(client, portfolio, "MAINTENANCE", maintenanceId, 150m, "Approve balcony repair");
        var assigned = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status = "ASSIGNED", rowVersion = quoting.GetProperty("rowVersion").GetInt64(), ownerApprovalId = approval }));
        var scheduled = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status = "SCHEDULED", rowVersion = assigned.GetProperty("rowVersion").GetInt64(), ownerApprovalId = approval }));
        var started = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status = "IN_PROGRESS", rowVersion = scheduled.GetProperty("rowVersion").GetInt64(), ownerApprovalId = approval }));
        var completed = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status = "COMPLETED", rowVersion = started.GetProperty("rowVersion").GetInt64(), ownerApprovalId = approval }));
        Assert.Equal("POSTED", completed.GetProperty("financialStatus").GetString());
        var correctionKey = $"maintenance-correction-{maintenanceId:N}";
        var correction = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}/financial-correction", new { expenseAmount = 110m, ownerCharge = 100m, managerFee = 0m, reason = "Vendor granted a credit", idempotencyKey = correctionKey, rowVersion = completed.GetProperty("rowVersion").GetInt64(), ownerApprovalId = approval }));
        Assert.False(correction.GetProperty("replayed").GetBoolean()); Assert.NotEqual(correction.GetProperty("reversalJournalId").GetGuid(), correction.GetProperty("replacementJournalId").GetGuid()); Assert.Equal("ADJUSTED", correction.GetProperty("maintenance").GetProperty("financialStatus").GetString());
        var repeatedCorrection = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}/financial-correction", new { expenseAmount = 110m, ownerCharge = 100m, managerFee = 0m, reason = "Vendor granted a credit", idempotencyKey = correctionKey, rowVersion = completed.GetProperty("rowVersion").GetInt64(), ownerApprovalId = approval }));
        Assert.True(repeatedCorrection.GetProperty("replayed").GetBoolean());
        var reopened = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status = "IN_PROGRESS", rowVersion = correction.GetProperty("maintenance").GetProperty("rowVersion").GetInt64(), ownerApprovalId = approval, details = "Repair failed quality review" }));
        Assert.Equal("IN_PROGRESS", reopened.GetProperty("status").GetString());

        var template = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/checklist-templates", new { name = "Safety", workflowType = "INSPECTION", itemsJson = "[{\"id\":\"door\",\"label\":\"Door safe\",\"required\":true,\"completed\":false}]" }));
        await Ok(client.PutAsJsonAsync($"/api/property-manager/professional/properties/{portfolio.Property}/checklist-template", new { templateId = template.GetProperty("id").GetGuid() }));
        var inspection = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/inspections", new { propertyId = portfolio.Property, inspectionType = "SAFETY", scheduledAt = DateTimeOffset.UtcNow.AddDays(1) }));
        Assert.Equal(template.GetProperty("id").GetGuid(), inspection.GetProperty("templateId").GetGuid());
        var failed = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/inspections/{inspection.GetProperty("id").GetGuid()}", new { status = "FAILED", evidenceJson = "[]", findingsJson = "[{\"checklistItemId\":\"door\",\"finding\":\"Latch remains unsafe\",\"severity\":\"HIGH\",\"correctiveRequired\":true}]", checklistJson = "[{\"id\":\"door\",\"label\":\"Door safe\",\"required\":true,\"completed\":true}]", rowVersion = inspection.GetProperty("rowVersion").GetInt64() }));
        Assert.Equal("FAILED", failed.GetProperty("status").GetString());
        var actions = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/corrective-actions?inspectionId={inspection.GetProperty("id").GetGuid()}"); Assert.Single(actions!); var action = actions![0];
        var work = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/inspections/{inspection.GetProperty("id").GetGuid()}/work-order", new { scope = "Replace unsafe latch" })); var workId = work.GetProperty("id").GetGuid();
        var vendor = await Ok(client.PostAsJsonAsync("/api/property-manager/vendors", new { name = "Latch Specialists", category = "CARPENTRY", contact = "vendor@example.test", notes = "Approved repair vendor" }));
        var workReceipt = await Ok(client.PostAsJsonAsync("/api/property-manager/maintenance/attachments", new { maintenanceId = workId, fileName = "work-order-receipt.pdf", contentType = "application/pdf", contentBase64 = "JVBERi0xLjQK" }));
        var workQuote = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/work-orders/{workId}/quotes", new { vendorId = vendor.GetProperty("id").GetGuid(), amount = 80m, currency = "JMD", scope = "Supply and install replacement latch", idempotencyKey = $"work-quote-{workId:N}" }));
        Assert.Equal("QUOTING", (await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/work-orders"))!.Single(x => x.GetProperty("id").GetGuid() == workId).GetProperty("status").GetString());
        var workCost = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/work-orders/{workId}/cost-lines", new { lineType = "MATERIAL", responsibility = "OWNER", description = "Replacement latch", amount = 80m, currency = "JMD", receiptAttachmentId = workReceipt.GetProperty("id").GetGuid(), idempotencyKey = $"work-cost-{workId:N}" })); Assert.Equal(80m, workCost.GetProperty("amount").GetDecimal()); Assert.Equal("work-order-receipt.pdf", workCost.GetProperty("receiptFileName").GetString());
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/maintenance/{workId}/attachments"))!);
        var workApproval = await CreateAndApprove(client, portfolio, "WORK_ORDER", workId, 100m, "Approve corrective work");
        var currentWork = (await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/work-orders"))!.Single(x => x.GetProperty("id").GetGuid() == workId);
        currentWork = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/work-orders/{workId}", new { status = "OWNER_APPROVAL", selectedQuoteId = workQuote.GetProperty("id").GetGuid(), approvedAmount = 80m, ownerApprovalId = workApproval, reason = "Selected approved bid", idempotencyKey = $"work-{workId:N}-OWNER_APPROVAL", rowVersion = currentWork.GetProperty("rowVersion").GetInt64() }));
        foreach (var status in new[] { "APPROVED", "ASSIGNED", "SCHEDULED", "IN_PROGRESS", "COMPLETED" })
        {
            currentWork = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/work-orders/{workId}", new { status, approvedAmount = 80m, ownerApprovalId = workApproval, reason = $"Advance to {status}", idempotencyKey = $"work-{workId:N}-{status}", rowVersion = currentWork.GetProperty("rowVersion").GetInt64() }));
        }
        Assert.Equal("POSTED", currentWork.GetProperty("postingStatus").GetString());
        var workCorrectionKey = $"work-correction-{workId:N}";
        var workCorrection = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/work-orders/{workId}/financial-correction", new { laborAmount = 70m, materialAmount = 0m, taxAmount = 0m, otherAmount = 0m, ownerResponsibility = 60m, managerResponsibility = 10m, vendorResponsibility = 0m, reason = "Negotiated warranty contribution", idempotencyKey = workCorrectionKey, rowVersion = currentWork.GetProperty("rowVersion").GetInt64(), ownerApprovalId = workApproval }));
        Assert.Equal("ADJUSTED", workCorrection.GetProperty("workOrder").GetProperty("postingStatus").GetString());
        var actionAfterWork = (await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/corrective-actions?inspectionId={inspection.GetProperty("id").GetGuid()}"))!.Single();
        var readyForRetest = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/corrective-actions/{actionAfterWork.GetProperty("id").GetGuid()}", new { status = "RETEST_REQUIRED", resolutionNotes = "Corrective work complete", rowVersion = actionAfterWork.GetProperty("rowVersion").GetInt64(), idempotencyKey = $"action-retest-{actionAfterWork.GetProperty("id").GetGuid():N}" }));
        var reinspection = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/corrective-actions/{readyForRetest.GetProperty("id").GetGuid()}/reinspection", new { scheduledAt = DateTimeOffset.UtcNow.AddDays(2), idempotencyKey = $"reinspect-{readyForRetest.GetProperty("id").GetGuid():N}" }));
        await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/inspections/{reinspection.GetProperty("id").GetGuid()}", new { status = "SIGNED_OFF", evidenceJson = "[]", findingsJson = "[]", checklistJson = "[{\"id\":\"door\",\"label\":\"Door safe\",\"required\":true,\"completed\":true}]", rowVersion = reinspection.GetProperty("rowVersion").GetInt64() }));
        var resolved = (await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/corrective-actions?inspectionId={inspection.GetProperty("id").GetGuid()}"))!.Single(); Assert.Equal("RESOLVED", resolved.GetProperty("status").GetString());
        var cleaningTemplate = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/checklist-templates", new { name = "Turnover readiness", workflowType = "CLEANING", itemsJson = "[{\"id\":\"ready\",\"label\":\"Property is ready\",\"required\":true,\"completed\":true}]" }));
        await Ok(client.PutAsJsonAsync($"/api/property-manager/professional/properties/{portfolio.Property}/checklist-template", new { templateId = cleaningTemplate.GetProperty("id").GetGuid() }));
        var readiness = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/cleaning", new { propertyId = portfolio.Property, dueAt = DateTimeOffset.UtcNow.AddDays(3) }));
        Assert.Equal("Turnover readiness", readiness.GetProperty("templateName").GetString()); Assert.Equal(1, readiness.GetProperty("templateVersion").GetInt32());
        var ready = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/cleaning/{readiness.GetProperty("id").GetGuid()}", new { status = "READY", checklistJson = readiness.GetProperty("checklistJson").GetString(), photosJson = "[]", issues = "", rowVersion = readiness.GetProperty("rowVersion").GetInt64() })); Assert.Equal("READY", ready.GetProperty("status").GetString());
    }

    private async Task<Guid> CreateAndApprove(HttpClient managerClient, (Guid Manager, Guid Owner, Guid Property, string OwnerEmail, string Password) portfolio, string sourceType, Guid sourceId, decimal amount, string description)
    {
        var approval = await Ok(managerClient.PostAsJsonAsync("/api/property-manager/p0/approvals", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, approvalType = sourceType, description, amount, currency = "JMD", sourceType, sourceId, expiresAt = DateTimeOffset.UtcNow.AddDays(7), idempotencyKey = $"approval-{sourceType}-{sourceId:N}" }));
        using var owner = factory.CreateClient(); var login = await owner.PostAsJsonAsync("/api/auth/login", new { email = portfolio.OwnerEmail, password = portfolio.Password }); Assert.True(login.IsSuccessStatusCode, await login.Content.ReadAsStringAsync()); owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var decided = await Ok(owner.PostAsJsonAsync($"/api/property-manager/p0/approvals/{approval.GetProperty("id").GetGuid()}/decision", new { status = "APPROVED", reason = description, rowVersion = approval.GetProperty("rowVersion").GetInt64(), idempotencyKey = $"decision-{approval.GetProperty("id").GetGuid():N}" })); return decided.GetProperty("id").GetGuid();
    }

    private async Task<(Guid Manager, Guid Owner, Guid Property, string OwnerEmail, string Password)> CreatePortfolio(HttpClient client)
    {
        var email = $"professional-{Guid.NewGuid():N}@nestystay.local";
        var registered = await Ok(client.PostAsJsonAsync("/api/auth/register", new { email, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Professional owner", phone = "+15550104003", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" }));
        var owner = registered.GetProperty("userId").GetGuid(); var manager = Guid.NewGuid(); SignIn(client, manager, UserRole.PropertyManager);
        await Ok(client.PostAsJsonAsync("/api/property-manager/owners", new { email, displayName = "Professional owner" }));
        var property = await Ok(client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = owner, title = "Professional unit", unitNumber = "P-1", address = "Kingston" }));
        return (manager, owner, property.GetProperty("id").GetGuid(), email, "NestyStay1");
    }
    private static void SignIn(HttpClient client, Guid userId, UserRole role) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, role));
    private static async Task<JsonElement> Ok(Task<HttpResponseMessage> action) { using var response = await action; var body = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}"); return JsonDocument.Parse(body).RootElement.Clone(); }
}
