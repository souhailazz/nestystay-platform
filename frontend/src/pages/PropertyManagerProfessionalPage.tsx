import { useCallback, useEffect, useMemo, useState } from "react";
import {
  api,
  type P0Approval,
  type PmAsset,
  type PmCalendarItem,
  type PmChecklistTemplate,
  type PmCleaning,
  type PmCorrectiveAction,
  type PmCostLine,
  type PmIncident,
  type PmInspection,
  type PmMaintenanceAttachment,
  type PmMaintenanceCase,
  type PmMaintenanceEvent,
  type PmOperationalDashboard,
  type PmOwnerBlock,
  type PmProfessionalWorkOrder,
  type PmReservation,
  type PmReservationEvent,
  type PmWorkOrderQuote,
  type PropertyManagerOwner,
  type PropertyManagerProperty,
  type PropertyManagerVendor,
} from "../lib/api";
import type { AuthController } from "../hooks/useAuth";
import { AppLink } from "../components/AppLink";
import { Button, buttonClassName } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { Field, Input, Select } from "../components/ui/Input";
import { LoadingState } from "../components/ui/LoadingState";
import { PageHeader } from "../components/ui/PageHeader";

type Tab =
  | "reservations"
  | "calendar"
  | "blocks"
  | "maintenance"
  | "work-orders"
  | "cleaning"
  | "inspections"
  | "assets"
  | "incidents";
const tabs: Array<[Tab, string]> = [
  ["reservations", "Reservations"],
  ["calendar", "Master calendar"],
  ["blocks", "Owner blocks"],
  ["maintenance", "Maintenance"],
  ["work-orders", "Work orders"],
  ["cleaning", "Readiness"],
  ["inspections", "Inspections"],
  ["assets", "Assets & inventory"],
  ["incidents", "Incidents"],
];
const maintenanceNextStates: Record<string, string[]> = {
  REQUESTED: ["TRIAGED", "CANCELLED"],
  TRIAGED: ["QUOTING", "OWNER_APPROVAL", "CANCELLED"],
  QUOTING: ["OWNER_APPROVAL", "ASSIGNED", "CANCELLED"],
  OWNER_APPROVAL: ["ASSIGNED", "CANCELLED"],
  ASSIGNED: ["SCHEDULED", "CANCELLED"],
  SCHEDULED: ["IN_PROGRESS", "CANCELLED"],
  IN_PROGRESS: ["COMPLETED"],
  COMPLETED: ["CLOSED", "IN_PROGRESS"],
  CLOSED: ["IN_PROGRESS"],
  CANCELLED: [],
};
const workOrderNextStates: Record<string, string[]> = {
  REQUEST: ["QUOTING", "CANCELLED"],
  QUOTING: ["OWNER_APPROVAL", "APPROVED", "ASSIGNED", "CANCELLED"],
  OWNER_APPROVAL: ["APPROVED", "CANCELLED"],
  APPROVED: ["ASSIGNED", "CANCELLED"],
  ASSIGNED: ["SCHEDULED", "CANCELLED"],
  SCHEDULED: ["IN_PROGRESS", "CANCELLED"],
  IN_PROGRESS: ["COMPLETED"],
  COMPLETED: ["CLOSED", "IN_PROGRESS"],
  CLOSED: ["IN_PROGRESS"],
  CANCELLED: [],
};
function ErrorBox({ error }: { error: unknown }) {
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

function CalendarGrid({
  items,
  view,
  start,
  days,
}: {
  items: PmCalendarItem[];
  view: "day" | "week" | "month";
  start: Date;
  days: number;
}) {
  return (
    <div className="mt-4 overflow-x-auto" aria-label={`${view} calendar`}>
      <div className="grid min-w-[720px] grid-cols-7 gap-px rounded-field border border-line bg-line">
        {Array.from({ length: days }, (_, index) => {
          const date = new Date(start);
          date.setDate(start.getDate() + index);
          const key = date.toISOString().slice(0, 10);
          const next = new Date(date);
          next.setDate(date.getDate() + 1);
          const dayItems = items.filter(
            (item) =>
              new Date(item.startsAt) < next && new Date(item.endsAt) > date,
          );
          return (
            <div
              className="min-h-28 bg-shell p-2"
              key={key}
              tabIndex={0}
              aria-label={`Events for ${date.toLocaleDateString()}`}
            >
              <div className="text-xs font-semibold text-ink-muted">
                {date.toLocaleDateString(undefined, {
                  weekday: "short",
                  month: "short",
                  day: "numeric",
                })}
              </div>
              <div className="mt-2 space-y-1">
                {dayItems.map((item) => (
                  <div
                    className={
                      item.conflictLevel === "BLOCKING"
                        ? "rounded border border-coral-text bg-coral-tint px-2 py-1 text-xs"
                        : item.conflictLevel === "WARNING"
                          ? "rounded border border-sun-deep bg-sun-wash px-2 py-1 text-xs"
                          : "rounded bg-mint-tint px-2 py-1 text-xs"
                    }
                    key={`${item.type}-${item.sourceId}`}
                  >
                    <strong>{item.type}</strong>
                    <span className="ml-1">{item.title}</span>
                    {item.conflictLevel !== "NONE" && (
                      <span className="ml-1 font-semibold">
                        · {item.conflictLevel}
                      </span>
                    )}
                  </div>
                ))}
                {dayItems.length === 0 && (
                  <span className="text-xs text-ink-muted">No events</span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export function PropertyManagerProfessionalPage({
  auth,
}: {
  auth: AuthController;
}) {
  const token = auth.session?.accessToken ?? "";
  const initialTab =
    typeof window === "undefined"
      ? "reservations"
      : new URLSearchParams(window.location.search).get("tab");
  const [tab, setTab] = useState<Tab>(
    tabs.some(([id]) => id === initialTab)
      ? (initialTab as Tab)
      : "reservations",
  );
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [owner, setOwner] = useState("");
  const [property, setProperty] = useState("");
  const [reservations, setReservations] = useState<PmReservation[]>([]);
  const [calendar, setCalendar] = useState<PmCalendarItem[]>([]);
  const [blocks, setBlocks] = useState<PmOwnerBlock[]>([]);
  const [maintenance, setMaintenance] = useState<PmMaintenanceCase[]>([]);
  const [cleaning, setCleaning] = useState<PmCleaning[]>([]);
  const [inspections, setInspections] = useState<PmInspection[]>([]);
  const [assets, setAssets] = useState<PmAsset[]>([]);
  const [incidents, setIncidents] = useState<PmIncident[]>([]);
  const [reservationHistory, setReservationHistory] = useState<
    Record<string, PmReservationEvent[]>
  >({});
  const [reservationDateDrafts, setReservationDateDrafts] = useState<
    Record<string, { checkIn: string; checkOut: string }>
  >({});
  const [reservationDatePreviews, setReservationDatePreviews] = useState<
    Record<
      string,
      Awaited<ReturnType<typeof api.previewProfessionalReservationDateChange>>
    >
  >({});
  const [reservationReasons, setReservationReasons] = useState<
    Record<string, string>
  >({});
  const [operationalDashboard, setOperationalDashboard] =
    useState<PmOperationalDashboard | null>(null);
  const [maintenanceHistory, setMaintenanceHistory] = useState<
    Record<string, PmMaintenanceEvent[]>
  >({});
  const [maintenanceAttachments, setMaintenanceAttachments] = useState<
    Record<string, PmMaintenanceAttachment[]>
  >({});
  const [selectedMaintenanceId, setSelectedMaintenanceId] = useState("");
  const [quoteVendorId, setQuoteVendorId] = useState("");
  const [quoteAmount, setQuoteAmount] = useState("");
  const [quoteScope, setQuoteScope] = useState("");
  const [ownerApprovalId, setOwnerApprovalId] = useState("");
  const [maintenanceQuotes, setMaintenanceQuotes] = useState<
    Record<
      string,
      Awaited<ReturnType<typeof api.listProfessionalMaintenanceQuotes>>
    >
  >({});
  const [maintenanceApprovals, setMaintenanceApprovals] = useState<
    P0Approval[]
  >([]);
  const [maintenanceCostLines, setMaintenanceCostLines] = useState<
    Record<string, PmCostLine[]>
  >({});
  const [workOrders, setWorkOrders] = useState<PmProfessionalWorkOrder[]>([]);
  const [workOrderCostLines, setWorkOrderCostLines] = useState<
    Record<string, PmCostLine[]>
  >({});
  const [workOrderQuotes, setWorkOrderQuotes] = useState<
    Record<string, PmWorkOrderQuote[]>
  >({});
  const [workOrderAttachments, setWorkOrderAttachments] = useState<
    Record<string, PmMaintenanceAttachment[]>
  >({});
  const [selectedWorkOrderId, setSelectedWorkOrderId] = useState("");
  const [costDraft, setCostDraft] = useState({
    lineType: "LABOR",
    responsibility: "OWNER",
    description: "",
    amount: "",
    receiptAttachmentId: "",
  });
  const [correctionDraft, setCorrectionDraft] = useState({
    expenseAmount: "",
    ownerCharge: "",
    managerFee: "0",
    reason: "",
  });
  const [workOrderReason, setWorkOrderReason] = useState("Operational update");
  const [checklistTemplates, setChecklistTemplates] = useState<
    PmChecklistTemplate[]
  >([]);
  const [correctiveActions, setCorrectiveActions] = useState<
    PmCorrectiveAction[]
  >([]);
  const [templateDraft, setTemplateDraft] = useState({
    name: "Property safety inspection",
    itemsJson:
      '[{"id":"safety","label":"Safety controls","required":true,"completed":false}]',
  });
  const [selectedTemplateId, setSelectedTemplateId] = useState("");
  const [cleaningTemplateDraft, setCleaningTemplateDraft] = useState({
    name: "Turnover readiness",
    itemsJson:
      '[{"id":"turnover","label":"Turnover checklist","required":true,"completed":false}]',
  });
  const [selectedCleaningTemplateId, setSelectedCleaningTemplateId] =
    useState("");
  const [inspectionFinding, setInspectionFinding] = useState("");
  const [inspectionWorkOrderScope, setInspectionWorkOrderScope] = useState("");
  const [portfolioOwners, setPortfolioOwners] = useState<
    PropertyManagerOwner[]
  >([]);
  const [portfolioProperties, setPortfolioProperties] = useState<
    PropertyManagerProperty[]
  >([]);
  const [portfolioVendors, setPortfolioVendors] = useState<
    PropertyManagerVendor[]
  >([]);
  const [reservationSearch, setReservationSearch] = useState("");
  const [reservationStatus, setReservationStatus] = useState("ALL");
  const [reservationOwnerFilter, setReservationOwnerFilter] = useState("");
  const [reservationPropertyFilter, setReservationPropertyFilter] =
    useState("");
  const [form, setForm] = useState({
    title: "",
    description: "",
    category: "OWNER_STAY",
    reason: "Owner stay",
    notes: "",
    startsAt: "",
    endsAt: "",
    dueAt: "",
    assetTag: "",
    assetName: "",
    assetCategory: "GENERAL",
    assetDescription: "",
    assetSerial: "",
    assetPurchaseDate: "",
    assetPurchaseCost: "",
    assetWarrantyExpiry: "",
    assetCondition: "GOOD",
    incidentDescription: "",
    inspectionAt: "",
  });
  const [reservationNote, setReservationNote] = useState("");
  const [calendarView, setCalendarView] = useState<"day" | "week" | "month">(
    "month",
  );
  const [calendarAnchor, setCalendarAnchor] = useState(() => new Date());
  const [calendarOwnerFilter, setCalendarOwnerFilter] = useState("");
  const [calendarPropertyFilter, setCalendarPropertyFilter] = useState("");
  const [calendarTypeFilter, setCalendarTypeFilter] = useState("ALL");

  const calendarRange = useMemo(() => {
    const start = new Date(calendarAnchor);
    start.setHours(0, 0, 0, 0);
    if (calendarView === "week") {
      const weekday = (start.getDay() + 6) % 7;
      start.setDate(start.getDate() - weekday);
    }
    if (calendarView === "month") start.setDate(1);
    const end = new Date(start);
    if (calendarView === "day") end.setDate(end.getDate() + 1);
    else if (calendarView === "week") end.setDate(end.getDate() + 7);
    else end.setMonth(end.getMonth() + 1);
    return {
      start,
      end,
      days: Math.round((end.getTime() - start.getTime()) / 86400000),
    };
  }, [calendarAnchor, calendarView]);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const start = calendarRange.start.toISOString();
      const end = calendarRange.end.toISOString();
      const reservationQuery = {
        ...(reservationSearch.trim()
          ? { search: reservationSearch.trim() }
          : {}),
        ...(reservationStatus !== "ALL" ? { status: reservationStatus } : {}),
        ...(reservationOwnerFilter
          ? { ownerUserId: reservationOwnerFilter }
          : {}),
        ...(reservationPropertyFilter
          ? { propertyId: reservationPropertyFilter }
          : {}),
      };
      const reservationsRequest =
        Object.keys(reservationQuery).length > 0
          ? api.listProfessionalReservations(token, reservationQuery)
          : api.listProfessionalReservations(token);
      const [
        dashboard,
        operational,
        reservationsRows,
        calendarRows,
        blockRows,
        maintenanceRows,
        workOrderRows,
        cleaningRows,
        inspectionRows,
        assetRows,
        incidentRows,
        approvalRows,
        templateRows,
        correctiveRows,
      ] = await Promise.all([
        api.getPropertyManagerDashboard(token),
        api.getProfessionalOperationalDashboard(token),
        reservationsRequest,
        api.listProfessionalCalendar(token, start, end, {
          propertyId: calendarPropertyFilter || undefined,
          ownerUserId: calendarOwnerFilter || undefined,
          eventType:
            calendarTypeFilter !== "ALL" ? calendarTypeFilter : undefined,
        }),
        api.listProfessionalOwnerBlocks(token),
        api.listProfessionalMaintenance(token),
        api.listProfessionalWorkOrders(token),
        api.listProfessionalCleaning(token),
        api.listProfessionalInspections(token),
        api.listProfessionalAssets(token),
        api.listProfessionalIncidents(token),
        api.getP0Approvals(token, {
          status: "APPROVED",
          page: 1,
          pageSize: 200,
        }),
        api.listProfessionalChecklistTemplates(token),
        api.listProfessionalCorrectiveActions(token),
      ]);
      setPortfolioOwners(dashboard.owners);
      setPortfolioProperties(dashboard.properties);
      setPortfolioVendors(dashboard.vendors);
      setOperationalDashboard(operational);
      setOwner(owner || dashboard.owners[0]?.ownerUserId || "");
      setProperty(property || dashboard.properties[0]?.id || "");
      setReservations(reservationsRows);
      setCalendar(calendarRows);
      setBlocks(blockRows);
      setMaintenance(maintenanceRows);
      setWorkOrders(workOrderRows);
      setCleaning(cleaningRows);
      setInspections(inspectionRows);
      setAssets(assetRows);
      setIncidents(incidentRows);
      setMaintenanceApprovals(approvalRows);
      setChecklistTemplates(templateRows);
      setCorrectiveActions(correctiveRows);
    } catch (e) {
      setError(e);
    } finally {
      setLoading(false);
    }
  }, [
    token,
    calendarRange,
    calendarOwnerFilter,
    calendarPropertyFilter,
    calendarTypeFilter,
    reservationSearch,
    reservationStatus,
    reservationOwnerFilter,
    reservationPropertyFilter,
  ]);
  useEffect(() => {
    void load();
  }, [load]);
  const selectedPropertyOwner = useMemo(
    () => ({ owner, property }),
    [owner, property],
  );
  const run = async (action: () => Promise<unknown>, message: string) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await action();
      setNotice(message);
      await load();
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  };
  const createBlock = () =>
    run(
      () =>
        api.createProfessionalOwnerBlock(token, {
          ownerUserId: selectedPropertyOwner.owner,
          propertyId: selectedPropertyOwner.property,
          startsAt: new Date(form.startsAt).toISOString(),
          endsAt: new Date(form.endsAt).toISOString(),
          timeZone: "America/Jamaica",
          category: form.category,
          reason: form.reason,
          notes: form.notes,
        }),
      "Owner block created",
    );
  const createMaintenance = () =>
    run(
      () =>
        api.createProfessionalMaintenance(token, {
          ownerUserId: selectedPropertyOwner.owner,
          propertyId: selectedPropertyOwner.property,
          title: form.title,
          description: form.description,
        }),
      "Maintenance case created",
    );
  const addQuote = () => {
    if (
      !selectedMaintenanceId ||
      !quoteVendorId.trim() ||
      !quoteAmount ||
      !quoteScope.trim()
    )
      return;
    return run(
      () =>
        api.addProfessionalMaintenanceQuote(token, selectedMaintenanceId, {
          vendorId: quoteVendorId.trim(),
          amount: Number(quoteAmount),
          scope: quoteScope.trim(),
        }),
      "Vendor quote added",
    );
  };
  const showMaintenanceHistory = async (id: string) => {
    setSelectedMaintenanceId(id);
    try {
      const history = await api.getProfessionalMaintenanceHistory(token, id);
      setMaintenanceHistory((current) => ({ ...current, [id]: history }));
    } catch (e) {
      setError(e);
    }
  };
  const showMaintenanceAttachments = async (id: string) => {
    setSelectedMaintenanceId(id);
    try {
      const files = await api.listMaintenanceAttachments(token, id);
      setMaintenanceAttachments((current) => ({ ...current, [id]: files }));
    } catch (e) {
      setError(e);
    }
  };
  const uploadMaintenanceAttachment = (id: string, file: File | undefined) => {
    if (!file) return;
    const reader = new FileReader();
    reader.onerror = () =>
      setError(
        new Error(
          "The attachment could not be read. Try a smaller PDF or image.",
        ),
      );
    reader.onload = () => {
      const value = typeof reader.result === "string" ? reader.result : "";
      const comma = value.indexOf(",");
      const contentBase64 = comma >= 0 ? value.slice(comma + 1) : value;
      void run(async () => {
        await api.addMaintenanceAttachment(token, {
          maintenanceId: id,
          fileName: file.name,
          contentType: file.type || "application/octet-stream",
          contentBase64,
        });
        const files = await api.listMaintenanceAttachments(token, id);
        setMaintenanceAttachments((current) => ({ ...current, [id]: files }));
      }, "Maintenance evidence uploaded");
    };
    reader.readAsDataURL(file);
  };
  const uploadWorkOrderAttachment = (id: string, file: File | undefined) => {
    if (!file) return;
    const reader = new FileReader();
    reader.onerror = () =>
      setError(
        new Error("The receipt could not be read. Try a smaller PDF or image."),
      );
    reader.onload = () => {
      const value = typeof reader.result === "string" ? reader.result : "";
      const comma = value.indexOf(",");
      const contentBase64 = comma >= 0 ? value.slice(comma + 1) : value;
      void run(async () => {
        await api.addMaintenanceAttachment(token, {
          maintenanceId: id,
          fileName: file.name,
          contentType: file.type || "application/octet-stream",
          contentBase64,
        });
        const files = await api.listMaintenanceAttachments(token, id);
        setWorkOrderAttachments((current) => ({ ...current, [id]: files }));
      }, "Work-order receipt uploaded");
    };
    reader.readAsDataURL(file);
  };
  const downloadMaintenanceAttachment = async (
    id: string,
    attachmentId: string,
  ) => {
    try {
      const result = await api.getMaintenanceAttachmentDownload(
        token,
        id,
        attachmentId,
      );
      window.open(result.url, "_blank", "noopener,noreferrer");
    } catch (e) {
      setError(e);
    }
  };
  const showMaintenanceQuotes = async (id: string) => {
    setSelectedMaintenanceId(id);
    try {
      const quotes = await api.listProfessionalMaintenanceQuotes(token, id);
      setMaintenanceQuotes((current) => ({ ...current, [id]: quotes }));
    } catch (e) {
      setError(e);
    }
  };
  const showMaintenanceCosts = async (id: string) => {
    try {
      const lines = await api.listProfessionalMaintenanceCostLines(token, id);
      setMaintenanceCostLines((current) => ({ ...current, [id]: lines }));
    } catch (e) {
      setError(e);
    }
  };
  const addMaintenanceCost = (item: PmMaintenanceCase) => {
    if (!costDraft.description.trim() || !costDraft.amount) return;
    return run(async () => {
      await api.addProfessionalMaintenanceCostLine(token, item.id, {
        lineType: costDraft.lineType,
        responsibility: costDraft.responsibility,
        description: costDraft.description.trim(),
        amount: Number(costDraft.amount),
        currency: item.currency,
        receiptAttachmentId: costDraft.receiptAttachmentId || undefined,
        idempotencyKey: `maintenance-cost-${item.id}-${Date.now()}`,
      });
      setCostDraft((current) => ({
        ...current,
        description: "",
        amount: "",
        receiptAttachmentId: "",
      }));
    }, "Maintenance cost line saved");
  };
  const correctMaintenance = (item: PmMaintenanceCase) => {
    if (
      !correctionDraft.reason.trim() ||
      !correctionDraft.expenseAmount ||
      !correctionDraft.ownerCharge
    )
      return;
    return run(
      () =>
        api.correctProfessionalMaintenanceFinancial(token, item.id, {
          expenseAmount: Number(correctionDraft.expenseAmount),
          ownerCharge: Number(correctionDraft.ownerCharge),
          managerFee: Number(correctionDraft.managerFee || 0),
          reason: correctionDraft.reason.trim(),
          idempotencyKey: `maintenance-correction-${item.id}-${Date.now()}`,
          rowVersion: item.rowVersion,
          ownerApprovalId: ownerApprovalId || undefined,
        }),
      "Financial correction posted as reversal and replacement",
    );
  };
  const showWorkOrderDetails = async (id: string) => {
    setSelectedWorkOrderId(id);
    try {
      const [lines, quotes, attachments] = await Promise.all([
        api.listProfessionalWorkOrderCostLines(token, id),
        api.listProfessionalWorkOrderQuotes(token, id),
        api.listMaintenanceAttachments(token, id),
      ]);
      setWorkOrderCostLines((current) => ({ ...current, [id]: lines }));
      setWorkOrderQuotes((current) => ({ ...current, [id]: quotes }));
      setWorkOrderAttachments((current) => ({ ...current, [id]: attachments }));
    } catch (e) {
      setError(e);
    }
  };
  const addWorkOrderCost = (item: PmProfessionalWorkOrder) => {
    if (!costDraft.description.trim() || !costDraft.amount) return;
    return run(
      () =>
        api.addProfessionalWorkOrderCostLine(token, item.id, {
          lineType: costDraft.lineType,
          responsibility: costDraft.responsibility,
          description: costDraft.description.trim(),
          amount: Number(costDraft.amount),
          currency: item.currency,
          receiptAttachmentId: costDraft.receiptAttachmentId || undefined,
          idempotencyKey: `work-order-cost-${item.id}-${Date.now()}`,
        }),
      "Work-order cost line saved",
    );
  };
  const addWorkOrderQuote = (item: PmProfessionalWorkOrder) => {
    if (!quoteVendorId || !quoteAmount || !quoteScope.trim()) return;
    return run(
      () =>
        api.addProfessionalWorkOrderQuote(token, item.id, {
          vendorId: quoteVendorId,
          amount: Number(quoteAmount),
          scope: quoteScope.trim(),
          currency: item.currency,
          idempotencyKey: `work-order-quote-${item.id}-${Date.now()}`,
        }),
      "Vendor bid added to work order",
    );
  };
  const updateWorkOrder = (
    item: PmProfessionalWorkOrder,
    status: string,
    selectedQuoteId?: string,
  ) =>
    run(
      () =>
        api.updateProfessionalWorkOrder(token, item.id, {
          status,
          selectedQuoteId,
          approvedAmount: selectedQuoteId
            ? workOrderQuotes[item.id]?.find(
                (quote) => quote.id === selectedQuoteId,
              )?.amount
            : (item.approvedAmount ?? undefined),
          ownerApprovalId: ownerApprovalId || item.ownerApprovalId || undefined,
          reason: workOrderReason.trim() || "Operational update",
          idempotencyKey: `work-order-${item.id}-${status}-${Date.now()}`,
          rowVersion: item.rowVersion,
        }),
      "Work order updated",
    );
  const correctWorkOrder = (item: PmProfessionalWorkOrder) => {
    const total = Number(correctionDraft.expenseAmount);
    const ownerAmount = Number(correctionDraft.ownerCharge);
    if (
      !correctionDraft.reason.trim() ||
      !total ||
      ownerAmount < 0 ||
      ownerAmount > total
    )
      return;
    return run(
      () =>
        api.correctProfessionalWorkOrderFinancial(token, item.id, {
          laborAmount: total,
          materialAmount: 0,
          taxAmount: 0,
          otherAmount: 0,
          ownerResponsibility: ownerAmount,
          managerResponsibility: total - ownerAmount,
          vendorResponsibility: 0,
          reason: correctionDraft.reason.trim(),
          idempotencyKey: `work-order-correction-${item.id}-${Date.now()}`,
          rowVersion: item.rowVersion,
          ownerApprovalId: ownerApprovalId || item.ownerApprovalId || undefined,
        }),
      "Work-order reversal and replacement posted",
    );
  };
  const showReservationHistory = async (id: string) => {
    try {
      const history = await api.getProfessionalReservationHistory(token, id);
      setReservationHistory((current) => ({ ...current, [id]: history }));
    } catch (e) {
      setError(e);
    }
  };
  const previewReservationDates = async (reservation: PmReservation) => {
    const draft = reservationDateDrafts[reservation.bookingId];
    if (!draft?.checkIn || !draft.checkOut) return;
    try {
      const preview = await api.previewProfessionalReservationDateChange(
        token,
        reservation.bookingId,
        draft,
      );
      setReservationDatePreviews((current) => ({
        ...current,
        [reservation.bookingId]: preview,
      }));
    } catch (e) {
      setError(e);
    }
  };
  const applyReservationDates = (reservation: PmReservation) => {
    const preview = reservationDatePreviews[reservation.bookingId];
    const draft = reservationDateDrafts[reservation.bookingId];
    const reason = reservationReasons[reservation.bookingId]?.trim();
    if (!preview?.allowed || !draft || !reason) return;
    return run(
      () =>
        api.amendProfessionalReservation(token, reservation.bookingId, {
          checkIn: draft.checkIn,
          checkOut: draft.checkOut,
          reason,
          idempotencyKey: `pm-amend-${reservation.bookingId}-${draft.checkIn}-${draft.checkOut}`,
          expectedUpdatedAt: reservation.updatedAt ?? undefined,
        }),
      "Reservation amendment confirmed",
    );
  };
  const cancelReservation = (reservation: PmReservation) => {
    const reason = reservationReasons[reservation.bookingId]?.trim();
    if (!reason) return;
    return run(
      () =>
        api.cancelProfessionalReservation(token, reservation.bookingId, {
          reason,
          idempotencyKey: `pm-cancel-${reservation.bookingId}-${reservation.updatedAt ?? "initial"}`,
          expectedUpdatedAt: reservation.updatedAt ?? undefined,
        }),
      "Reservation cancelled",
    );
  };
  const rebookReservation = (reservation: PmReservation) => {
    const draft = reservationDateDrafts[reservation.bookingId];
    const reason = reservationReasons[reservation.bookingId]?.trim();
    if (!draft?.checkIn || !draft.checkOut || !reason) return;
    return run(
      () =>
        api.rebookProfessionalReservation(token, reservation.bookingId, {
          checkIn: draft.checkIn,
          checkOut: draft.checkOut,
          reason,
          idempotencyKey: `pm-rebook-${reservation.bookingId}-${draft.checkIn}-${draft.checkOut}`,
        }),
      "Replacement reservation created",
    );
  };
  const selectMaintenanceQuote = (
    maintenanceId: string,
    quote: Awaited<
      ReturnType<typeof api.listProfessionalMaintenanceQuotes>
    >[number],
  ) => {
    const item = maintenance.find((row) => row.id === maintenanceId);
    if (!item) return;
    return run(
      () =>
        api.transitionProfessionalMaintenance(token, maintenanceId, {
          status: "ASSIGNED",
          vendorId: quote.vendorId,
          approvedAmount: quote.amount,
          ownerApprovalId: ownerApprovalId || undefined,
          rowVersion: item.rowVersion,
          details: `Selected quote ${quote.id}`,
        }),
      "Quote selected and work assigned",
    );
  };
  const createCleaning = () =>
    run(
      () =>
        api.createProfessionalCleaning(token, {
          propertyId: selectedPropertyOwner.property,
          dueAt: new Date(form.dueAt).toISOString(),
          checklistJson: cleaningTemplateDraft.itemsJson,
          templateId: selectedCleaningTemplateId || undefined,
        }),
      "Readiness task created from a versioned checklist snapshot",
    );
  const completeCleaningChecklist = (item: PmCleaning) => {
    try {
      const checklist = JSON.parse(item.checklistJson) as Array<
        Record<string, unknown>
      >;
      const completed = checklist.map((entry) => ({
        ...entry,
        completed: true,
      }));
      return run(
        () =>
          api.updateProfessionalCleaning(token, item.id, {
            status: "IN_PROGRESS",
            checklistJson: JSON.stringify(completed),
            issues: item.issues,
            photosJson: item.photosJson,
            rowVersion: item.rowVersion,
          }),
        "Checklist saved",
      );
    } catch {
      setError(
        new Error(
          "The checklist data is invalid and must be repaired before updating.",
        ),
      );
      return undefined;
    }
  };
  const createAsset = () =>
    run(
      () =>
        api.createProfessionalAsset(token, {
          propertyId: selectedPropertyOwner.property,
          assetTag: form.assetTag,
          name: form.assetName,
          category: form.assetCategory,
          description: form.assetDescription,
          serialReference: form.assetSerial,
          purchaseDate: form.assetPurchaseDate || undefined,
          purchaseCost: form.assetPurchaseCost
            ? Number(form.assetPurchaseCost)
            : undefined,
          warrantyExpiry: form.assetWarrantyExpiry || undefined,
          condition: form.assetCondition,
        }),
      "Asset added",
    );
  const createIncident = () =>
    run(
      () =>
        api.createProfessionalIncident(token, {
          propertyId: selectedPropertyOwner.property,
          incidentType: "GENERAL",
          severity: "MEDIUM",
          occurredAt: new Date().toISOString(),
          description: form.incidentDescription,
        }),
      "Incident logged",
    );
  const createInspection = () =>
    run(
      () =>
        api.createProfessionalInspection(token, {
          propertyId: selectedPropertyOwner.property,
          inspectionType: "ROUTINE",
          scheduledAt: new Date(form.inspectionAt).toISOString(),
          templateId: selectedTemplateId || undefined,
        }),
      "Inspection scheduled",
    );
  const toggleInspectionChecklist = (
    item: PmInspection,
    index: number,
    completed: boolean,
  ) => {
    try {
      const checklist = JSON.parse(item.checklistJson) as Array<
        Record<string, unknown>
      >;
      if (!checklist[index]) return;
      checklist[index] = { ...checklist[index], completed };
      return run(
        () =>
          api.updateProfessionalInspection(token, item.id, {
            status: item.status === "SCHEDULED" ? "IN_PROGRESS" : item.status,
            checklistJson: JSON.stringify(checklist),
            evidenceJson: item.evidenceJson,
            findingsJson: item.findingsJson,
            rowVersion: item.rowVersion,
          }),
        "Inspection checklist saved",
      );
    } catch {
      setError(
        new Error(
          "The inspection checklist data is invalid and must be repaired before updating.",
        ),
      );
      return undefined;
    }
  };
  const createInspectionWorkOrder = (inspectionId: string) => {
    if (!inspectionWorkOrderScope.trim()) return;
    return run(
      () =>
        api.createProfessionalInspectionWorkOrder(token, inspectionId, {
          scope: inspectionWorkOrderScope.trim(),
        }),
      "Corrective work order created",
    );
  };
  const createInspectionTemplate = () =>
    run(
      () =>
        api.createProfessionalChecklistTemplate(token, {
          name: templateDraft.name.trim(),
          workflowType: "INSPECTION",
          itemsJson: templateDraft.itemsJson,
        }),
      "Versioned inspection template created",
    );
  const assignInspectionTemplate = () =>
    run(
      () =>
        api.assignProfessionalChecklistTemplate(
          token,
          property,
          selectedTemplateId,
        ),
      "Inspection template assigned to property",
    );
  const createCleaningTemplate = () =>
    run(
      () =>
        api.createProfessionalChecklistTemplate(token, {
          name: cleaningTemplateDraft.name.trim(),
          workflowType: "CLEANING",
          itemsJson: cleaningTemplateDraft.itemsJson,
        }),
      "Versioned cleaning template created",
    );
  const assignCleaningTemplate = () =>
    run(
      () =>
        api.assignProfessionalChecklistTemplate(
          token,
          property,
          selectedCleaningTemplateId,
        ),
      "Cleaning template assigned to property",
    );
  const failInspection = (item: PmInspection) => {
    if (!inspectionFinding.trim()) return;
    let checklist: Array<Record<string, unknown>>;
    try {
      checklist = JSON.parse(item.checklistJson) as Array<
        Record<string, unknown>
      >;
    } catch {
      setError(new Error("Inspection checklist data is invalid."));
      return;
    }
    const completed = checklist.map((entry) => ({ ...entry, completed: true }));
    return run(
      () =>
        api.updateProfessionalInspection(token, item.id, {
          status: "FAILED",
          checklistJson: JSON.stringify(completed),
          evidenceJson: item.evidenceJson,
          findingsJson: JSON.stringify([
            {
              checklistItemId: "observed-failure",
              finding: inspectionFinding.trim(),
              severity: "HIGH",
              correctiveRequired: true,
            },
          ]),
          rowVersion: item.rowVersion,
        }),
      "Inspection failed and corrective action opened",
    );
  };
  const advanceCorrectiveAction = (item: PmCorrectiveAction, status: string) =>
    run(
      () =>
        api.updateProfessionalCorrectiveAction(token, item.id, {
          status,
          resolutionNotes:
            status === "CANCELLED"
              ? "Cancelled by property manager with review"
              : "Corrective work completed and ready for retest",
          rowVersion: item.rowVersion,
          idempotencyKey: `corrective-${item.id}-${status}-${Date.now()}`,
        }),
      "Corrective action updated",
    );
  const createReinspection = (item: PmCorrectiveAction) =>
    run(
      () =>
        api.createProfessionalReinspection(token, item.id, {
          scheduledAt: new Date(form.inspectionAt).toISOString(),
          idempotencyKey: `reinspection-${item.id}-${form.inspectionAt}`,
        }),
      "Reinspection scheduled",
    );

  const reservationActions =
    tab === "reservations" ? (
      <>
        <Card className="mb-4">
          <h2 className="text-lg font-semibold">Reservation safety controls</h2>
          <p className="text-sm text-ink-muted">
            Every date change is previewed and then confirmed through an
            idempotent amendment command. Paid price-changing stays must be
            cancelled and rebooked.
          </p>
          <div className="mt-2 text-xs text-ink-muted">
            The server enforces portfolio ownership, booking and owner-block
            conflicts, payment rules, stale-write recovery and complete history.
          </div>
        </Card>
        <Card className="mb-4">
          <h2 className="text-lg font-semibold">Reservation actions</h2>
          <div className="mt-3 space-y-2">
            {reservations.map((r) => {
              const draft = reservationDateDrafts[r.bookingId] ?? {
                checkIn: r.checkIn.slice(0, 10),
                checkOut: r.checkOut.slice(0, 10),
              };
              const preview = reservationDatePreviews[r.bookingId];
              const cancelled = r.status.toUpperCase() === "CANCELLED";
              const reason = reservationReasons[r.bookingId] ?? "";
              return (
                <div
                  key={r.bookingId}
                  id={`reservation-${r.bookingId}`}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-field border border-line p-3 text-sm"
                >
                  <span>
                    <strong>
                      {r.guestName || r.guestEmail || r.guestUserId}
                    </strong>{" "}
                    · {r.status} · {r.paymentStatus} · {r.currency}{" "}
                    {r.totalAmount.toFixed(2)}
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => void showReservationHistory(r.bookingId)}
                  >
                    Timeline
                  </Button>
                  <div className="basis-full grid gap-2 sm:grid-cols-3">
                    <Field
                      label={
                        cancelled ? "Replacement check-in" : "New check-in"
                      }
                    >
                      <Input
                        type="date"
                        value={draft.checkIn}
                        onChange={(e) => {
                          setReservationDateDrafts((current) => ({
                            ...current,
                            [r.bookingId]: {
                              ...draft,
                              checkIn: e.target.value,
                            },
                          }));
                          setReservationDatePreviews((current) => {
                            const next = { ...current };
                            delete next[r.bookingId];
                            return next;
                          });
                        }}
                      />
                    </Field>
                    <Field
                      label={
                        cancelled ? "Replacement check-out" : "New check-out"
                      }
                    >
                      <Input
                        type="date"
                        value={draft.checkOut}
                        onChange={(e) => {
                          setReservationDateDrafts((current) => ({
                            ...current,
                            [r.bookingId]: {
                              ...draft,
                              checkOut: e.target.value,
                            },
                          }));
                          setReservationDatePreviews((current) => {
                            const next = { ...current };
                            delete next[r.bookingId];
                            return next;
                          });
                        }}
                      />
                    </Field>
                    <Field label="Reason">
                      <Input
                        value={reason}
                        onChange={(e) =>
                          setReservationReasons((current) => ({
                            ...current,
                            [r.bookingId]: e.target.value,
                          }))
                        }
                        placeholder={
                          cancelled
                            ? "Reason for replacement stay"
                            : "Reason for amendment or cancellation"
                        }
                      />
                    </Field>
                  </div>
                  <div className="basis-full flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      disabled={
                        busy || cancelled || !draft.checkIn || !draft.checkOut
                      }
                      onClick={() => void previewReservationDates(r)}
                    >
                      Preview amendment
                    </Button>
                    {!cancelled && (
                      <Button
                        type="button"
                        variant="destructive"
                        disabled={busy || !reason.trim()}
                        onClick={() => void cancelReservation(r)}
                      >
                        Cancel reservation
                      </Button>
                    )}
                    {cancelled && (
                      <Button
                        type="button"
                        disabled={
                          busy ||
                          !reason.trim() ||
                          !draft.checkIn ||
                          !draft.checkOut
                        }
                        onClick={() => void rebookReservation(r)}
                      >
                        Create replacement reservation
                      </Button>
                    )}
                  </div>
                  {preview && (
                    <div
                      className={
                        preview.allowed
                          ? "basis-full rounded-field bg-mint-tint p-2 text-xs"
                          : "basis-full rounded-field bg-coral-tint p-2 text-xs text-coral-text"
                      }
                      role="status"
                    >
                      {preview.allowed
                        ? `Available · ${preview.proposedNights} nights · ${preview.currency} ${preview.proposedTotal.toFixed(2)}`
                        : `Cannot amend: ${preview.blockingReason}`}{" "}
                      {preview.allowed && (
                        <Button
                          type="button"
                          variant="outline"
                          className="ml-2"
                          disabled={busy || !reason.trim()}
                          onClick={() => void applyReservationDates(r)}
                        >
                          Confirm amendment
                        </Button>
                      )}
                      {preview.requiresRebooking && (
                        <span className="ml-2 font-semibold">
                          Cancel this reservation first, then use the preserved
                          replacement dates.
                        </span>
                      )}
                    </div>
                  )}
                  {reservationHistory[r.bookingId] && (
                    <div
                      className="basis-full rounded-field bg-shell p-2 text-xs text-ink-muted"
                      aria-label="Reservation history"
                    >
                      {reservationHistory[r.bookingId].map((event) => (
                        <div key={event.id}>
                          {new Date(event.createdAt).toLocaleString()} ·{" "}
                          {event.eventType} · {event.reason}
                          {event.relatedBookingId
                            ? ` · related reservation ${event.relatedBookingId}`
                            : ""}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              );
            })}
            {reservations.length === 0 && (
              <p className="text-sm text-ink-muted">
                No reservations match the current filters.
              </p>
            )}
          </div>
        </Card>
      </>
    ) : null;
  const maintenanceControls =
    tab === "maintenance" ? (
      <Card className="mb-4">
        <h2 className="text-lg font-semibold">Approval and cost controls</h2>
        <p className="text-sm text-ink-muted">
          Assignment and spend are blocked until the linked owner approval is
          approved when the agreement threshold requires it.
        </p>
        <div className="mt-3 max-w-xl">
          <Field label="Approved owner decision (when required)">
            <Select
              value={ownerApprovalId}
              onChange={(e) => setOwnerApprovalId(e.target.value)}
            >
              <option value="">Select an approved request</option>
              {maintenanceApprovals
                .filter(
                  (item) =>
                    (!owner || item.ownerUserId === owner) &&
                    (!property || item.propertyId === property) &&
                    (!item.sourceType || item.sourceType === "MAINTENANCE"),
                )
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.description} · {item.currency}{" "}
                    {item.amount.toFixed(2)}
                  </option>
                ))}
            </Select>
          </Field>
        </div>
        <AppLink className="mt-3 inline-block underline" href="/pm/approvals">
          Create or review owner approval requests
        </AppLink>
      </Card>
    ) : null;
  const maintenanceFinancialControls =
    tab === "maintenance" ? (
      <Card className="mb-4">
        <h2 className="text-lg font-semibold">
          Receipts, classified costs and corrections
        </h2>
        <p className="text-sm text-ink-muted">
          Cost lines identify owner, PM-company or vendor responsibility. Posted
          journals are never edited: corrections create a linked reversal and
          replacement.
        </p>
        <Field className="mt-3" label="Lifecycle reason">
          <Input
            value={workOrderReason}
            onChange={(e) => setWorkOrderReason(e.target.value)}
            placeholder="Required when cancelling or reopening work"
          />
        </Field>
        <div className="mt-3 grid gap-2 sm:grid-cols-4">
          <Field label="Cost type">
            <Select
              value={costDraft.lineType}
              onChange={(e) =>
                setCostDraft((current) => ({
                  ...current,
                  lineType: e.target.value,
                }))
              }
            >
              <option>LABOR</option>
              <option>MATERIAL</option>
              <option>TAX</option>
              <option>FEE</option>
              <option>OTHER</option>
            </Select>
          </Field>
          <Field label="Responsibility">
            <Select
              value={costDraft.responsibility}
              onChange={(e) =>
                setCostDraft((current) => ({
                  ...current,
                  responsibility: e.target.value,
                }))
              }
            >
              <option>OWNER</option>
              <option>PM</option>
              <option>VENDOR</option>
            </Select>
          </Field>
          <Field label="Description">
            <Input
              value={costDraft.description}
              onChange={(e) =>
                setCostDraft((current) => ({
                  ...current,
                  description: e.target.value,
                }))
              }
            />
          </Field>
          <Field label="Amount">
            <Input
              inputMode="decimal"
              value={costDraft.amount}
              onChange={(e) =>
                setCostDraft((current) => ({
                  ...current,
                  amount: e.target.value,
                }))
              }
            />
          </Field>
        </div>
        <div className="mt-4 space-y-3">
          {maintenance.map((item) => (
            <div
              className="rounded-field border border-line p-3"
              key={`finance-${item.id}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-sm">
                  <strong>{item.number}</strong> ·{" "}
                  {item.financialStatus ??
                    (item.financiallyPosted ? "POSTED" : "UNPOSTED")}{" "}
                  · owner {item.currency} {item.ownerCharge.toFixed(2)} / total{" "}
                  {item.expenseAmount.toFixed(2)}
                </span>
                <span className="flex flex-wrap gap-2">
                  {!item.financiallyPosted &&
                  maintenanceAttachments[item.id]?.length ? (
                    <Select
                      aria-label={`Receipt for ${item.number}`}
                      value={costDraft.receiptAttachmentId}
                      onChange={(e) =>
                        setCostDraft((current) => ({
                          ...current,
                          receiptAttachmentId: e.target.value,
                        }))
                      }
                    >
                      <option value="">No receipt linked</option>
                      {maintenanceAttachments[item.id].map((file) => (
                        <option key={file.id} value={file.id}>
                          {file.fileName}
                        </option>
                      ))}
                    </Select>
                  ) : null}
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => void showMaintenanceCosts(item.id)}
                  >
                    View cost lines
                  </Button>
                  {!item.financiallyPosted && (
                    <Button
                      type="button"
                      disabled={
                        busy || !costDraft.description || !costDraft.amount
                      }
                      onClick={() => void addMaintenanceCost(item)}
                    >
                      Add cost to this case
                    </Button>
                  )}
                  {["REQUESTED", "TRIAGED", "QUOTING", "OWNER_APPROVAL", "ASSIGNED", "SCHEDULED"].includes(item.status) && (
                    <Button
                      type="button"
                      variant="destructive"
                      disabled={busy || !workOrderReason.trim()}
                      onClick={() =>
                        void run(
                          () =>
                            api.transitionProfessionalMaintenance(
                              token,
                              item.id,
                              {
                                status: "CANCELLED",
                                rowVersion: item.rowVersion,
                                details: workOrderReason.trim(),
                              },
                            ),
                          "Maintenance cancelled with reason",
                        )
                      }
                    >
                      Cancel
                    </Button>
                  )}
                  {(item.status === "COMPLETED" ||
                    item.status === "CLOSED") && (
                    <Button
                      type="button"
                      variant="outline"
                      disabled={busy || !workOrderReason.trim()}
                      onClick={() =>
                        void run(
                          () =>
                            api.transitionProfessionalMaintenance(
                              token,
                              item.id,
                              {
                                status: "IN_PROGRESS",
                                rowVersion: item.rowVersion,
                                details: workOrderReason.trim(),
                                ownerApprovalId: ownerApprovalId || undefined,
                              },
                            ),
                          "Maintenance reopened with history",
                        )
                      }
                    >
                      Reopen
                    </Button>
                  )}
                </span>
              </div>
              {maintenanceCostLines[item.id] && (
                <div className="mt-2 space-y-1 rounded-field bg-shell p-2 text-xs">
                  {maintenanceCostLines[item.id].length === 0
                    ? "No cost lines yet."
                    : maintenanceCostLines[item.id].map((line) => (
                        <div key={line.id}>
                          {line.lineType} · {line.responsibility} ·{" "}
                          {line.description} · {line.currency}{" "}
                          {line.amount.toFixed(2)}
                          {line.receiptFileName
                            ? ` · receipt ${line.receiptFileName}`
                            : ""}
                        </div>
                      ))}
                </div>
              )}
              {item.financiallyPosted && (
                <details className="mt-3 rounded-field border border-line p-3">
                  <summary className="cursor-pointer text-sm font-semibold">
                    Correct posted financials
                  </summary>
                  <div className="mt-2 grid gap-2 sm:grid-cols-4">
                    <Field label="Correct total">
                      <Input
                        inputMode="decimal"
                        value={correctionDraft.expenseAmount}
                        onChange={(e) =>
                          setCorrectionDraft((current) => ({
                            ...current,
                            expenseAmount: e.target.value,
                          }))
                        }
                      />
                    </Field>
                    <Field label="Correct owner charge">
                      <Input
                        inputMode="decimal"
                        value={correctionDraft.ownerCharge}
                        onChange={(e) =>
                          setCorrectionDraft((current) => ({
                            ...current,
                            ownerCharge: e.target.value,
                          }))
                        }
                      />
                    </Field>
                    <Field label="Manager fee">
                      <Input
                        inputMode="decimal"
                        value={correctionDraft.managerFee}
                        onChange={(e) =>
                          setCorrectionDraft((current) => ({
                            ...current,
                            managerFee: e.target.value,
                          }))
                        }
                      />
                    </Field>
                    <Field label="Correction reason">
                      <Input
                        value={correctionDraft.reason}
                        onChange={(e) =>
                          setCorrectionDraft((current) => ({
                            ...current,
                            reason: e.target.value,
                          }))
                        }
                      />
                    </Field>
                  </div>
                  <Button
                    className="mt-2"
                    type="button"
                    disabled={
                      busy ||
                      !correctionDraft.expenseAmount ||
                      !correctionDraft.ownerCharge ||
                      !correctionDraft.reason.trim()
                    }
                    onClick={() => void correctMaintenance(item)}
                  >
                    Post reversal and replacement
                  </Button>
                </details>
              )}
            </div>
          ))}
        </div>
      </Card>
    ) : null;
  const workOrderControls =
    tab === "work-orders" ? (
      <Card>
        <h2 className="text-lg font-semibold">
          Work-order financial lifecycle
        </h2>
        <p className="text-sm text-ink-muted">
          Compare bids, classify final costs, link owner approval, complete the
          work and post the source-linked expense exactly once.
        </p>
        <Field className="mt-3" label="Operational reason">
          <Input
            value={workOrderReason}
            onChange={(e) => setWorkOrderReason(e.target.value)}
          />
        </Field>
        <div className="mt-4 space-y-3">
          {workOrders.map((item) => (
            <div
              className="rounded-field border border-line p-3"
              id={`work-order-${item.id}`}
              key={item.id}
            >
              <div className="font-semibold">
                {item.workOrderNumber} · {item.scope}
              </div>
              <p className="m-0 text-sm text-ink-muted">
                {item.status} · final {item.currency}{" "}
                {item.finalAmount.toFixed(2)} · owner{" "}
                {item.ownerResponsibility.toFixed(2)} · PM{" "}
                {item.managerResponsibility.toFixed(2)} · vendor{" "}
                {item.vendorResponsibility.toFixed(2)} · posting{" "}
                {item.postingStatus}
              </p>
              <div className="mt-2 flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => void showWorkOrderDetails(item.id)}
                >
                  Costs, bids and receipts
                </Button>
                {item.postingStatus === "UNPOSTED" && (
                  <Button
                    type="button"
                    disabled={
                      busy || !costDraft.description || !costDraft.amount
                    }
                    onClick={() => void addWorkOrderCost(item)}
                  >
                    Add current cost line
                  </Button>
                )}
                <label className="inline-flex cursor-pointer items-center rounded-field border border-line px-3 py-2 text-sm">
                  <span>Upload receipt</span>
                  <input
                    className="sr-only"
                    type="file"
                    accept="application/pdf,image/jpeg,image/png"
                    onChange={(e) =>
                      uploadWorkOrderAttachment(item.id, e.target.files?.[0])
                    }
                  />
                </label>
                <Select
                  aria-label={`Work order state ${item.workOrderNumber}`}
                  value={item.status}
                  onChange={(e) => void updateWorkOrder(item, e.target.value)}
                >
                  <option>{item.status}</option>
                  {(workOrderNextStates[item.status] ?? []).map((status) => (
                      <option key={status}>{status}</option>
                    ))}
                </Select>
              </div>
              {selectedWorkOrderId === item.id && item.status === "QUOTING" && (
                <div className="mt-2 grid gap-2 rounded-field bg-shell p-3 sm:grid-cols-4">
                  <Field label="Vendor">
                    <Select
                      value={quoteVendorId}
                      onChange={(e) => setQuoteVendorId(e.target.value)}
                    >
                      <option value="">Select active vendor</option>
                      {portfolioVendors
                        .filter(
                          (vendor) => vendor.isActive && !vendor.isSuspended,
                        )
                        .map((vendor) => (
                          <option key={vendor.id} value={vendor.id}>
                            {vendor.name}
                          </option>
                        ))}
                    </Select>
                  </Field>
                  <Field label="Bid amount">
                    <Input
                      inputMode="decimal"
                      value={quoteAmount}
                      onChange={(e) => setQuoteAmount(e.target.value)}
                    />
                  </Field>
                  <Field label="Bid scope">
                    <Input
                      value={quoteScope}
                      onChange={(e) => setQuoteScope(e.target.value)}
                    />
                  </Field>
                  <Button
                    type="button"
                    disabled={
                      busy ||
                      !quoteVendorId ||
                      !quoteAmount ||
                      !quoteScope.trim()
                    }
                    onClick={() => void addWorkOrderQuote(item)}
                  >
                    Add vendor bid
                  </Button>
                </div>
              )}
              {selectedWorkOrderId === item.id &&
              item.postingStatus === "UNPOSTED" &&
              workOrderAttachments[item.id]?.length ? (
                <Field className="mt-2" label="Receipt for the next cost line">
                  <Select
                    value={costDraft.receiptAttachmentId}
                    onChange={(e) =>
                      setCostDraft((current) => ({
                        ...current,
                        receiptAttachmentId: e.target.value,
                      }))
                    }
                  >
                    <option value="">No receipt linked</option>
                    {workOrderAttachments[item.id].map((file) => (
                      <option key={file.id} value={file.id}>
                        {file.fileName}
                      </option>
                    ))}
                  </Select>
                </Field>
              ) : null}
              {workOrderQuotes[item.id]?.length ? (
                <div className="mt-2 space-y-1 rounded-field bg-shell p-2 text-xs">
                  {workOrderQuotes[item.id].map((quote) => (
                    <div
                      className="flex flex-wrap items-center justify-between gap-2"
                      key={quote.id}
                    >
                      <span>
                        {quote.scope} · {quote.currency}{" "}
                        {quote.amount.toFixed(2)} · {quote.status}
                      </span>
                      {item.status === "QUOTING" && (
                        <Button
                          type="button"
                          variant="outline"
                          onClick={() =>
                            void updateWorkOrder(
                              item,
                              "OWNER_APPROVAL",
                              quote.id,
                            )
                          }
                        >
                          Select quote
                        </Button>
                      )}
                    </div>
                  ))}
                </div>
              ) : null}
              {workOrderAttachments[item.id] && (
                <div className="mt-2 rounded-field bg-shell p-2 text-xs">
                  {workOrderAttachments[item.id].length === 0
                    ? "No receipts uploaded."
                    : workOrderAttachments[item.id].map((file) => (
                        <div key={file.id}>
                          {file.fileName} · {file.status}
                        </div>
                      ))}
                </div>
              )}
              {workOrderCostLines[item.id] && (
                <div className="mt-2 space-y-1 rounded-field bg-shell p-2 text-xs">
                  {workOrderCostLines[item.id].length === 0
                    ? "No final costs entered."
                    : workOrderCostLines[item.id].map((line) => (
                        <div key={line.id}>
                          {line.lineType} · {line.responsibility} ·{" "}
                          {line.description} · {line.currency}{" "}
                          {line.amount.toFixed(2)}
                          {line.receiptFileName
                            ? ` · receipt ${line.receiptFileName}`
                            : ""}
                        </div>
                      ))}
                </div>
              )}
            </div>
          ))}
          {workOrders.length === 0 && (
            <p className="text-sm text-ink-muted">
              No work orders. Failed inspections can create corrective work
              here.
            </p>
          )}
        </div>
      </Card>
    ) : null;
  const workOrderCorrectionControls =
    tab === "work-orders" &&
    workOrders.some((item) => item.postingStatus !== "UNPOSTED") ? (
      <Card className="mt-4">
        <h2 className="text-lg font-semibold">Work-order corrections</h2>
        <p className="text-sm text-ink-muted">
          Enter a corrected total and owner portion. The remainder is PM-company
          responsibility. The server reverses the active posting and creates a
          replacement.
        </p>
        <div className="mt-2 grid gap-2 sm:grid-cols-3">
          <Field label="Correct total">
            <Input
              inputMode="decimal"
              value={correctionDraft.expenseAmount}
              onChange={(e) =>
                setCorrectionDraft((current) => ({
                  ...current,
                  expenseAmount: e.target.value,
                }))
              }
            />
          </Field>
          <Field label="Owner portion">
            <Input
              inputMode="decimal"
              value={correctionDraft.ownerCharge}
              onChange={(e) =>
                setCorrectionDraft((current) => ({
                  ...current,
                  ownerCharge: e.target.value,
                }))
              }
            />
          </Field>
          <Field label="Correction reason">
            <Input
              value={correctionDraft.reason}
              onChange={(e) =>
                setCorrectionDraft((current) => ({
                  ...current,
                  reason: e.target.value,
                }))
              }
            />
          </Field>
        </div>
        <div className="mt-3 flex flex-wrap gap-2">
          {workOrders
            .filter((item) => item.postingStatus !== "UNPOSTED")
            .map((item) => (
              <Button
                type="button"
                variant="outline"
                key={item.id}
                disabled={
                  busy ||
                  !correctionDraft.expenseAmount ||
                  !correctionDraft.reason.trim()
                }
                onClick={() => void correctWorkOrder(item)}
              >
                Correct {item.workOrderNumber}
              </Button>
            ))}
        </div>
      </Card>
    ) : null;
  const inspectionCorrectiveControls =
    tab === "inspections" ? (
      <Card className="mb-4">
        <h2 className="text-lg font-semibold">
          Templates and corrective actions
        </h2>
        <div className="mt-3 grid gap-2 md:grid-cols-3">
          <Field label="Template name">
            <Input
              value={templateDraft.name}
              onChange={(e) =>
                setTemplateDraft((current) => ({
                  ...current,
                  name: e.target.value,
                }))
              }
            />
          </Field>
          <Field label="Template items JSON">
            <Input
              value={templateDraft.itemsJson}
              onChange={(e) =>
                setTemplateDraft((current) => ({
                  ...current,
                  itemsJson: e.target.value,
                }))
              }
            />
          </Field>
          <Field label="Active template">
            <Select
              value={selectedTemplateId}
              onChange={(e) => setSelectedTemplateId(e.target.value)}
            >
              <option value="">Custom/default checklist</option>
              {checklistTemplates
                .filter((item) => item.status === "ACTIVE")
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} v{item.version}
                  </option>
                ))}
            </Select>
          </Field>
        </div>
        <div className="mt-2 flex flex-wrap gap-2">
          <Button
            type="button"
            onClick={() => void createInspectionTemplate()}
            disabled={
              busy ||
              !templateDraft.name.trim() ||
              !templateDraft.itemsJson.trim()
            }
          >
            Create template version
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => void assignInspectionTemplate()}
            disabled={busy || !property || !selectedTemplateId}
          >
            Assign to selected property
          </Button>
        </div>
        <div className="mt-4 space-y-2">
          {correctiveActions.map((action) => (
            <div
              className="rounded-field border border-line p-3 text-sm"
              key={action.id}
            >
              <strong>
                {action.severity} · {action.description}
              </strong>
              <div>
                {action.status} · readiness{" "}
                {action.blocksReadiness ? "BLOCKED" : "warning only"}
                {action.workOrderId
                  ? ` · work order ${action.workOrderId}`
                  : ""}
                {action.retestInspectionId
                  ? ` · reinspection ${action.retestInspectionId}`
                  : ""}
              </div>
              <div className="mt-2 flex flex-wrap gap-2">
                {!action.workOrderId && (
                  <Button
                    type="button"
                    variant="outline"
                    disabled={busy || !inspectionWorkOrderScope.trim()}
                    onClick={() =>
                      void createInspectionWorkOrder(action.inspectionId)
                    }
                  >
                    Create corrective work order
                  </Button>
                )}
                {action.workOrderId && action.status === "IN_PROGRESS" && (
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() =>
                      void advanceCorrectiveAction(action, "RETEST_REQUIRED")
                    }
                  >
                    Request reinspection
                  </Button>
                )}
                {action.status === "RETEST_REQUIRED" && (
                  <Button
                    type="button"
                    disabled={busy || !form.inspectionAt}
                    onClick={() => void createReinspection(action)}
                  >
                    Schedule reinspection
                  </Button>
                )}
              </div>
            </div>
          ))}
          {correctiveActions.length === 0 && (
            <p className="text-sm text-ink-muted">
              No unresolved corrective actions.
            </p>
          )}
        </div>
      </Card>
    ) : null;
  const cleaningControls =
    tab === "cleaning" ? (
      <Card className="mb-4">
        <h2 className="text-lg font-semibold">
          Versioned readiness checklists
        </h2>
        <p className="text-sm text-ink-muted">
          Assign an active cleaning template to a property. Each new task keeps
          its own immutable template name, version and checklist snapshot.
        </p>
        <div className="mt-3 grid gap-2 md:grid-cols-3">
          <Field label="Template name">
            <Input
              value={cleaningTemplateDraft.name}
              onChange={(e) =>
                setCleaningTemplateDraft((current) => ({
                  ...current,
                  name: e.target.value,
                }))
              }
            />
          </Field>
          <Field label="Template items JSON">
            <Input
              value={cleaningTemplateDraft.itemsJson}
              onChange={(e) =>
                setCleaningTemplateDraft((current) => ({
                  ...current,
                  itemsJson: e.target.value,
                }))
              }
            />
          </Field>
          <Field label="Active cleaning template">
            <Select
              value={selectedCleaningTemplateId}
              onChange={(e) => setSelectedCleaningTemplateId(e.target.value)}
            >
              <option value="">Default checklist</option>
              {checklistTemplates
                .filter(
                  (item) =>
                    item.workflowType === "CLEANING" &&
                    item.status === "ACTIVE",
                )
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} v{item.version}
                  </option>
                ))}
            </Select>
          </Field>
        </div>
        <div className="mt-2 flex flex-wrap gap-2">
          <Button
            type="button"
            onClick={() => void createCleaningTemplate()}
            disabled={
              busy ||
              !cleaningTemplateDraft.name.trim() ||
              !cleaningTemplateDraft.itemsJson.trim()
            }
          >
            Create template version
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => void assignCleaningTemplate()}
            disabled={busy || !property || !selectedCleaningTemplateId}
          >
            Assign to selected property
          </Button>
        </div>
        <div className="mt-4 space-y-2">
          {cleaning.map((item) => (
            <div
              key={item.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-field border border-line p-3 text-sm"
            >
              <span>
                {new Date(item.dueAt).toLocaleString()} · {item.status} ·
                template {item.templateName || "DEFAULT"} v
                {item.templateVersion || 1}
              </span>
              <span className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={busy || item.status === "READY"}
                  onClick={() => void completeCleaningChecklist(item)}
                >
                  Complete required items
                </Button>
                {item.status !== "READY" && (
                  <Button
                    type="button"
                    variant="outline"
                    disabled={busy}
                    onClick={() =>
                      void run(
                        () =>
                          api.updateProfessionalCleaning(token, item.id, {
                            status: "READY",
                            checklistJson: item.checklistJson,
                            issues: item.issues,
                            photosJson: item.photosJson,
                            rowVersion: item.rowVersion,
                          }),
                        "Readiness marked ready",
                      )
                    }
                  >
                    Mark READY
                  </Button>
                )}
              </span>
            </div>
          ))}
        </div>
      </Card>
    ) : null;
  const dashboardCards = operationalDashboard ? (
    <Card className="mb-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold">Operations at a glance</h2>
          <p className="text-sm text-ink-muted">
            Live, permission-filtered portfolio KPIs. Select a tile to open the
            relevant workspace.
          </p>
        </div>
        <AppLink href="/pm/p0" className={buttonClassName("outline")}>
          Open financials
        </AppLink>
      </div>
      <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {(
          [
            ["Reservations", operationalDashboard.reservations, "reservations"],
            [
              "Occupancy",
              `${operationalDashboard.occupancyPercent}%`,
              "calendar",
            ],
            [
              "Open maintenance",
              operationalDashboard.openMaintenance,
              "maintenance",
            ],
            ["Work orders", operationalDashboard.openWorkOrders, "maintenance"],
            ["Not ready", operationalDashboard.notReady, "cleaning"],
            [
              "Inspections (30d)",
              operationalDashboard.upcomingInspections,
              "inspections",
            ],
            ["Open incidents", operationalDashboard.openIncidents, "incidents"],
            [
              "Pending approvals",
              operationalDashboard.pendingApprovals,
              "maintenance",
            ],
            [
              "Active vendors",
              operationalDashboard.activeVendors,
              "maintenance",
            ],
            [
              "Overdue actions",
              operationalDashboard.overdueActions,
              "maintenance",
            ],
          ] as Array<[string, string | number, Tab]>
        ).map(([label, value, target]) => (
          <button
            type="button"
            key={label}
            onClick={() => setTab(target)}
            className="rounded-field border border-line bg-shell p-3 text-left transition hover:border-ink focus-visible:outline-2 focus-visible:outline-offset-2"
          >
            <span className="block text-xs text-ink-muted">{label}</span>
            <strong className="mt-1 block text-2xl">{value}</strong>
          </button>
        ))}
      </div>
      <div className="mt-3 text-xs text-ink-muted">
        {operationalDashboard.occupiedNights} occupied nights of{" "}
        {operationalDashboard.portfolioNights} portfolio nights ·{" "}
        {operationalDashboard.anomalousUtilities} utility anomalies
      </div>
    </Card>
  ) : null;
  if (!auth.isAuthenticated)
    return (
      <Card>
        <p className="text-sm">
          Sign in as a Property Manager to open operations.
        </p>
        <AppLink href="/login" className={buttonClassName("dark")}>
          Sign in
        </AppLink>
      </Card>
    );
  return (
    <main className="page-shell" aria-label="Professional operations">
      <PageHeader
        eyebrow="Property Manager"
        title="Professional operations"
        copy="Reservations, owner blocks, maintenance and property controls backed by the live portfolio API."
        actions={
          <AppLink href="/pm/p0" className={buttonClassName("outline")}>
            Financial workspace
          </AppLink>
        }
      />
      {dashboardCards}
      {reservationActions}
      {maintenanceControls}
      {maintenanceFinancialControls}
      {workOrderControls}
      {workOrderCorrectionControls}
      {inspectionCorrectiveControls}
      {cleaningControls}
      <nav
        aria-label="Professional operations"
        className="mb-6 flex flex-wrap gap-2"
      >
        {tabs.map(([id, label]) => (
          <button
            key={id}
            type="button"
            className={
              id === tab ? buttonClassName("dark") : buttonClassName("outline")
            }
            onClick={() => setTab(id)}
            aria-current={id === tab ? "page" : undefined}
          >
            {label}
          </button>
        ))}
      </nav>
      <Card className="mb-6">
        <div className="grid gap-4 md:grid-cols-3">
          <Field label="Owner">
            <Select
              value={owner}
              onChange={(e) => {
                setOwner(e.target.value);
                const first = portfolioProperties.find(
                  (item) => item.ownerUserId === e.target.value,
                );
                if (first) setProperty(first.id);
              }}
            >
              <option value="">Select owner</option>
              {portfolioOwners.map((item) => (
                <option key={item.ownerUserId} value={item.ownerUserId}>
                  {item.displayName || item.email}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Property">
            <Select
              value={property}
              onChange={(e) => setProperty(e.target.value)}
            >
              <option value="">Select property</option>
              {portfolioProperties
                .filter((item) => !owner || item.ownerUserId === owner)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.title} · {item.unitNumber}
                  </option>
                ))}
            </Select>
          </Field>
          <div className="flex items-end">
            <Button
              type="button"
              variant="outline"
              onClick={() => void load()}
              disabled={loading}
            >
              Refresh portfolio
            </Button>
          </div>
        </div>
      </Card>
      {notice && (
        <div
          className="mb-4 rounded-field bg-mint-tint px-4 py-3 text-sm"
          role="status"
        >
          {notice}
        </div>
      )}
      <ErrorBox error={error} />
      {loading ? (
        <LoadingState label="Loading professional operations" />
      ) : (
        <>
          {tab === "reservations" && (
            <Card>
              <h2 className="text-lg font-semibold">Reservation workspace</h2>
              <p className="mb-4 text-sm text-ink-muted">
                Search the manager portfolio, inspect guest/payment context, and
                apply only server-validated status or date changes. Use the
                reasoned cancellation action above for cancellations.
              </p>
              <div className="grid gap-3 md:grid-cols-4">
                <Field label="Search">
                  <Input
                    value={reservationSearch}
                    onChange={(e) => setReservationSearch(e.target.value)}
                    placeholder="Guest, email, property"
                  />
                </Field>
                <Field label="Status">
                  <Select
                    value={reservationStatus}
                    onChange={(e) => setReservationStatus(e.target.value)}
                  >
                    <option value="ALL">All statuses</option>
                    <option value="PENDING_VERIFICATION">
                      Pending verification
                    </option>
                    <option value="APPROVED">Approved</option>
                    <option value="CONFIRMED">Confirmed</option>
                    <option value="CANCELLED">Cancelled</option>
                    <option value="REJECTED">Rejected</option>
                  </Select>
                </Field>
                <Field label="Owner">
                  <Select
                    value={reservationOwnerFilter}
                    onChange={(e) => setReservationOwnerFilter(e.target.value)}
                  >
                    <option value="">All owners</option>
                    {portfolioOwners.map((item) => (
                      <option key={item.ownerUserId} value={item.ownerUserId}>
                        {item.displayName}
                      </option>
                    ))}
                  </Select>
                </Field>
                <Field label="Property">
                  <Select
                    value={reservationPropertyFilter}
                    onChange={(e) =>
                      setReservationPropertyFilter(e.target.value)
                    }
                  >
                    <option value="">All properties</option>
                    {portfolioProperties.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.title} · {item.unitNumber}
                      </option>
                    ))}
                  </Select>
                </Field>
              </div>
              <Field className="mt-3" label="Internal note">
                <Input
                  value={reservationNote}
                  onChange={(e) => setReservationNote(e.target.value)}
                  placeholder="Add a note before selecting a reservation"
                />
              </Field>
              <div className="mt-4 overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr>
                      <th>Dates</th>
                      <th>Guest</th>
                      <th>Status</th>
                      <th>Payment</th>
                      <th>Total</th>
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {reservations.map((r) => (
                      <tr key={r.bookingId} className="border-t border-line">
                        <td>
                          {r.checkIn.slice(0, 10)} → {r.checkOut.slice(0, 10)}
                        </td>
                        <td>
                          <details>
                            <summary className="cursor-pointer">
                              {r.guestUserId}
                            </summary>
                            <div className="mt-1 text-xs text-ink-muted">
                              Host/owner: {r.hostUserId}
                              <br />
                              Property: {r.propertyId}
                              {r.notes.length ? (
                                <>
                                  <br />
                                  Notes:{" "}
                                  {r.notes.map((note) => note.body).join(" · ")}
                                </>
                              ) : null}
                            </div>
                          </details>
                        </td>
                        <td>{r.status}</td>
                        <td>{r.paymentStatus}</td>
                        <td>
                          {r.currency} {r.totalAmount.toFixed(2)}
                        </td>
                        <td>
                          <Select
                            aria-label={`Update reservation ${r.bookingId}`}
                            value={r.status}
                            onChange={(e) =>
                              void run(
                                () =>
                                  api.updateProfessionalReservation(
                                    token,
                                    r.bookingId,
                                    { status: e.target.value },
                                  ),
                                "Reservation updated",
                              )
                            }
                          >
                            <option value={r.status}>{r.status}</option>
                            <option value="APPROVED">APPROVED</option>
                            <option value="REJECTED">REJECTED</option>
                          </Select>
                          <button
                            className="ml-2 underline"
                            type="button"
                            disabled={!reservationNote.trim()}
                            onClick={() =>
                              void run(
                                () =>
                                  api.addProfessionalReservationNote(
                                    token,
                                    r.bookingId,
                                    { body: reservationNote },
                                  ),
                                "Reservation note added",
                              )
                            }
                          >
                            Add note
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {reservations.length === 0 && (
                  <p className="py-6 text-sm text-ink-muted">
                    No reservations in this portfolio.
                  </p>
                )}
              </div>
            </Card>
          )}
          {tab === "calendar" && (
            <Card>
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold">
                    Portfolio master calendar
                  </h2>
                  <p className="text-sm text-ink-muted">
                    Reservations, owner blocks, maintenance, work orders,
                    cleaning and inspections are consolidated with
                    server-calculated overlap warnings.
                  </p>
                </div>
                <div
                  className="flex flex-wrap gap-2"
                  role="group"
                  aria-label="Calendar range"
                >
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() =>
                      setCalendarAnchor((current) => {
                        const next = new Date(current);
                        if (calendarView === "month")
                          next.setMonth(next.getMonth() - 1);
                        else
                          next.setDate(
                            next.getDate() - (calendarView === "week" ? 7 : 1),
                          );
                        return next;
                      })
                    }
                  >
                    Previous
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setCalendarAnchor(new Date())}
                  >
                    Today
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() =>
                      setCalendarAnchor((current) => {
                        const next = new Date(current);
                        if (calendarView === "month")
                          next.setMonth(next.getMonth() + 1);
                        else
                          next.setDate(
                            next.getDate() + (calendarView === "week" ? 7 : 1),
                          );
                        return next;
                      })
                    }
                  >
                    Next
                  </Button>
                  <Button
                    type="button"
                    variant={calendarView === "day" ? "dark" : "outline"}
                    onClick={() => setCalendarView("day")}
                  >
                    Day
                  </Button>
                  <Button
                    type="button"
                    variant={calendarView === "week" ? "dark" : "outline"}
                    onClick={() => setCalendarView("week")}
                  >
                    Week
                  </Button>
                  <Button
                    type="button"
                    variant={calendarView === "month" ? "dark" : "outline"}
                    onClick={() => setCalendarView("month")}
                  >
                    Month
                  </Button>
                </div>
              </div>
              <p className="mt-2 text-sm font-semibold" aria-live="polite">
                {calendarRange.start.toLocaleDateString()} –{" "}
                {new Date(calendarRange.end.getTime() - 1).toLocaleDateString()}
              </p>
              <div className="mt-3 grid gap-3 sm:grid-cols-3">
                <Field label="Owner">
                  <Select
                    value={calendarOwnerFilter}
                    onChange={(e) => {
                      setCalendarOwnerFilter(e.target.value);
                      setCalendarPropertyFilter("");
                    }}
                  >
                    <option value="">All owners</option>
                    {portfolioOwners.map((item) => (
                      <option key={item.ownerUserId} value={item.ownerUserId}>
                        {item.displayName || item.email}
                      </option>
                    ))}
                  </Select>
                </Field>
                <Field label="Property">
                  <Select
                    value={calendarPropertyFilter}
                    onChange={(e) => setCalendarPropertyFilter(e.target.value)}
                  >
                    <option value="">All properties</option>
                    {portfolioProperties
                      .filter(
                        (item) =>
                          !calendarOwnerFilter ||
                          item.ownerUserId === calendarOwnerFilter,
                      )
                      .map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.title} · {item.unitNumber}
                        </option>
                      ))}
                  </Select>
                </Field>
                <Field label="Event type">
                  <Select
                    value={calendarTypeFilter}
                    onChange={(e) => setCalendarTypeFilter(e.target.value)}
                  >
                    <option>ALL</option>
                    <option>RESERVATION</option>
                    <option>OWNER_BLOCK</option>
                    <option>MAINTENANCE</option>
                    <option>WORK_ORDER</option>
                    <option>CLEANING</option>
                    <option>INSPECTION</option>
                    <option>OUT_OF_SERVICE</option>
                    <option>IMPORTED_UNAVAILABLE</option>
                  </Select>
                </Field>
              </div>
              <CalendarGrid
                items={calendar}
                view={calendarView}
                start={calendarRange.start}
                days={calendarRange.days}
              />
              <div className="mt-4 space-y-2">
                {calendar.map((item) => (
                  <div
                    key={`${item.type}-${item.sourceId}`}
                    className={
                      item.conflictLevel === "BLOCKING"
                        ? "rounded-field border border-coral-text bg-coral-tint p-3 text-sm"
                        : item.conflictLevel === "WARNING"
                          ? "rounded-field border border-sun-deep bg-sun-wash p-3 text-sm"
                          : "rounded-field border border-line p-3 text-sm"
                    }
                  >
                    <div>
                      <span className="font-semibold">{item.type}</span> ·{" "}
                      {item.title}
                      <span className="ml-2 text-ink-muted">
                        {new Date(item.startsAt).toLocaleString()} –{" "}
                        {new Date(item.endsAt).toLocaleString()}
                      </span>
                      <span className="ml-2">{item.status}</span>
                      {item.conflictLevel !== "NONE" && (
                        <span className="ml-2 font-semibold">
                          {item.conflictLevel}
                        </span>
                      )}
                      {item.relatedPath && (
                        <AppLink
                          className="ml-3 underline"
                          href={item.relatedPath}
                        >
                          Open related record
                        </AppLink>
                      )}
                    </div>
                    {item.conflicts?.length > 0 && (
                      <details className="mt-2">
                        <summary className="cursor-pointer font-semibold">
                          Explain {item.conflicts.length} conflict
                          {item.conflicts.length === 1 ? "" : "s"}
                        </summary>
                        {item.conflicts.map((conflict) => (
                          <p
                            className="m-0 mt-1 text-xs"
                            key={`${conflict.type}-${conflict.sourceId}`}
                          >
                            {conflict.level}: {conflict.explanation}
                          </p>
                        ))}
                      </details>
                    )}
                  </div>
                ))}
                {calendar.length === 0 && (
                  <p className="text-sm text-ink-muted">
                    No events in the selected range and filters.
                  </p>
                )}
              </div>
            </Card>
          )}
          {tab === "blocks" && (
            <Card>
              <h2 className="text-lg font-semibold">
                Owner blocks and conflicts
              </h2>
              <div className="mt-4 grid gap-3 md:grid-cols-3">
                <Field label="Starts">
                  <Input
                    type="datetime-local"
                    value={form.startsAt}
                    onChange={(e) =>
                      setForm({ ...form, startsAt: e.target.value })
                    }
                  />
                </Field>
                <Field label="Ends">
                  <Input
                    type="datetime-local"
                    value={form.endsAt}
                    onChange={(e) =>
                      setForm({ ...form, endsAt: e.target.value })
                    }
                  />
                </Field>
                <Field label="Category">
                  <Select
                    value={form.category}
                    onChange={(e) =>
                      setForm({ ...form, category: e.target.value })
                    }
                  >
                    <option>OWNER_STAY</option>
                    <option>MAINTENANCE</option>
                    <option>PERSONAL</option>
                    <option>OTHER</option>
                  </Select>
                </Field>
                <Field label="Reason">
                  <Input
                    value={form.reason}
                    onChange={(e) =>
                      setForm({ ...form, reason: e.target.value })
                    }
                  />
                </Field>
                <Field label="Notes">
                  <Input
                    value={form.notes}
                    onChange={(e) =>
                      setForm({ ...form, notes: e.target.value })
                    }
                  />
                </Field>
              </div>
              <Button
                className="mt-4"
                onClick={createBlock}
                disabled={
                  busy ||
                  !owner ||
                  !property ||
                  !form.startsAt ||
                  !form.endsAt ||
                  !form.reason.trim()
                }
              >
                Create owner block
              </Button>
              <div className="mt-6 space-y-2">
                {blocks.map((b) => (
                  <div
                    key={b.id}
                    className="rounded-field border border-line p-3 text-sm"
                  >
                    <strong>{b.category}</strong> ·{" "}
                    {new Date(b.startsAt).toLocaleString()} –{" "}
                    {new Date(b.endsAt).toLocaleString()} · {b.reason} ·{" "}
                    {b.status}
                    {b.notes && (
                      <span className="ml-2 text-ink-muted">{b.notes}</span>
                    )}
                    {b.status === "ACTIVE" && (
                      <button
                        className="ml-3 underline"
                        type="button"
                        onClick={() =>
                          void run(
                            () =>
                              api.cancelProfessionalOwnerBlock(token, b.id, {
                                reason: "Cancelled by manager",
                                rowVersion: b.rowVersion,
                              }),
                            "Owner block cancelled",
                          )
                        }
                      >
                        Cancel
                      </button>
                    )}
                  </div>
                ))}
              </div>
            </Card>
          )}
          {tab === "maintenance" && (
            <Card>
              <h2 className="text-lg font-semibold">Maintenance lifecycle</h2>
              <p className="text-sm text-ink-muted">
                REQUESTED → TRIAGED → QUOTING → OWNER_APPROVAL → ASSIGNED →
                SCHEDULED → IN_PROGRESS → COMPLETED → CLOSED.
              </p>
              <div className="mt-4 grid gap-3 md:grid-cols-2">
                <Field label="Title">
                  <Input
                    value={form.title}
                    onChange={(e) =>
                      setForm({ ...form, title: e.target.value })
                    }
                  />
                </Field>
                <Field label="Description">
                  <Input
                    value={form.description}
                    onChange={(e) =>
                      setForm({ ...form, description: e.target.value })
                    }
                  />
                </Field>
              </div>
              <Button
                className="mt-4"
                onClick={createMaintenance}
                disabled={busy || !owner || !property || !form.title}
              >
                Create case
              </Button>
              <div className="mt-6 space-y-3">
                {maintenance.map((m) => (
                  <div
                    key={m.id}
                    className="rounded-field border border-line p-3"
                  >
                    <div className="font-semibold">
                      {m.number} · {m.title}
                    </div>
                    <div className="text-sm text-ink-muted">
                      {m.status} · {m.priority} · owner charge {m.currency}{" "}
                      {m.ownerCharge.toFixed(2)}
                    </div>
                    <Select
                      aria-label={`Maintenance state ${m.number}`}
                      value={m.status}
                      onChange={(e) =>
                        void run(
                          () =>
                            api.transitionProfessionalMaintenance(token, m.id, {
                              status: e.target.value,
                              rowVersion: m.rowVersion,
                              details:
                                workOrderReason.trim() || "Operational update",
                              ownerApprovalId: ownerApprovalId || undefined,
                            }),
                          "Maintenance state updated",
                        )
                      }
                    >
                      <option>{m.status}</option>
                      {(maintenanceNextStates[m.status] ?? []).map((s) => (
                        <option key={s}>{s}</option>
                      ))}
                    </Select>
                    <div className="mt-2 flex flex-wrap gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => void showMaintenanceHistory(m.id)}
                      >
                        View timeline
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => void showMaintenanceQuotes(m.id)}
                      >
                        Compare quotes
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => void showMaintenanceAttachments(m.id)}
                      >
                        View evidence
                      </Button>
                      <label className="inline-flex cursor-pointer items-center rounded-field border border-line px-3 py-2 text-sm">
                        <span>Upload receipt/photo</span>
                        <input
                          className="sr-only"
                          type="file"
                          accept="application/pdf,image/jpeg,image/png"
                          onChange={(e) =>
                            uploadMaintenanceAttachment(
                              m.id,
                              e.target.files?.[0],
                            )
                          }
                        />
                      </label>
                      {m.status === "QUOTING" && (
                        <Button
                          type="button"
                          variant="outline"
                          onClick={() => setSelectedMaintenanceId(m.id)}
                        >
                          Add quote
                        </Button>
                      )}
                    </div>
                    {selectedMaintenanceId === m.id &&
                      m.status === "QUOTING" && (
                        <div className="mt-2 grid gap-2 rounded-field bg-shell p-3 sm:grid-cols-4">
                          <Input
                            aria-label="Quote vendor id"
                            placeholder="Vendor UUID"
                            value={quoteVendorId}
                            onChange={(e) => setQuoteVendorId(e.target.value)}
                          />
                          <Input
                            aria-label="Quote amount"
                            inputMode="decimal"
                            placeholder="Amount"
                            value={quoteAmount}
                            onChange={(e) => setQuoteAmount(e.target.value)}
                          />
                          <Input
                            aria-label="Quote scope"
                            placeholder="Scope"
                            value={quoteScope}
                            onChange={(e) => setQuoteScope(e.target.value)}
                          />
                          <Button
                            type="button"
                            disabled={
                              busy ||
                              !quoteVendorId ||
                              !quoteAmount ||
                              !quoteScope
                            }
                            onClick={() => void addQuote()}
                          >
                            Save quote
                          </Button>
                        </div>
                      )}
                    {maintenanceQuotes[m.id] && (
                      <div className="mt-2 grid gap-2 rounded-field border border-line bg-shell p-2 text-xs">
                        {maintenanceQuotes[m.id].length === 0 ? (
                          <span>No quotes received yet.</span>
                        ) : (
                          maintenanceQuotes[m.id].map((quote) => (
                            <div
                              className="flex flex-wrap items-center justify-between gap-2"
                              key={quote.id}
                            >
                              <span>
                                Vendor {quote.vendorId.slice(0, 8)} ·{" "}
                                {quote.scope} · {quote.currency}{" "}
                                {quote.amount.toFixed(2)}
                                {quote.expiresAt
                                  ? ` · expires ${new Date(quote.expiresAt).toLocaleDateString()}`
                                  : ""}
                              </span>
                              {m.status === "QUOTING" && (
                                <Button
                                  type="button"
                                  variant="outline"
                                  disabled={busy}
                                  onClick={() =>
                                    void selectMaintenanceQuote(m.id, quote)
                                  }
                                >
                                  Select
                                </Button>
                              )}
                            </div>
                          ))
                        )}
                      </div>
                    )}
                    {maintenanceHistory[m.id] && (
                      <div className="mt-2 rounded-field border border-line bg-shell p-2 text-xs">
                        {maintenanceHistory[m.id].map((event) => (
                          <div key={event.id}>
                            {event.eventType}: {event.fromStatus || "—"} →{" "}
                            {event.toStatus} · {event.details}
                          </div>
                        ))}
                      </div>
                    )}
                    {maintenanceAttachments[m.id] && (
                      <div
                        className="mt-2 rounded-field border border-line bg-shell p-2 text-xs"
                        aria-label="Maintenance evidence"
                      >
                        {maintenanceAttachments[m.id].length === 0 ? (
                          <span>No evidence uploaded yet.</span>
                        ) : (
                          maintenanceAttachments[m.id].map((file) => (
                            <div
                              className="flex flex-wrap items-center justify-between gap-2"
                              key={file.id}
                            >
                              <span>
                                {file.fileName} · {file.status} ·{" "}
                                {new Date(file.createdAt).toLocaleString()}
                              </span>
                              <Button
                                type="button"
                                variant="outline"
                                onClick={() =>
                                  void downloadMaintenanceAttachment(
                                    m.id,
                                    file.id,
                                  )
                                }
                              >
                                Open
                              </Button>
                            </div>
                          ))
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </Card>
          )}
          {tab === "cleaning" && (
            <Card>
              <h2 className="text-lg font-semibold">Cleaning and readiness</h2>
              <Field label="Due at">
                <Input
                  type="datetime-local"
                  value={form.dueAt}
                  onChange={(e) => setForm({ ...form, dueAt: e.target.value })}
                />
              </Field>
              <Button
                className="mt-4"
                onClick={createCleaning}
                disabled={busy || !property || !form.dueAt}
              >
                Create readiness task
              </Button>
              <div className="mt-6 space-y-2">
                {cleaning.map((c) => (
                  <div
                    key={c.id}
                    className="rounded-field border border-line p-3 text-sm"
                  >
                    <div>
                      {new Date(c.dueAt).toLocaleString()} · {c.status} ·
                      checklist {c.checklistJson}
                    </div>
                    <Select
                      aria-label="Readiness status"
                      value={c.status}
                      onChange={(e) =>
                        void run(
                          () =>
                            api.updateProfessionalCleaning(token, c.id, {
                              status: e.target.value,
                              checklistJson: c.checklistJson,
                              issues: c.issues,
                              photosJson: c.photosJson,
                              rowVersion: c.rowVersion,
                            }),
                          "Readiness status updated",
                        )
                      }
                    >
                      <option>NOT_READY</option>
                      <option>READY</option>
                    </Select>
                  </div>
                ))}
              </div>
            </Card>
          )}
          {tab === "inspections" && (
            <Card>
              <h2 className="text-lg font-semibold">Inspections</h2>
              <p className="text-sm text-ink-muted">
                Schedule, capture findings, complete required checks, create
                corrective work, reinspect and sign off.
              </p>
              <div className="grid gap-3 md:grid-cols-2">
                <Field label="Scheduled at">
                  <Input
                    type="datetime-local"
                    value={form.inspectionAt}
                    onChange={(e) =>
                      setForm({ ...form, inspectionAt: e.target.value })
                    }
                  />
                </Field>
                <Field label="Failed finding / corrective scope">
                  <Input
                    value={inspectionFinding}
                    onChange={(e) => {
                      setInspectionFinding(e.target.value);
                      setInspectionWorkOrderScope(e.target.value);
                    }}
                    placeholder="Describe the failed item"
                  />
                </Field>
              </div>
              <Button
                className="mt-4"
                onClick={createInspection}
                disabled={busy || !property || !form.inspectionAt}
              >
                Schedule inspection
              </Button>
              <div className="mt-6 space-y-2">
                {inspections.map((i) => (
                  <div
                    key={i.id}
                    className="rounded-field border border-line p-3 text-sm"
                  >
                    <div>
                      {i.inspectionType} ·{" "}
                      {new Date(i.scheduledAt).toLocaleString()} · {i.status} ·{" "}
                      {i.templateName ?? "CUSTOM"} v{i.templateVersion ?? 1}
                    </div>
                    <Select
                      aria-label={`Inspection status ${i.id}`}
                      value={i.status}
                      onChange={(e) =>
                        void run(
                          () =>
                            api.updateProfessionalInspection(token, i.id, {
                              status: e.target.value,
                              evidenceJson: i.evidenceJson,
                              findingsJson: i.findingsJson,
                              checklistJson: i.checklistJson,
                              rowVersion: i.rowVersion,
                            }),
                          "Inspection status updated",
                        )
                      }
                    >
                      <option>SCHEDULED</option>
                      <option>IN_PROGRESS</option>
                      <option>FAILED</option>
                      <option>SIGNED_OFF</option>
                      <option>CANCELLED</option>
                    </Select>
                    <Button
                      className="ml-2"
                      type="button"
                      variant="outline"
                      disabled={
                        busy ||
                        !inspectionFinding.trim() ||
                        i.status === "SIGNED_OFF" ||
                        i.status === "CANCELLED"
                      }
                      onClick={() => void failInspection(i)}
                    >
                      Record failed item
                    </Button>
                    {(() => {
                      try {
                        const checklist = JSON.parse(i.checklistJson) as Array<
                          Record<string, unknown>
                        >;
                        return (
                          checklist.length > 0 && (
                            <div
                              className="mt-2 space-y-1 rounded-field bg-shell p-2"
                              aria-label={`Inspection checklist ${i.id}`}
                            >
                              {checklist.map((entry, index) => (
                                <label
                                  className="flex items-center gap-2 text-xs"
                                  key={String(entry.id ?? index)}
                                >
                                  <input
                                    type="checkbox"
                                    checked={entry.completed === true}
                                    onChange={(e) =>
                                      void toggleInspectionChecklist(
                                        i,
                                        index,
                                        e.target.checked,
                                      )
                                    }
                                    disabled={
                                      busy ||
                                      i.status === "SIGNED_OFF" ||
                                      i.status === "CANCELLED"
                                    }
                                  />
                                  <span>
                                    {String(
                                      entry.label ??
                                        entry.id ??
                                        `Item ${index + 1}`,
                                    )}
                                    {entry.required === true
                                      ? " (required)"
                                      : ""}
                                  </span>
                                </label>
                              ))}
                            </div>
                          )
                        );
                        return (
                          <span className="mt-2 block text-xs text-coral-text">
                            Checklist data is invalid.
                          </span>
                        );
                      } catch {
                        return (
                          <span className="mt-2 block text-xs text-coral-text">
                            Checklist data is invalid.
                          </span>
                        );
                      }
                    })()}
                    {(i.status === "SIGNED_OFF" || i.status === "FAILED") && (
                      <div className="mt-2 flex flex-wrap gap-2">
                        {i.correctiveWorkOrderId ? (
                          <span className="text-xs text-ink-muted">
                            Corrective work order {i.correctiveWorkOrderId}
                          </span>
                        ) : (
                          <>
                            <Input
                              aria-label={`Corrective scope ${i.id}`}
                              placeholder="Corrective work scope"
                              value={inspectionWorkOrderScope}
                              onChange={(e) =>
                                setInspectionWorkOrderScope(e.target.value)
                              }
                            />
                            <Button
                              type="button"
                              variant="outline"
                              disabled={
                                busy || !inspectionWorkOrderScope.trim()
                              }
                              onClick={() =>
                                void createInspectionWorkOrder(i.id)
                              }
                            >
                              Create work order
                            </Button>
                          </>
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </Card>
          )}
          {tab === "assets" && (
            <Card>
              <h2 className="text-lg font-semibold">Assets and inventory</h2>
              <p className="text-sm text-ink-muted">
                Track equipment, consumables and evidence without overwriting
                historical records.
              </p>
              <div className="grid gap-3 md:grid-cols-3">
                <Field label="Asset tag">
                  <Input
                    value={form.assetTag}
                    onChange={(e) =>
                      setForm({ ...form, assetTag: e.target.value })
                    }
                  />
                </Field>
                <Field label="Name">
                  <Input
                    value={form.assetName}
                    onChange={(e) =>
                      setForm({ ...form, assetName: e.target.value })
                    }
                  />
                </Field>
                <Field label="Category">
                  <Input
                    value={form.assetCategory}
                    onChange={(e) =>
                      setForm({ ...form, assetCategory: e.target.value })
                    }
                  />
                </Field>
                <Field label="Description">
                  <Input
                    value={form.assetDescription}
                    onChange={(e) =>
                      setForm({ ...form, assetDescription: e.target.value })
                    }
                  />
                </Field>
                <Field label="Serial / reference">
                  <Input
                    value={form.assetSerial}
                    onChange={(e) =>
                      setForm({ ...form, assetSerial: e.target.value })
                    }
                  />
                </Field>
                <Field label="Condition">
                  <Select
                    value={form.assetCondition}
                    onChange={(e) =>
                      setForm({ ...form, assetCondition: e.target.value })
                    }
                  >
                    <option>GOOD</option>
                    <option>FAIR</option>
                    <option>POOR</option>
                    <option>DAMAGED</option>
                  </Select>
                </Field>
                <Field label="Purchase date">
                  <Input
                    type="date"
                    value={form.assetPurchaseDate}
                    onChange={(e) =>
                      setForm({ ...form, assetPurchaseDate: e.target.value })
                    }
                  />
                </Field>
                <Field label="Purchase cost">
                  <Input
                    inputMode="decimal"
                    value={form.assetPurchaseCost}
                    onChange={(e) =>
                      setForm({ ...form, assetPurchaseCost: e.target.value })
                    }
                  />
                </Field>
                <Field label="Warranty expiry">
                  <Input
                    type="date"
                    value={form.assetWarrantyExpiry}
                    onChange={(e) =>
                      setForm({ ...form, assetWarrantyExpiry: e.target.value })
                    }
                  />
                </Field>
              </div>
              <Button
                className="mt-4"
                onClick={createAsset}
                disabled={
                  busy || !property || !form.assetTag || !form.assetName
                }
              >
                Add asset
              </Button>
              <div className="mt-6 space-y-2">
                {assets.map((a) => (
                  <div
                    key={a.id}
                    className="rounded-field border border-line p-3 text-sm"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span>
                        <strong>{a.assetTag}</strong> · {a.name} · {a.status} ·
                        qty {a.quantity}
                      </span>
                      {a.status !== "RETIRED" && (
                        <Button
                          variant="outline"
                          type="button"
                          onClick={() =>
                            void run(
                              () =>
                                api.updateProfessionalAsset(token, a.id, {
                                  status: "RETIRED",
                                  quantity: a.quantity,
                                  location: a.location,
                                  metadataJson: a.metadataJson,
                                  photosJson: a.photosJson,
                                  rowVersion: a.rowVersion,
                                }),
                              "Asset retired",
                            )
                          }
                        >
                          Retire
                        </Button>
                      )}
                    </div>
                    <p className="m-0 mt-1 text-xs text-ink-muted">
                      {a.description || "No description"} · {a.condition} ·{" "}
                      {a.serialReference || "No serial/reference"}
                      {a.purchaseCost != null
                        ? ` · ${a.purchaseCost.toFixed(2)} purchase cost`
                        : ""}
                      {a.warrantyExpiry
                        ? ` · warranty ${a.warrantyExpiry}`
                        : ""}
                    </p>
                  </div>
                ))}
              </div>
            </Card>
          )}
          {tab === "incidents" && (
            <Card>
              <h2 className="text-lg font-semibold">Incident tracking</h2>
              <Field label="Description">
                <Input
                  value={form.incidentDescription}
                  onChange={(e) =>
                    setForm({ ...form, incidentDescription: e.target.value })
                  }
                />
              </Field>
              <Button
                className="mt-4"
                onClick={createIncident}
                disabled={busy || !property || !form.incidentDescription}
              >
                Log incident
              </Button>
              <div className="mt-6 space-y-2">
                {incidents.map((i) => (
                  <div
                    key={i.id}
                    className="rounded-field border border-line p-3 text-sm"
                  >
                    <div>
                      {i.severity} · {i.status} · {i.description}
                    </div>
                    <Select
                      aria-label="Incident status"
                      value={i.status}
                      onChange={(e) =>
                        void run(
                          () =>
                            api.updateProfessionalIncident(token, i.id, {
                              status: e.target.value,
                              actionTaken: i.actionTaken,
                              followUp: i.followUp,
                              insuranceReference:
                                i.insuranceReference ?? undefined,
                              rowVersion: i.rowVersion,
                            }),
                          "Incident status updated",
                        )
                      }
                    >
                      <option>OPEN</option>
                      <option>RESOLVED</option>
                      <option>CLOSED</option>
                    </Select>
                  </div>
                ))}
              </div>
            </Card>
          )}
        </>
      )}
    </main>
  );
}

export default PropertyManagerProfessionalPage;
