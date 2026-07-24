import type { AuthController } from "../../hooks/useAuth";
import { AdminOverview } from "./AdminOverview";
import { AdminUsers } from "./AdminUsers";
import { AdminProperties } from "./AdminProperties";
import { AdminFinancials } from "./AdminFinancials";
import { AdminAuditSystemHealth } from "./AdminAuditSystemHealth";
import { AdminPricebookCampaigns } from "./AdminPricebookCampaigns";

interface AdminStateContainerProps {
  view: string;
  auth: AuthController;
}

export function AdminStateContainer({ view, auth }: AdminStateContainerProps) {
  const token = auth.session?.accessToken || "";

  if (view === "overview" || view === "dashboard" || view === "metrics") {
    return <AdminOverview token={token} />;
  }

  if (view === "users" || view === "roles" || view === "accounts") {
    return <AdminUsers view={view} token={token} />;
  }

  if (view === "properties" || view === "moderation" || view === "review") {
    return <AdminProperties view={view} token={token} />;
  }

  if (view === "financials" || view === "refunds" || view === "ledgers") {
    return <AdminFinancials view={view} token={token} />;
  }

  if (view === "audit" || view === "health" || view === "logs") {
    return <AdminAuditSystemHealth view={view} token={token} />;
  }

  if (view === "pricebook" || view === "campaigns" || view === "fees") {
    return <AdminPricebookCampaigns view={view} token={token} />;
  }

  return <AdminOverview token={token} />;
}
