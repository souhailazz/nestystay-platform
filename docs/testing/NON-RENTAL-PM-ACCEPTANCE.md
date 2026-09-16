# Non-Rental Property-Manager Acceptance

The Property Manager product is intentionally modeled around manager-owned
properties, owners, finance, operations, and governance. A property does not
need to be linked to a short-term rental listing and does not need an Airbnb
booking to use the PM workflows.

## Locally implemented areas

- owner invitations, owner verification requirements, profiles, agreements,
  fee rules and ownership scoping;
- invoices, invoice lines, payments, refunds, statements, ledger posting and
  financial corrections;
- utilities, schedules, disputes and charges;
- maintenance cases, quotes, attachments, work orders, inspections, cleaning
  readiness and corrective actions;
- vendors, assets, incidents, documents, versions, downloads and exports;
- community notices, comments, acknowledgements, proposals and voting;
- staff/team records, calendar events and owner blocks;
- gate messages, QR issuance/validation/revocation and scan history;
- reporting and the PM subscription lifecycle.

## Acceptance evidence

- `PropertyManagerEndpointTests` covers owners, invoices, utilities, owner
  portal, documents, subscription changes, notices and governance.
- `PropertyManagerFoundationRegressionTests` covers authorization, persisted
  preferences, utility ownership, agreements, invoice adjustments, payment
  idempotency, refund boundaries, approvals, notices, and the explicit
  no-rental/no-booking dashboard case.
- `PropertyManagerP0EndpointTests` and
  `PropertyManagerProfessionalWorkflowTests` cover accounting, approvals,
  maintenance, work orders, utilities, professional records and lifecycle
  idempotency.

## Boundary

This acceptance is local application behavior. It does not certify external
payment, object storage, email, monitoring, backup, or production TLS until the
client's staging environment is exercised with its configured providers.
