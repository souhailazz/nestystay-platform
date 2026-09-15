import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ChevronDown,
  FileDown,
  RefreshCw,
  ShieldCheck,
  Wallet,
  X,
} from "lucide-react";
import { AppLink } from "../components/AppLink";
import { Button, buttonClassName } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import { Field, Input, Select, Textarea } from "../components/ui/Input";
import { LoadingState } from "../components/ui/LoadingState";
import { PageHeader } from "../components/ui/PageHeader";
import { StatusChip } from "../components/ui/StatusChip";
import type { AuthController } from "../hooks/useAuth";
import {
  api,
  formatMoney,
  type P0Account,
  type P0Agreement,
  type P0Approval,
  type P0FeeCalculation,
  type P0FeeRule,
  type P0Journal,
  type P0OwnerLifecycleEvent,
  type P0PayoutBatch,
  type P0Portfolio,
  type P0Profitability,
  type P0StaffEvent,
  type P0StaffMembership,
  type P0Statement,
  type PropertyAssignmentHistoryDto,
  type PropertyManagerDashboard,
} from "../lib/api";

function ErrorNotice({ error }: { error: unknown }) {
  return error ? (
    <div
      className="rounded-field bg-coral-tint px-4 py-3 text-sm text-coral-text"
      role="alert"
    >
      {error instanceof Error
        ? error.message
        : "The request could not be completed."}
    </div>
  ) : null;
}

function today() {
  return new Date().toISOString().slice(0, 10);
}
function monthAgo() {
  const date = new Date();
  date.setMonth(date.getMonth() - 1);
  return date.toISOString().slice(0, 10);
}
function randomId() {
  return typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}
function parseJson(value: string, fallback: unknown) {
  try {
    return JSON.parse(value);
  } catch {
    return fallback;
  }
}

export function PropertyManagerP0Page({ auth }: { auth: AuthController }) {
  const token = auth.session?.accessToken ?? "";
  const sessionUserId = auth.session?.userId ?? "";
  const [dashboard, setDashboard] = useState<PropertyManagerDashboard | null>(
    null,
  );
  const [portfolio, setPortfolio] = useState<P0Portfolio | null>(null);
  const [agreements, setAgreements] = useState<P0Agreement[]>([]);
  const [feeRules, setFeeRules] = useState<P0FeeRule[]>([]);
  const [accounts, setAccounts] = useState<P0Account[]>([]);
  const [journals, setJournals] = useState<P0Journal[]>([]);
  const [approvals, setApprovals] = useState<P0Approval[]>([]);
  const [payouts, setPayouts] = useState<P0PayoutBatch[]>([]);
  const [staff, setStaff] = useState<P0StaffMembership[]>([]);
  const [profitability, setProfitability] = useState<P0Profitability | null>(
    null,
  );
  const [statement, setStatement] = useState<P0Statement | null>(null);
  const [ownerLifecycle, setOwnerLifecycle] = useState<P0OwnerLifecycleEvent[]>(
    [],
  );
  const [assignmentHistory, setAssignmentHistory] = useState<
    PropertyAssignmentHistoryDto[]
  >([]);
  const [staffHistory, setStaffHistory] = useState<
    Record<string, P0StaffEvent[]>
  >({});
  const [expandedStaffId, setExpandedStaffId] = useState<string | null>(null);
  const [expandedJournalId, setExpandedJournalId] = useState<string | null>(
    null,
  );
  const [feeCalculation, setFeeCalculation] = useState<P0FeeCalculation | null>(
    null,
  );
  const [feeBaseAmount, setFeeBaseAmount] = useState("100");
  const [editingAgreementId, setEditingAgreementId] = useState<string | null>(
    null,
  );
  const [busy, setBusy] = useState(false);
  const [ownerId, setOwnerId] = useState("");
  const [propertyId, setPropertyId] = useState("");
  const [currency, setCurrency] = useState("JMD");
  const [search, setSearch] = useState("");
  const [assignmentReason, setAssignmentReason] = useState(
    "Owner assignment confirmed",
  );
  const [reconcileReason, setReconcileReason] = useState(
    "Matched to bank evidence",
  );
  const [profile, setProfile] = useState({
    legalName: "",
    contactEmail: "",
    contactPhone: "",
    billingAddress: "",
    billingMetadataJson: "{}",
    paymentProviderCustomerReference: "",
    timeZone: "America/Jamaica",
    notes: "",
  });
  const [agreement, setAgreement] = useState({
    from: today(),
    to: "",
    feeRuleJson: "[]",
    termsJson: "{}",
    maintenanceLimit: "500",
    expenseLimit: "500",
    documentId: "",
  });
  const [fee, setFee] = useState({
    category: "MANAGEMENT",
    type: "PERCENTAGE",
    basis: "COLLECTED_RENT",
    percentage: "10",
    fixed: "0",
    minimum: "0",
    cleaningMarkup: "0",
    maintenanceMarkup: "0",
    from: today(),
    to: "",
  });
  const [journal, setJournal] = useState({
    amount: "",
    memo: "Rent receipt",
    source: "RENT_RECEIPT",
    reconciled: true,
  });
  const [period, setPeriod] = useState({ from: monthAgo(), to: today() });
  const [approval, setApproval] = useState({
    description: "",
    type: "EXPENSE",
    amount: "",
    evidence: "",
    sourceType: "",
    sourceId: "",
    expiresAt: "",
  });
  const [staffForm, setStaffForm] = useState({
    staffUserId: "",
    role: "OPERATIONS",
    finance: false,
    payouts: false,
    limit: "0",
  });
  const [error, setError] = useState<unknown>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(async () => {
    // Cookie-backed sessions intentionally keep the bearer token empty.  The
    // API client sends credentials on every request, so the session identity
    // is the correct readiness signal here.
    if (!sessionUserId) return;
    setError(null);
    const [
      nextDashboard,
      nextPortfolio,
      nextAgreements,
      nextFeeRules,
      nextAccounts,
      nextJournals,
      nextApprovals,
      nextPayouts,
      nextStaff,
      nextProfitability,
    ] = await Promise.all([
      api.getPropertyManagerDashboard(token),
      api.getP0Portfolio(token, {
        search: search || undefined,
        page: 1,
        pageSize: 100,
      }),
      api.getP0Agreements(token, {
        ownerUserId: ownerId || undefined,
        page: 1,
        pageSize: 100,
      }),
      api.getP0FeeRules(token, {
        ownerUserId: ownerId || undefined,
        propertyId: propertyId || undefined,
        currency,
      }),
      api.getP0Accounts(token, currency),
      api.getP0Journals(token, {
        ownerUserId: ownerId || undefined,
        propertyId: propertyId || undefined,
        currency,
        page: 1,
        pageSize: 100,
      }),
      api.getP0Approvals(token, {
        ownerUserId: ownerId || undefined,
        propertyId: propertyId || undefined,
        page: 1,
        pageSize: 100,
      }),
      api.getP0Payouts(token, {
        ownerUserId: ownerId || undefined,
        currency,
        page: 1,
        pageSize: 100,
      }),
      api.getP0Staff(token),
      api.getP0Profitability(token, {
        currency,
        from: period.from,
        to: period.to,
        ownerUserId: ownerId || undefined,
        propertyId: propertyId || undefined,
      }),
    ]);
    setDashboard(nextDashboard);
    setPortfolio(nextPortfolio);
    setAgreements(nextAgreements);
    setFeeRules(nextFeeRules);
    setAccounts(nextAccounts);
    setJournals(nextJournals);
    setApprovals(nextApprovals);
    setPayouts(nextPayouts);
    setStaff(nextStaff);
    setProfitability(nextProfitability);
    const firstOwner =
      ownerId ||
      nextDashboard.owners[0]?.ownerUserId ||
      nextPortfolio.items[0]?.ownerUserId ||
      "";
    if (firstOwner && !ownerId) setOwnerId(firstOwner);
    const firstProperty =
      propertyId ||
      nextDashboard.properties.find((item) => item.ownerUserId === firstOwner)
        ?.id ||
      nextPortfolio.items.find((item) => item.ownerUserId === firstOwner)
        ?.propertyId ||
      "";
    if (firstProperty && !propertyId) setPropertyId(firstProperty);
  }, [token, sessionUserId, ownerId, propertyId, currency, period.from, period.to, search]);

  useEffect(() => {
    void load().catch(setError);
  }, [load]);
  useEffect(() => {
    if (!sessionUserId || !ownerId) {
      setOwnerLifecycle([]);
      return;
    }
    void Promise.all([
      api.getP0OwnerProfile(token, ownerId),
      api.getP0OwnerLifecycle(token, ownerId),
    ])
      .then(([value, events]) => {
        if (value) {
          setProfile({
            legalName: value.legalName,
            contactEmail: value.contactEmail,
            contactPhone: value.contactPhone,
            billingAddress: value.billingAddress,
            billingMetadataJson: value.billingMetadataJson || "{}",
            paymentProviderCustomerReference:
              value.paymentProviderCustomerReference || "",
            timeZone: value.timeZone,
            notes: value.notes,
          });
          setCurrency(value.preferredCurrency);
        }
        setOwnerLifecycle(events);
      })
      .catch(() => undefined);
  }, [token, sessionUserId, ownerId]);
  useEffect(() => {
    if (!sessionUserId || !propertyId) {
      setAssignmentHistory([]);
      return;
    }
    void api
      .getP0AssignmentHistory(token, propertyId)
      .then(setAssignmentHistory)
      .catch(() => undefined);
  }, [token, sessionUserId, propertyId]);

  const properties = dashboard?.properties ?? [];
  const selectedProperties = useMemo(
    () => properties.filter((item) => !ownerId || item.ownerUserId === ownerId),
    [properties, ownerId],
  );
  const selectedDocumentOptions = useMemo(
    () =>
      (dashboard?.documents ?? [])
        .filter(
          (document) =>
            !document.ownerUserId || document.ownerUserId === ownerId,
        )
        .filter(
          (document) =>
            !document.propertyId || document.propertyId === propertyId,
        ),
    [dashboard?.documents, ownerId, propertyId],
  );

  async function run(action: () => Promise<unknown>, message: string) {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await action();
      setNotice(message);
      await load();
    } catch (cause) {
      setError(cause);
    } finally {
      setBusy(false);
    }
  }
  async function openApprovalEvidence(documentId: string) {
    setError(null);
    try {
      const result = await api.getPropertyManagerDocumentDownload(token, documentId);
      window.open(result.url, "_blank", "noopener,noreferrer");
    } catch (cause) {
      setError(cause);
    }
  }
  async function saveProfile() {
    if (!ownerId) return;
    await run(
      () =>
        api.saveP0OwnerProfile(token, ownerId, {
          ...profile,
          preferredCurrency: currency,
          operationalMetadataJson: "{}",
        }),
      "Owner profile saved",
    );
  }
  function loadAgreementIntoForm(item: P0Agreement) {
    setEditingAgreementId(item.id);
    setAgreement({
      from: item.effectiveFrom,
      to: item.effectiveTo ?? "",
      feeRuleJson: item.feeRuleJson,
      termsJson: item.termsJson,
      maintenanceLimit: String(item.maintenanceApprovalLimit),
      expenseLimit: String(item.expenseApprovalLimit),
      documentId: item.documentId ?? "",
    });
    setCurrency(item.currency);
  }
  async function saveAgreement() {
    if (!ownerId) return;
    const item = editingAgreementId
      ? agreements.find((row) => row.id === editingAgreementId)
      : undefined;
    const body = {
      effectiveFrom: agreement.from,
      effectiveTo: agreement.to || undefined,
      currency,
      termsJson: agreement.termsJson,
      feeRuleJson: agreement.feeRuleJson,
      maintenanceApprovalLimit: Number(agreement.maintenanceLimit),
      expenseApprovalLimit: Number(agreement.expenseLimit),
      documentId: agreement.documentId || undefined,
    };
    await run(
      () =>
        item
          ? api.updateP0AgreementDraft(token, item.id, {
              ...body,
              rowVersion: item.rowVersion,
            })
          : api.createP0Agreement(token, {
              ownerUserId: ownerId,
              propertyId: propertyId || undefined,
              ...body,
            }),
      item ? "Draft agreement updated" : "Draft agreement created",
    );
    setEditingAgreementId(null);
  }
  async function calculateFee() {
    if (!ownerId) return;
    await run(
      async () =>
        setFeeCalculation(
          await api.calculateP0Fee(token, {
            ownerUserId: ownerId,
            propertyId: propertyId || undefined,
            category: fee.category,
            currency,
            baseAmount: Number(feeBaseAmount),
            on: today(),
          }),
        ),
      "Fee calculation refreshed",
    );
  }
  async function postJournal() {
    if (!ownerId || Number(journal.amount) <= 0) return;
    const amount = Number(journal.amount);
    const expense = journal.source === "OWNER_EXPENSE" || journal.source === "THIRD_PARTY_EXPENSE";
    const ownerBilling = journal.source === "OWNER_BILLING";
    const ownerBillingReceipt = journal.source === "OWNER_BILLING_RECEIPT";
    const thirdParty = journal.source === "THIRD_PARTY_EXPENSE";
    const ownerAccount = `${expense ? "OWNER_EXPENSE" : "OWNER_INCOME"}:${ownerId}:${propertyId || "ALL"}:${currency}`;
    const ownerReceivable = `OWNER_RECEIVABLE:${ownerId}:${currency}`;
    const debitAccount = ownerBilling
      ? ownerReceivable
      : expense
        ? ownerAccount
        : `CASH_CLEARING:${currency}`;
    const creditAccount = ownerBilling
      ? `PM_FEE_REVENUE:${currency}`
      : ownerBillingReceipt
        ? ownerReceivable
        : thirdParty
      ? `THIRD_PARTY_PAYABLE:${ownerId}:${propertyId || "ALL"}:${currency}`
      : `CASH_CLEARING:${currency}`;
    await run(
      () =>
        api.postP0Journal(token, {
          sourceType: journal.source,
          idempotencyKey: randomId(),
          currency,
          accountingDate: today(),
          memo: journal.memo,
          reconcile: journal.reconciled,
          reconciliationReference: journal.reconciled
            ? `UI-${Date.now()}`
            : undefined,
          lines: [
            {
              accountCode: debitAccount,
              debit: amount,
              credit: 0,
              ownerUserId: expense || ownerBilling ? ownerId : undefined,
              propertyId:
                expense || ownerBilling || ownerBillingReceipt
                  ? propertyId || undefined
                  : undefined,
              description: journal.memo,
            },
            {
              accountCode: expense || ownerBilling || ownerBillingReceipt ? creditAccount : ownerAccount,
              debit: 0,
              credit: amount,
              ownerUserId: ownerBilling ? undefined : ownerId,
              propertyId: thirdParty
                ? propertyId || undefined
                : expense
                  ? undefined
                  : propertyId || undefined,
              description: journal.memo,
            },
          ],
        }),
      "Balanced journal posted",
    );
  }
  async function reconcile(item: P0Journal) {
    await run(
      () =>
        api.reconcileP0Journal(token, {
          journalId: item.id,
          externalReference: `UI-${item.id.slice(0, 8)}`,
          amount: item.totalDebit,
          currency: item.currency,
          reason: reconcileReason,
        }),
      "Journal reconciled",
    );
  }
  async function toggleStaffHistory(id: string) {
    if (expandedStaffId === id) {
      setExpandedStaffId(null);
      return;
    }
    setExpandedStaffId(id);
    if (staffHistory[id]) return;
    try {
      const history = await api.getP0StaffHistory(token, id);
      setStaffHistory((current) => ({ ...current, [id]: history }));
    } catch (cause) {
      setError(cause);
    }
  }

  if (!auth.session)
    return (
      <div className="product-page">
        <PageHeader
          eyebrow="Property Manager P0"
          title="Sign in required"
          copy="Sign in with a manager account to access owner money and authority controls."
        />
      </div>
    );
  if (!dashboard || !portfolio)
    return (
      <div className="product-page">
        <PageHeader
          eyebrow="Property Manager P0"
          title="Loading money and authority workspace"
        />
        <LoadingState label="Loading manager-scoped records" />
        <ErrorNotice error={error} />
      </div>
    );

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="P0 · Money & authority"
        title="Run every owner relationship from one controlled workspace"
        copy="Owner records, agreements, fees, balanced journals, approvals, statements and payouts are persisted through the P0 API."
        actions={
          <Button
            disabled={busy}
            onClick={() => void run(load, "Workspace refreshed")}
          >
            <RefreshCw size={16} /> Refresh
          </Button>
        }
      />
      <nav aria-label="P0 sections" className="mb-6 flex flex-wrap gap-2">
        <AppLink className={buttonClassName("outline")} href="/pm/dashboard">
          Overview
        </AppLink>
        <AppLink className={buttonClassName("outline")} href="/pm/p0">
          P0 finance
        </AppLink>
        <AppLink className={buttonClassName("outline")} href="/pm/agreements">
          Agreements
        </AppLink>
        <AppLink className={buttonClassName("outline")} href="/pm/approvals">
          Approvals
        </AppLink>
        <AppLink className={buttonClassName("outline")} href="/pm/team">
          Team access
        </AppLink>
        <AppLink className={buttonClassName("outline")} href="/owner/dashboard">
          Owner portal
        </AppLink>
      </nav>
      <ErrorNotice error={error} />
      {notice && (
        <div
          className="mb-4 rounded-field bg-success-tint px-4 py-3 text-sm font-semibold text-success-text"
          role="status"
        >
          {notice}
        </div>
      )}
      <section className="mb-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <small className="text-sand-600">Managed properties</small>
          <strong className="mt-1 block font-display text-3xl">
            {portfolio.total}
          </strong>
        </Card>
        <Card>
          <small className="text-sand-600">Owners</small>
          <strong className="mt-1 block font-display text-3xl">
            {dashboard.totalOwners}
          </strong>
        </Card>
        <Card>
          <small className="text-sand-600">Owner net ({currency})</small>
          <strong className="mt-1 block font-display text-3xl">
            {formatMoney(profitability?.ownerNet ?? 0, currency)}
          </strong>
        </Card>
        <Card>
          <small className="text-sand-600">PM margin ({currency})</small>
          <strong className="mt-1 block font-display text-3xl">
            {formatMoney(profitability?.pmMargin ?? 0, currency)}
          </strong>
        </Card>
      </section>
      <Card className="mb-6">
        <div className="grid gap-3 md:grid-cols-[1fr_1fr_160px]">
          <Field label="Owner">
            <Select
              value={ownerId}
              onChange={(event) => {
                setOwnerId(event.target.value);
                setPropertyId("");
              }}
            >
              <option value="">Select owner</option>
              {dashboard.owners.map((owner) => (
                <option key={owner.ownerUserId} value={owner.ownerUserId}>
                  {owner.displayName} · {owner.email}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Property">
            <Select
              value={propertyId}
              onChange={(event) => setPropertyId(event.target.value)}
            >
              <option value="">All properties</option>
              {selectedProperties.map((property) => (
                <option key={property.id} value={property.id}>
                  {property.title} · {property.unitNumber}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Currency">
            <Select
              value={currency}
              onChange={(event) => setCurrency(event.target.value)}
            >
              <option>JMD</option>
              <option>USD</option>
            </Select>
          </Field>
        </div>
        <Field className="mt-3" label="Search portfolio">
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Owner, property, address"
          />
        </Field>
      </Card>
      <section className="mb-6 grid gap-5 lg:grid-cols-2">
        <Card>
          <h2 className="font-display text-2xl">Owner profile & lifecycle</h2>
          {!ownerId ? (
            <EmptyState
              title="Select an owner"
              copy="Owner contact, billing and operational metadata appear after selection."
            />
          ) : (
            <>
              <div className="mt-3 grid gap-3 sm:grid-cols-2">
                <Field label="Legal name">
                  <Input
                    value={profile.legalName}
                    onChange={(event) =>
                      setProfile({ ...profile, legalName: event.target.value })
                    }
                  />
                </Field>
                <Field label="Billing email">
                  <Input
                    type="email"
                    value={profile.contactEmail}
                    onChange={(event) =>
                      setProfile({
                        ...profile,
                        contactEmail: event.target.value,
                      })
                    }
                  />
                </Field>
                <Field label="Phone">
                  <Input
                    value={profile.contactPhone}
                    onChange={(event) =>
                      setProfile({
                        ...profile,
                        contactPhone: event.target.value,
                      })
                    }
                  />
                </Field>
                <Field label="Time zone">
                  <Input
                    value={profile.timeZone}
                    onChange={(event) =>
                      setProfile({ ...profile, timeZone: event.target.value })
                    }
                  />
                </Field>
              </div>
              <Field className="mt-3" label="Billing address">
                <Input
                  value={profile.billingAddress}
                  onChange={(event) =>
                    setProfile({
                      ...profile,
                      billingAddress: event.target.value,
                    })
                  }
                />
              </Field>
              <div className="mt-3 grid gap-3 sm:grid-cols-2">
                <Field
                  label="Billing metadata JSON"
                  hint="Non-sensitive invoice preferences only."
                >
                  <Input
                    value={profile.billingMetadataJson}
                    onChange={(event) =>
                      setProfile({
                        ...profile,
                        billingMetadataJson: event.target.value,
                      })
                    }
                  />
                </Field>
                <Field
                  label="Payment provider customer reference"
                  hint="Reference only; never store card or bank details here."
                >
                  <Input
                    value={profile.paymentProviderCustomerReference}
                    onChange={(event) =>
                      setProfile({
                        ...profile,
                        paymentProviderCustomerReference: event.target.value,
                      })
                    }
                  />
                </Field>
              </div>
              <Field className="mt-3" label="Operational notes">
                <Textarea
                  value={profile.notes}
                  onChange={(event) =>
                    setProfile({ ...profile, notes: event.target.value })
                  }
                />
              </Field>
              <div className="mt-3 flex flex-wrap gap-2">
                <Button
                  disabled={busy || !profile.legalName || !profile.contactEmail}
                  onClick={() => void saveProfile()}
                >
                  Save owner profile
                </Button>
                <Button
                  disabled={busy}
                  variant="outline"
                  onClick={() =>
                    void run(
                      () =>
                        api.changeP0OwnerStatus(token, ownerId, {
                          status: "SUSPENDED",
                          reason: "Temporarily suspended by manager",
                        }),
                      "Owner suspended",
                    )
                  }
                >
                  Suspend
                </Button>
                <Button
                  disabled={busy}
                  variant="outline"
                  onClick={() =>
                    void run(
                      () =>
                        api.changeP0OwnerStatus(token, ownerId, {
                          status: "ACTIVE",
                          reason: "Owner relationship reactivated",
                        }),
                      "Owner activated",
                    )
                  }
                >
                  Activate
                </Button>
              </div>
              <div className="mt-5 border-t border-sand-border pt-4">
                <h3 className="m-0 text-sm font-semibold">Lifecycle history</h3>
                {ownerLifecycle.length === 0 ? (
                  <p className="m-0 mt-2 text-xs text-sand-600">
                    No lifecycle events recorded yet.
                  </p>
                ) : (
                  <div className="mt-2 space-y-2">
                    {ownerLifecycle.slice(0, 8).map((event) => (
                      <div
                        className="rounded-field bg-shell p-2 text-xs"
                        key={event.id}
                      >
                        <strong>{event.eventType}</strong> ·{" "}
                        {event.fromStatus || "—"} → {event.toStatus} ·{" "}
                        {event.reason}
                        <span className="block text-sand-500">
                          {new Date(event.createdAt).toLocaleString()}
                        </span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}
        </Card>
        <Card>
          <h2 className="font-display text-2xl">Portfolio ownership</h2>
          <p className="mt-1 text-sm text-sand-600">
            Assignment changes are transactional and append an immutable history
            event.
          </p>
          <div className="mt-3 max-h-72 space-y-2 overflow-auto">
            {portfolio.items.map((row) => (
              <div
                className="rounded-field border border-sand-border p-3"
                key={row.propertyId}
              >
                <div className="flex items-center justify-between gap-2">
                  <strong>{row.propertyTitle}</strong>
                  <StatusChip value={row.ownerStatus} />
                </div>
                <p className="m-0 text-xs text-sand-600">
                  {row.ownerName} · {row.address}
                </p>
              </div>
            ))}
          </div>
          <Field className="mt-3" label="Assignment reason">
            <Input
              value={assignmentReason}
              onChange={(event) => setAssignmentReason(event.target.value)}
            />
          </Field>
          <Button
            className="mt-3"
            disabled={
              busy || !ownerId || !propertyId || !assignmentReason.trim()
            }
            onClick={() =>
              void run(
                () =>
                  api.assignP0Properties(token, {
                    propertyIds: [propertyId],
                    ownerUserId: ownerId,
                    reason: assignmentReason,
                  }),
                "Property assignment saved",
              )
            }
          >
            Assign selected property to owner
          </Button>
          <div className="mt-5 border-t border-sand-border pt-4">
            <h3 className="m-0 text-sm font-semibold">Assignment history</h3>
            {assignmentHistory.length === 0 ? (
              <p className="m-0 mt-2 text-xs text-sand-600">
                No changes recorded for the selected property.
              </p>
            ) : (
              <div className="mt-2 space-y-2">
                {assignmentHistory.slice(0, 8).map((event) => (
                  <div
                    className="rounded-field bg-shell p-2 text-xs"
                    key={event.id}
                  >
                    <strong>
                      {event.previousOwnerUserId
                        ? "Owner changed"
                        : "Initial assignment"}
                    </strong>{" "}
                    · {event.reason}
                    <span className="block text-sand-500">
                      {new Date(event.changedAt).toLocaleString()}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Card>
      </section>
      <section className="mb-6 grid gap-5 lg:grid-cols-2">
        <Card>
          <div className="flex items-start justify-between gap-3">
            <div>
              <h2 className="font-display text-2xl">Management agreements</h2>
              <p className="mt-1 text-sm text-sand-600">
                Draft, activate, renew and terminate versioned terms scoped to
                the selected owner/property.
              </p>
            </div>
            {editingAgreementId && (
              <Button
                variant="ghost"
                onClick={() => setEditingAgreementId(null)}
              >
                <X size={16} /> Cancel edit
              </Button>
            )}
          </div>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <Field label="Effective from">
              <Input
                type="date"
                value={agreement.from}
                onChange={(event) =>
                  setAgreement({ ...agreement, from: event.target.value })
                }
              />
            </Field>
            <Field label="Effective to">
              <Input
                type="date"
                value={agreement.to}
                onChange={(event) =>
                  setAgreement({ ...agreement, to: event.target.value })
                }
              />
            </Field>
            <Field label="Maintenance approval limit">
              <Input
                inputMode="decimal"
                value={agreement.maintenanceLimit}
                onChange={(event) =>
                  setAgreement({
                    ...agreement,
                    maintenanceLimit: event.target.value,
                  })
                }
              />
            </Field>
            <Field label="Expense approval limit">
              <Input
                inputMode="decimal"
                value={agreement.expenseLimit}
                onChange={(event) =>
                  setAgreement({
                    ...agreement,
                    expenseLimit: event.target.value,
                  })
                }
              />
            </Field>
          </div>
          <Field
            className="mt-3"
            label="Terms JSON"
            hint="Stored as an immutable version when activated."
          >
            <Textarea
              value={agreement.termsJson}
              onChange={(event) =>
                setAgreement({ ...agreement, termsJson: event.target.value })
              }
            />
          </Field>
          <Field className="mt-3" label="Fee rules JSON">
            <Textarea
              value={agreement.feeRuleJson}
              onChange={(event) =>
                setAgreement({ ...agreement, feeRuleJson: event.target.value })
              }
            />
          </Field>
          <Field className="mt-3" label="Associated document">
            <Select
              value={agreement.documentId}
              onChange={(event) =>
                setAgreement({ ...agreement, documentId: event.target.value })
              }
            >
              <option value="">No document</option>
              {selectedDocumentOptions.map((document) => (
                <option key={document.id} value={document.id}>
                  {document.title} · {document.fileName}
                </option>
              ))}
            </Select>
          </Field>
          <Button
            className="mt-3"
            disabled={
              busy ||
              !ownerId ||
              !agreement.from ||
              parseJson(agreement.termsJson, null) === null ||
              parseJson(agreement.feeRuleJson, null) === null
            }
            onClick={() => void saveAgreement()}
          >
            {editingAgreementId
              ? "Save draft changes"
              : "Create draft agreement"}
          </Button>
          <div className="mt-4 space-y-2">
            {agreements.length === 0 ? (
              <p className="text-sm text-sand-600">
                No agreements for this scope yet.
              </p>
            ) : (
              agreements.map((item) => (
                <div
                  className="rounded-field border border-sand-border p-3"
                  key={item.id}
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div>
                      <strong>
                        v{item.version} · {item.status}
                      </strong>
                      <p className="m-0 text-xs text-sand-600">
                        {item.effectiveFrom} →{" "}
                        {item.effectiveTo ?? "open-ended"} · {item.currency} ·
                        limits{" "}
                        {formatMoney(item.expenseApprovalLimit, item.currency)}
                      </p>
                      {item.supersedesAgreementId && (
                        <small className="text-sand-500">
                          Supersedes {item.supersedesAgreementId.slice(0, 8)}
                        </small>
                      )}
                    </div>
                    <StatusChip value={item.status} />
                  </div>
                  <div className="mt-2 flex flex-wrap gap-2">
                    {item.status === "DRAFT" && (
                      <>
                        <Button
                          variant="outline"
                          disabled={busy}
                          onClick={() => loadAgreementIntoForm(item)}
                        >
                          Edit draft
                        </Button>
                        <Button
                          variant="outline"
                          disabled={busy}
                          onClick={() =>
                            void run(
                              () => api.activateP0Agreement(token, item.id),
                              "Agreement activated",
                            )
                          }
                        >
                          Activate
                        </Button>
                      </>
                    )}
                    {item.status === "ACTIVE" && (
                      <>
                        <Button
                          variant="outline"
                          disabled={busy}
                          onClick={() =>
                            void run(
                              () =>
                                api.renewP0Agreement(token, item.id, {
                                  effectiveFrom:
                                    item.effectiveTo ??
                                    new Date(Date.now() + 86_400_000)
                                      .toISOString()
                                      .slice(0, 10),
                                  reason: "Renewal prepared by manager",
                                }),
                              "Renewal draft created",
                            )
                          }
                        >
                          Renew
                        </Button>
                        <Button
                          variant="destructive"
                          disabled={busy}
                          confirmMessage="Terminate this active agreement?"
                          onClick={() =>
                            void run(
                              () =>
                                api.terminateP0Agreement(
                                  token,
                                  item.id,
                                  "Manager terminated agreement",
                                ),
                              "Agreement terminated",
                            )
                          }
                        >
                          Terminate
                        </Button>
                      </>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </Card>
        <Card>
          <h2 className="font-display text-2xl">Configurable fee engine</h2>
          <p className="mt-1 text-sm text-sand-600">
            Percentage rules default to collected rent. Fixed, minimum, markup
            and combined rules are calculated server-side.
          </p>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <Field label="Category">
              <Input
                value={fee.category}
                onChange={(event) =>
                  setFee({ ...fee, category: event.target.value })
                }
              />
            </Field>
            <Field label="Rule type">
              <Select
                value={fee.type}
                onChange={(event) =>
                  setFee({ ...fee, type: event.target.value })
                }
              >
                <option>PERCENTAGE</option>
                <option>FIXED</option>
                <option>MINIMUM</option>
                <option>MARKUP</option>
                <option>COMBINED</option>
              </Select>
            </Field>
            <Field label="Basis">
              <Select
                value={fee.basis}
                onChange={(event) =>
                  setFee({ ...fee, basis: event.target.value })
                }
              >
                <option>COLLECTED_RENT</option>
                <option>INVOICED_RENT</option>
                <option>EXPENSE</option>
              </Select>
            </Field>
            <Field label="Effective from">
              <Input
                type="date"
                value={fee.from}
                onChange={(event) =>
                  setFee({ ...fee, from: event.target.value })
                }
              />
            </Field>
            <Field label="Effective to">
              <Input
                type="date"
                value={fee.to}
                onChange={(event) => setFee({ ...fee, to: event.target.value })}
              />
            </Field>
            <Field label="Percentage">
              <Input
                inputMode="decimal"
                value={fee.percentage}
                onChange={(event) =>
                  setFee({ ...fee, percentage: event.target.value })
                }
              />
            </Field>
            <Field label="Fixed amount">
              <Input
                inputMode="decimal"
                value={fee.fixed}
                onChange={(event) =>
                  setFee({ ...fee, fixed: event.target.value })
                }
              />
            </Field>
            <Field label="Minimum amount">
              <Input
                inputMode="decimal"
                value={fee.minimum}
                onChange={(event) =>
                  setFee({ ...fee, minimum: event.target.value })
                }
              />
            </Field>
          </div>
          <div className="mt-3 flex flex-wrap items-end gap-2">
            <Button
              disabled={busy || !ownerId}
              onClick={() =>
                void run(
                  () =>
                    api.createP0FeeRule(token, {
                      ownerUserId: ownerId,
                      propertyId: propertyId || undefined,
                      category: fee.category,
                      ruleType: fee.type,
                      calculationBasis: fee.basis,
                      currency,
                      percentage: Number(fee.percentage),
                      fixedAmount: Number(fee.fixed),
                      minimumAmount: Number(fee.minimum),
                      cleaningMarkup: Number(fee.cleaningMarkup),
                      maintenanceMarkup: Number(fee.maintenanceMarkup),
                      effectiveFrom: fee.from,
                      effectiveTo: fee.to || undefined,
                    }),
                  "Fee rule saved",
                )
              }
            >
              Save fee rule
            </Button>
            <Button
              variant="outline"
              disabled={busy || !ownerId || Number(feeBaseAmount) < 0}
              onClick={() => void calculateFee()}
            >
              Calculate
            </Button>
            <Field label="Calculation base">
              <Input
                inputMode="decimal"
                value={feeBaseAmount}
                onChange={(event) => setFeeBaseAmount(event.target.value)}
              />
            </Field>
          </div>
          {feeCalculation && (
            <div
              className="mt-3 rounded-field bg-shell p-3 text-sm"
              role="status"
            >
              {feeCalculation.ruleId ? (
                <>
                  <strong>
                    {formatMoney(feeCalculation.calculatedAmount, currency)}
                  </strong>{" "}
                  fee on {formatMoney(feeCalculation.baseAmount, currency)} ·{" "}
                  {feeCalculation.percentageAmount
                    ? `${formatMoney(feeCalculation.percentageAmount, currency)} percentage`
                    : "fixed/minimum"}
                </>
              ) : (
                "No active fee rule matches this date and scope."
              )}
            </div>
          )}
          <div className="mt-4 space-y-2">
            {feeRules.map((rule) => (
              <div
                className="flex items-center justify-between rounded-field border border-sand-border p-3 text-sm"
                key={rule.id}
              >
                <span>
                  {rule.category} · {rule.ruleType} · {rule.effectiveFrom} ·{" "}
                  {rule.currency}
                </span>
                <strong>
                  {rule.percentage
                    ? `${rule.percentage}%`
                    : formatMoney(rule.fixedAmount, rule.currency)}
                </strong>
              </div>
            ))}
          </div>
        </Card>
      </section>
      <section className="mb-6 grid gap-5 lg:grid-cols-[1.2fr_0.8fr]">
        <Card>
          <h2 className="font-display text-2xl">Auditable owner ledger</h2>
          <p className="mt-1 text-sm text-sand-600">
            Posted journals are balanced, currency-separated and immutable.
            Corrections use reversals.
          </p>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <Field label="Source">
              <Select
                value={journal.source}
                onChange={(event) =>
                  setJournal({ ...journal, source: event.target.value })
                }
              >
                <option>RENT_RECEIPT</option>
                <option>OWNER_EXPENSE</option>
                <option>THIRD_PARTY_EXPENSE</option>
                <option>OWNER_BILLING</option>
                <option>OWNER_BILLING_RECEIPT</option>
              </Select>
            </Field>
            <Field label="Amount">
              <Input
                inputMode="decimal"
                value={journal.amount}
                onChange={(event) =>
                  setJournal({ ...journal, amount: event.target.value })
                }
              />
            </Field>
          </div>
          <Field className="mt-3" label="Memo">
            <Input
              value={journal.memo}
              onChange={(event) =>
                setJournal({ ...journal, memo: event.target.value })
              }
            />
          </Field>
          <label className="mt-3 flex items-center gap-2 text-sm">
            <input
              checked={journal.reconciled}
              onChange={(event) =>
                setJournal({ ...journal, reconciled: event.target.checked })
              }
              type="checkbox"
            />{" "}
            Reconciled against external evidence
          </label>
          <Button
            className="mt-3"
            disabled={busy || !ownerId || Number(journal.amount) <= 0}
            onClick={() => void postJournal()}
          >
            Post balanced entry
          </Button>
          <Field className="mt-3" label="Reconciliation note">
            <Input
              value={reconcileReason}
              onChange={(event) => setReconcileReason(event.target.value)}
            />
          </Field>
          <div className="mt-4 space-y-2">
            {journals.length === 0 ? (
              <p className="text-sm text-sand-600">
                No posted journals in this scope.
              </p>
            ) : (
              journals.slice(0, 25).map((item) => (
                <div
                  className="rounded-field border border-sand-border p-3"
                  key={item.id}
                >
                  <button
                    className="flex w-full items-center justify-between gap-2 text-left"
                    aria-expanded={expandedJournalId === item.id}
                    onClick={() =>
                      setExpandedJournalId(
                        expandedJournalId === item.id ? null : item.id,
                      )
                    }
                  >
                    <span>
                      <strong>{item.sourceType}</strong>
                      <span className="ml-2 text-xs text-sand-600">
                        {item.accountingDate} ·{" "}
                        {formatMoney(item.totalDebit, item.currency)}
                      </span>
                    </span>
                    <ChevronDown
                      className={
                        expandedJournalId === item.id ? "rotate-180" : ""
                      }
                      size={16}
                    />
                  </button>
                  <div className="mt-2 flex flex-wrap items-center gap-2">
                    <StatusChip value={item.reconciliationStatus} />
                    {item.reconciliationStatus !== "RECONCILED" && (
                      <Button
                        variant="outline"
                        disabled={busy}
                        onClick={() => void reconcile(item)}
                      >
                        Reconcile
                      </Button>
                    )}
                    {item.sourceType !== "REVERSAL" && (
                      <Button
                        variant="destructive"
                        disabled={busy}
                        confirmMessage="Post a reversal for this journal?"
                        onClick={() =>
                          void run(
                            () =>
                              api.reverseP0Journal(token, item.id, {
                                reason:
                                  "Correction requested in manager workspace",
                                idempotencyKey: randomId(),
                              }),
                            "Reversal posted",
                          )
                        }
                      >
                        Reverse
                      </Button>
                    )}
                  </div>
                  {expandedJournalId === item.id && (
                    <div className="mt-3 space-y-1 border-t border-sand-border pt-3 text-xs">
                      {item.lines.map((line) => (
                        <div
                          className="flex justify-between gap-3"
                          key={line.id}
                        >
                          <span>
                            {line.accountCode} · {line.description}
                          </span>
                          <span>
                            {line.debit
                              ? `Dr ${formatMoney(line.debit, item.currency)}`
                              : `Cr ${formatMoney(line.credit, item.currency)}`}
                          </span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        </Card>
        <Card>
          <h2 className="font-display text-2xl">Chart of accounts</h2>
          <p className="mt-1 text-sm text-sand-600">
            Separate owner/client money, PM revenue and third-party/suspense
            balances.
          </p>
          <div className="mt-3 max-h-96 space-y-2 overflow-auto">
            {accounts.map((account) => (
              <div
                className="rounded-field border border-sand-border p-3 text-xs"
                key={account.id}
              >
                <strong>{account.code}</strong>
                <span className="ml-2">{account.name}</span>
                <div className="mt-1 flex flex-wrap gap-2 text-sand-600">
                  <span>{account.accountType}</span>
                  <span>{account.currency}</span>
                  {account.isClientMoney && <span>CLIENT</span>}
                  {account.isPmMoney && <span>PM</span>}
                  {account.isThirdParty && <span>THIRD-PARTY/SUSPENSE</span>}
                </div>
              </div>
            ))}
          </div>
        </Card>
      </section>
      <section className="mb-6 grid gap-5 lg:grid-cols-2">
        <Card>
          <h2 className="font-display text-2xl">Statements & profitability</h2>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <Field label="From">
              <Input
                type="date"
                value={period.from}
                onChange={(event) =>
                  setPeriod({ ...period, from: event.target.value })
                }
              />
            </Field>
            <Field label="To">
              <Input
                type="date"
                value={period.to}
                onChange={(event) =>
                  setPeriod({ ...period, to: event.target.value })
                }
              />
            </Field>
          </div>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button
              disabled={busy || !ownerId}
              onClick={() =>
                void run(
                  async () =>
                    setStatement(
                      await api.getP0Statement(token, {
                        ownerUserId: ownerId,
                        propertyId: propertyId || undefined,
                        currency,
                        from: period.from,
                        to: period.to,
                      }),
                    ),
                  "Statement preview built",
                )
              }
            >
              Build preview
            </Button>
            <Button
              disabled={busy || !ownerId}
              variant="outline"
              onClick={() =>
                void run(
                  async () =>
                    setStatement(
                      await api.finalizeP0Statement(token, {
                        ownerUserId: ownerId,
                        propertyId: propertyId || undefined,
                        currency,
                        from: period.from,
                        to: period.to,
                        idempotencyKey: randomId(),
                      }),
                    ),
                  "Statement finalized",
                )
              }
            >
              Finalize
            </Button>
            {statement?.snapshotId && (
              <Button
                disabled={busy}
                variant="outline"
                onClick={() =>
                  void run(async () => {
                    const exportFile = await api.exportP0Statement(
                      token,
                      statement.snapshotId!,
                    );
                    const anchor = document.createElement("a");
                    anchor.href = `data:${exportFile.contentType};base64,${exportFile.contentBase64}`;
                    anchor.download = exportFile.fileName;
                    anchor.click();
                  }, "Statement export downloaded")
                }
              >
                <FileDown size={15} /> Export
              </Button>
            )}
          </div>
          {statement && (
            <div className="mt-4 rounded-field bg-shell p-3 text-sm">
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                <span>
                  Income
                  <strong className="block">
                    {formatMoney(statement.income, currency)}
                  </strong>
                </span>
                <span>
                  Expenses
                  <strong className="block">
                    {formatMoney(statement.expenses, currency)}
                  </strong>
                </span>
                <span>
                  Fees
                  <strong className="block">
                    {formatMoney(statement.managementFees, currency)}
                  </strong>
                </span>
                <span>
                  Closing
                  <strong className="block">
                    {formatMoney(statement.closingBalance, currency)}
                  </strong>
                </span>
              </div>
              {statement.entries.length > 0 && (
                <div className="mt-3 space-y-1 border-t border-sand-border pt-3 text-xs">
                  {statement.entries.slice(0, 12).map((entry) => (
                    <div
                      className="flex justify-between gap-2"
                      key={entry.journalId}
                    >
                      <span>
                        {entry.date} · {entry.description}
                      </span>
                      <span>{formatMoney(entry.net, currency)}</span>
                    </div>
                  ))}
                </div>
              )}
              {statement.hasUnresolvedSuspense && (
                <p className="m-0 mt-2 text-amber-text" role="alert">
                  Resolve suspense before finalization or payout.
                </p>
              )}
            </div>
          )}
        </Card>
        <Card>
          <h2 className="font-display text-2xl">Profitability snapshot</h2>
          {profitability ? (
            <>
              <div className="mt-3 grid gap-3 sm:grid-cols-2">
                <div className="rounded-field bg-shell p-3">
                  Accrual income
                  <strong className="block">
                    {formatMoney(profitability.income, currency)}
                  </strong>
                </div>
                <div className="rounded-field bg-shell p-3">
                  Expenses
                  <strong className="block">
                    {formatMoney(profitability.expenses, currency)}
                  </strong>
                </div>
                <div className="rounded-field bg-shell p-3">
                  Cash collected
                  <strong className="block">
                    {formatMoney(profitability.cashCollected, currency)}
                  </strong>
                </div>
                <div className="rounded-field bg-shell p-3">
                  PM margin
                  <strong className="block">
                    {formatMoney(profitability.pmMargin, currency)}
                  </strong>
                </div>
              </div>
              <div className="mt-4 space-y-2 text-xs">
                {profitability.rows.map((row) => (
                  <div
                    className="flex justify-between rounded-field border border-sand-border p-2"
                    key={`${row.ownerUserId}-${row.propertyId ?? "ALL"}`}
                  >
                    <span>
                      {row.propertyId
                        ? `Property ${row.propertyId.slice(0, 8)}`
                        : "Portfolio"}
                    </span>
                    <span>{formatMoney(row.ownerNet, currency)} owner net</span>
                  </div>
                ))}
              </div>
            </>
          ) : (
            <LoadingState label="Calculating profitability" />
          )}
        </Card>
      </section>
      <section className="mb-6 grid gap-5 lg:grid-cols-2">
        <Card>
          <h2 className="font-display text-2xl">Owner approvals</h2>
          <p className="mt-1 text-sm text-sand-600">
            Thresholds come from the active agreement; evidence and decisions
            are persisted with a history.
          </p>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <Field label="Type">
              <Select
                value={approval.type}
                onChange={(event) =>
                  setApproval({ ...approval, type: event.target.value })
                }
              >
                <option>EXPENSE</option>
                <option>MAINTENANCE</option>
                <option>OWNER_BILLING</option>
              </Select>
            </Field>
            <Field label="Description">
              <Input
                value={approval.description}
                onChange={(event) =>
                  setApproval({ ...approval, description: event.target.value })
                }
              />
            </Field>
            <Field label="Amount">
              <Input
                inputMode="decimal"
                value={approval.amount}
                onChange={(event) =>
                  setApproval({ ...approval, amount: event.target.value })
                }
              />
            </Field>
            <Field label="Evidence document ID">
              <Select value={approval.evidence} onChange={(event) => setApproval({ ...approval, evidence: event.target.value })}>
                <option value="">No evidence document</option>
                {selectedDocumentOptions.map((document) => <option key={document.id} value={document.id}>{document.title} · {document.fileName}</option>)}
              </Select>
            </Field>
            <Field label="Related record type">
              <Select value={approval.sourceType} onChange={(event) => setApproval({ ...approval, sourceType: event.target.value })}>
                <option value="">General approval</option>
                <option value="MAINTENANCE">Maintenance</option>
                <option value="WORK_ORDER">Work order</option>
                <option value="EXPENSE">Expense</option>
              </Select>
            </Field>
            <Field label="Related record ID">
              <Input value={approval.sourceId} onChange={(event) => setApproval({ ...approval, sourceId: event.target.value })} placeholder="Required when a related type is selected" />
            </Field>
            <Field label="Decision deadline">
              <Input type="datetime-local" value={approval.expiresAt} onChange={(event) => setApproval({ ...approval, expiresAt: event.target.value })} />
            </Field>
          </div>
          <Button
            className="mt-3"
            disabled={
              busy ||
              !ownerId ||
              !approval.description ||
              Number(approval.amount) <= 0 ||
              (!!approval.sourceType && !approval.sourceId)
            }
            onClick={() =>
              void run(
                () =>
                  api.createP0Approval(token, {
                    ownerUserId: ownerId,
                    propertyId: propertyId || undefined,
                    approvalType: approval.type,
                    description: approval.description,
                    amount: Number(approval.amount),
                    currency,
                    evidenceDocumentIds: approval.evidence
                      ? [approval.evidence]
                      : undefined,
                    sourceType: approval.sourceType || undefined,
                    sourceId: approval.sourceId || undefined,
                    expiresAt: approval.expiresAt ? new Date(approval.expiresAt).toISOString() : undefined,
                    idempotencyKey: `approval-${randomId()}`,
                  }),
                "Approval request created",
              )
            }
          >
            Request approval
          </Button>
          <div className="mt-4 space-y-2">
            {approvals.map((item) => (
              <div
                className="rounded-field border border-sand-border p-3"
                key={item.id}
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <strong>{item.description}</strong>
                    <p className="m-0 text-xs text-sand-600">
                      {formatMoney(item.amount, item.currency)} · threshold{" "}
                      {formatMoney(item.threshold, item.currency)}
                    </p>
                  </div>
                  <StatusChip value={item.status} />
                </div>
                {item.decisionReason && (
                  <p className="m-0 mt-1 text-xs text-sand-600">
                    Decision: {item.decisionReason}
                  </p>
                )}
                {item.sourceType && item.sourceId && <p className="m-0 mt-1 text-xs text-sand-600">Related {item.sourceType.toLowerCase()}: {item.sourceId}</p>}
                {item.expiresAt && <p className="m-0 mt-1 text-xs text-sand-600">Decision deadline: {new Date(item.expiresAt).toLocaleString()}</p>}
                {(item.evidence ?? []).length > 0 && <div className="mt-2 flex flex-wrap gap-2" aria-label="Approval evidence">{item.evidence?.map((document) => <Button key={document.id} type="button" variant="outline" onClick={() => void openApprovalEvidence(document.id)}>Open {document.title || document.fileName}</Button>)}</div>}
                {item.status === "REQUIRED" && (
                  <p className="mt-2 rounded-field bg-shell p-2 text-xs text-sand-600">Awaiting the linked owner’s decision in the owner portal. Managers cannot approve on an owner’s behalf.</p>
                )}
                {item.history.length > 0 && (
                  <details className="mt-2 text-xs">
                    <summary className="cursor-pointer font-semibold">
                      Decision history
                    </summary>
                    {item.history.map((event) => (
                      <div className="mt-1" key={event.id}>
                        {event.eventType} · {event.fromStatus || "—"} →{" "}
                        {event.toStatus} · {event.reason}
                      </div>
                    ))}
                  </details>
                )}
              </div>
            ))}
          </div>
        </Card>
        <Card>
          <h2 className="font-display text-2xl">
            <Wallet size={18} className="mr-2 inline" />
            Payout controls
          </h2>
          <p className="mt-1 text-sm text-sand-600">
            Cash-based availability is separate from accrual reporting. A
            different authorised person must approve every batch.
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button
              disabled={busy || !ownerId}
              onClick={() =>
                void run(async () => {
                  const available = await api.getP0PayoutAvailability(token, {
                    ownerUserId: ownerId,
                    currency,
                    from: period.from,
                    to: period.to,
                  });
                  if (available.payableAmount <= 0)
                    throw new Error(
                      "No reconciled payable balance is available.",
                    );
                  await api.createP0Payout(token, {
                    ownerUserId: ownerId,
                    currency,
                    periodFrom: period.from,
                    periodTo: period.to,
                    idempotencyKey: randomId(),
                    statementSnapshotId: statement?.snapshotId ?? undefined,
                  });
                }, "Payout batch created")
              }
            >
              Create payout batch
            </Button>
          </div>
          <div className="mt-4 space-y-2">
            {payouts.map((item) => (
              <div
                className="rounded-field border border-sand-border p-3"
                key={item.id}
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <strong>{formatMoney(item.amount, item.currency)}</strong>
                    <p className="m-0 text-xs text-sand-600">
                      {item.periodFrom} → {item.periodTo}
                    </p>
                  </div>
                  <StatusChip value={item.status} />
                </div>
                <div className="mt-2 flex flex-wrap gap-2">
                  {item.status === "PENDING_APPROVAL" && (
                    <Button
                      variant="outline"
                      disabled={busy}
                      onClick={() =>
                        void run(
                          () =>
                            api.approveP0Payout(token, item.id, {
                              reason: "Second-person approval",
                              rowVersion: item.rowVersion,
                            }),
                          "Payout approved",
                        )
                      }
                    >
                      Approve
                    </Button>
                  )}
                  {item.status === "APPROVED" && (
                    <Button
                      variant="outline"
                      disabled={busy}
                      onClick={() =>
                        void run(
                          () => api.processP0Payout(token, item.id),
                          "Payout processed",
                        )
                      }
                    >
                      Process
                    </Button>
                  )}
                  {["PENDING_APPROVAL", "APPROVED"].includes(item.status) && (
                    <Button
                      variant="destructive"
                      disabled={busy}
                      onClick={() =>
                        void run(
                          () =>
                            api.cancelP0Payout(token, item.id, {
                              reason: "Cancelled in manager workspace",
                              rowVersion: item.rowVersion,
                            }),
                          "Payout cancelled",
                        )
                      }
                    >
                      Cancel
                    </Button>
                  )}
                  {item.status === "FAILED" && (
                    <Button
                      variant="outline"
                      disabled={busy}
                      onClick={() =>
                        void run(
                          () => api.retryP0Payout(token, item.id),
                          "Payout retry queued",
                        )
                      }
                    >
                      Retry
                    </Button>
                  )}
                </div>
                {item.history.length > 0 && (
                  <details className="mt-2 text-xs">
                    <summary className="cursor-pointer font-semibold">
                      Payout history
                    </summary>
                    {item.history.map((event) => (
                      <div className="mt-1" key={event.id}>
                        {event.eventType} · {event.fromStatus || "—"} →{" "}
                        {event.toStatus} · {event.reason}
                      </div>
                    ))}
                  </details>
                )}
              </div>
            ))}
          </div>
        </Card>
      </section>
      <section className="grid gap-5 lg:grid-cols-2">
        <Card>
          <h2 className="font-display text-2xl">
            <ShieldCheck size={18} className="mr-2 inline" />
            Team and RBAC
          </h2>
          <p className="mt-1 text-sm text-sand-600">
            Scope staff by owner/property and separate finance from payout
            approval.
          </p>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <Field label="Staff user id">
              <Input
                value={staffForm.staffUserId}
                onChange={(event) =>
                  setStaffForm({
                    ...staffForm,
                    staffUserId: event.target.value,
                  })
                }
                placeholder="Existing user id"
              />
            </Field>
            <Field label="Role">
              <Select
                value={staffForm.role}
                onChange={(event) =>
                  setStaffForm({ ...staffForm, role: event.target.value })
                }
              >
                <option>OPERATIONS</option>
                <option>FINANCE</option>
                <option>APPROVER</option>
              </Select>
            </Field>
            <Field label="Approval limit">
              <Input
                inputMode="decimal"
                value={staffForm.limit}
                onChange={(event) =>
                  setStaffForm({ ...staffForm, limit: event.target.value })
                }
              />
            </Field>
          </div>
          <div className="mt-3 flex flex-wrap gap-4 text-sm">
            <label>
              <input
                checked={staffForm.finance}
                onChange={(event) =>
                  setStaffForm({ ...staffForm, finance: event.target.checked })
                }
                type="checkbox"
              />{" "}
              Finance access
            </label>
            <label>
              <input
                checked={staffForm.payouts}
                onChange={(event) =>
                  setStaffForm({ ...staffForm, payouts: event.target.checked })
                }
                type="checkbox"
              />{" "}
              Payout approval
            </label>
          </div>
          <Button
            className="mt-3"
            disabled={busy || !staffForm.staffUserId}
            onClick={() =>
              void run(
                () =>
                  api.inviteP0Staff(token, {
                    staffUserId: staffForm.staffUserId,
                    role: staffForm.role,
                    ownerIds: ownerId ? [ownerId] : [],
                    propertyIds: propertyId ? [propertyId] : [],
                    canManageFinance: staffForm.finance,
                    canApprovePayouts: staffForm.payouts,
                    approvalLimit: Number(staffForm.limit),
                  }),
                "Team invitation created",
              )
            }
          >
            Invite team member
          </Button>
          <div className="mt-4 space-y-2">
            {staff.length === 0 ? (
              <p className="text-sm text-sand-600">
                No P0 staff memberships yet.
              </p>
            ) : (
              staff.map((item) => (
                <div
                  className="rounded-field border border-sand-border p-3 text-sm"
                  key={item.id}
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span>
                      {item.staffUserId.slice(0, 8)} · {item.role}
                    </span>
                    <StatusChip value={item.status} />
                  </div>
                  <div className="mt-2 flex flex-wrap gap-2">
                    <Button
                      variant="ghost"
                      disabled={busy}
                      onClick={() => void toggleStaffHistory(item.id)}
                      aria-expanded={expandedStaffId === item.id}
                    >
                      {expandedStaffId === item.id
                        ? "Hide activity"
                        : "View activity"}
                    </Button>
                    {item.status !== "REVOKED" && (
                      <Button
                        variant="outline"
                        disabled={busy}
                        onClick={() =>
                          void run(
                            () =>
                              api.updateP0Staff(token, item.id, {
                                role: item.role,
                                ownerIds: item.ownerIds,
                                propertyIds: item.propertyIds,
                                canManageFinance: item.canManageFinance,
                                canApprovePayouts: item.canApprovePayouts,
                                approvalLimit: item.approvalLimit,
                                status: item.status,
                                rowVersion: item.rowVersion,
                              }),
                            "Team member updated",
                          )
                        }
                      >
                        Save scope
                      </Button>
                    )}
                    {item.status !== "REVOKED" && (
                      <Button
                        variant="destructive"
                        disabled={busy}
                        onClick={() =>
                          void run(
                            () =>
                              api.revokeP0Staff(token, item.id, {
                                status: "REVOKED",
                                reason: "Access revoked by manager",
                              }),
                            "Team member revoked",
                          )
                        }
                      >
                        Revoke
                      </Button>
                    )}
                    {item.status === "INVITED" && (
                      <Button
                        variant="ghost"
                        disabled={busy}
                        onClick={() =>
                          void run(
                            () => api.acceptP0Staff(token, item.id),
                            "Invitation accepted",
                          )
                        }
                      >
                        Accept invitation
                      </Button>
                    )}
                  </div>
                  {expandedStaffId === item.id && (
                    <div className="mt-3 space-y-1 border-t border-sand-border pt-3 text-xs">
                      {(staffHistory[item.id] ?? []).length === 0 ? (
                        <span className="text-sand-600">No activity recorded.</span>
                      ) : (
                        staffHistory[item.id].map((event) => (
                          <div key={event.id}>
                            <strong>{event.eventType}</strong> ·{" "}
                            {event.fromStatus || "—"} → {event.toStatus} ·{" "}
                            {event.reason} ·{" "}
                            {new Date(event.createdAt).toLocaleString()}
                          </div>
                        ))
                      )}
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        </Card>
        <Card>
          <h2 className="font-display text-2xl">Control notes</h2>
          <ul className="m-0 mt-3 list-disc space-y-2 pl-5 text-sm text-sand-600">
            <li>JMD and USD are separate balances; no FX is performed.</li>
            <li>
              Statements use accrual-style journal summaries; payouts require
              reconciled cash.
            </li>
            <li>
              Ambiguous legacy activity belongs in suspense and blocks final
              statements/payouts until resolved.
            </li>
            <li>
              Posted journals and history rows cannot be edited or deleted; use
              a reversal with a reason.
            </li>
            <li>
              This is an application accounting control model, not a claim of
              Jamaican legal trust-accounting compliance.
            </li>
          </ul>
        </Card>
      </section>
    </div>
  );
}

export default PropertyManagerP0Page;
