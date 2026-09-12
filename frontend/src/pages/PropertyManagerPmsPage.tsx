import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { LoadingState } from "../components/ui/LoadingState";
import { PageHeader } from "../components/ui/PageHeader";
import { api, formatMoney, type PropertyManagerDashboard, type PropertyManagerReport } from "../lib/api";
import { userSafeErrorMessage } from "../lib/errorMessages";
import type { AuthController } from "../hooks/useAuth";
import { PropertyManagerModuleContent, type PropertyManagerModule } from "../features/propertyManager/modules/PropertyManagerModules";

function ErrorBox({ error }: { error: unknown }) {
  return error ? <div className="mb-4 rounded-field bg-coral-tint px-4 py-3 text-sm text-coral-text" role="alert">{userSafeErrorMessage(error, "The request could not be completed.")}</div> : null;
}

export function PropertyManagerPmsPage({ auth, module = "reports" }: { auth: AuthController; module?: PropertyManagerModule }) {
  const token = auth.session?.accessToken ?? "";
  const userId = auth.session?.userId;
  const [data, setData] = useState<PropertyManagerDashboard | null>(null);
  const [report, setReport] = useState<PropertyManagerReport | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);
  const [owner, setOwner] = useState("");
  const [property, setProperty] = useState("");
  const [subscriptionAction, setSubscriptionAction] = useState("PAUSE");
  const [subscriptionTargetTier, setSubscriptionTargetTier] = useState("Professional");
  const [subscriptionReason, setSubscriptionReason] = useState("");
  const [comment, setComment] = useState("");
  const [workScope, setWorkScope] = useState("");
  const [notice, setNotice] = useState<string | null>(null);
  const [gateDelivery, setGateDelivery] = useState<Record<string, Awaited<ReturnType<typeof api.getPropertyManagerGateDelivery>>>>({});
  const [subscriptionEvents, setSubscriptionEvents] = useState<Awaited<ReturnType<typeof api.getPropertyManagerSubscriptionEvents>>>([]);
  const [qrs, setQrs] = useState<Awaited<ReturnType<typeof api.listPropertyManagerQrs>>>([]);
  const [qrReason, setQrReason] = useState("No longer needed");
  const [selectedPayments, setSelectedPayments] = useState<Awaited<ReturnType<typeof api.getPropertyManagerPayments>>>([]);
  const [calendar, setCalendar] = useState<Awaited<ReturnType<typeof api.getPropertyManagerCalendar>>>([]);
  const loadInFlight = useRef<Promise<void> | null>(null);

  const load = useCallback(async () => {
    if (!userId) return;
    if (loadInFlight.current) return loadInFlight.current;
    setError(null);
    const request = (async () => {
      // The manager-scoped endpoints lazily ensure the manager record exists.
      // Resolve the dashboard first so dependent reads cannot race that bootstrap.
      const dashboard = await api.getPropertyManagerDashboard(token);
      const [nextReport, payments, events, qrRows, history] = await Promise.all([
        module === "reports" || module === "invoices" ? api.getPropertyManagerReport(token) : null,
        module === "payments" || module === "invoices" ? api.getPropertyManagerPayments(token) : [],
        module === "calendar" ? api.getPropertyManagerCalendar(token) : [],
        module === "gates" ? api.listPropertyManagerQrs(token) : [],
        module === "subscription" ? api.getPropertyManagerSubscriptionEvents(token) : [],
      ]);
      setData(dashboard);
      setReport(nextReport);
      setSelectedPayments(payments);
      setCalendar(events);
      setQrs(qrRows);
      setSubscriptionEvents(history);
    })();
    loadInFlight.current = request;
    void request.then(() => {
      if (loadInFlight.current === request) loadInFlight.current = null;
    }, () => {
      if (loadInFlight.current === request) loadInFlight.current = null;
    });
    return request;
  }, [module, token, userId]);

  useEffect(() => {
    setData(null);
    void load().catch(setError);
  }, [load]);

  const run = async (action: () => Promise<unknown>, refresh = true) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await action();
      if (refresh) await load();
    } catch (caught) {
      setError(caught);
    } finally {
      setBusy(false);
    }
  };

  const selectedOwner = owner || data?.owners[0]?.ownerUserId || "";
  const selectedProperty = property || data?.properties.find((item) => item.ownerUserId === selectedOwner)?.id || data?.properties[0]?.id || "";
  const ownerName = useMemo(() => data?.owners.find((item) => item.ownerUserId === selectedOwner)?.displayName ?? "owner", [data, selectedOwner]);
  const title = module === "reports" ? "Live portfolio reporting" : module === "insurance" ? "Coverage readiness" : module.replaceAll("-", " ").replace(/\b\w/g, (letter) => letter.toUpperCase());

  if (!auth.session) return <div className="product-page"><PageHeader eyebrow="Property Manager" title="Sign in required" copy="Sign in with a Property Manager account to access portfolio operations." /></div>;
  if (!data) return <div className="product-page"><ErrorBox error={error} />{error ? <Button disabled={busy} onClick={() => void run(load, false)}>Retry loading workspace</Button> : <LoadingState label="Loading live Property Manager data…" />}</div>;

  return <div className="product-page"><PageHeader eyebrow="Property Manager · real API workspace" title={title} copy="Every list and mutation on this screen is loaded from the manager-scoped API. Empty, loading, validation and recovery states remain visible." actions={<Button disabled={busy} onClick={() => void run(load, false)}><RefreshCw size={16} /> Refresh</Button>} /><ErrorBox error={error} />{notice && <div className="mb-4 rounded-field bg-success-tint px-4 py-3 text-sm font-semibold text-success-text" role="status">{notice}</div>}<section className="mb-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-4"><Card><small className="text-sand-600">Properties</small><strong className="mt-1 block font-display text-3xl">{data.totalProperties}</strong></Card><Card><small className="text-sand-600">Owners</small><strong className="mt-1 block font-display text-3xl">{data.totalOwners}</strong></Card><Card><small className="text-sand-600">Outstanding</small><strong className="mt-1 block font-display text-3xl">{formatMoney(data.outstandingBalance)}</strong></Card><Card><small className="text-sand-600">Open work</small><strong className="mt-1 block font-display text-3xl">{data.openMaintenance}</strong></Card></section><PropertyManagerModuleContent module={module} title={title} data={data} token={token} report={report} busy={busy} run={run} selectedOwner={selectedOwner} setOwner={setOwner} selectedProperty={selectedProperty} setProperty={setProperty} ownerName={ownerName} workScope={workScope} setWorkScope={setWorkScope} subscriptionAction={subscriptionAction} setSubscriptionAction={setSubscriptionAction} subscriptionTargetTier={subscriptionTargetTier} setSubscriptionTargetTier={setSubscriptionTargetTier} subscriptionReason={subscriptionReason} setSubscriptionReason={setSubscriptionReason} subscriptionEvents={subscriptionEvents} setSubscriptionEvents={setSubscriptionEvents} qrs={qrs} qrReason={qrReason} setQrReason={setQrReason} gateDelivery={gateDelivery} setGateDelivery={setGateDelivery} selectedPayments={selectedPayments} calendar={calendar} comment={comment} setComment={setComment} /></div>;
}
