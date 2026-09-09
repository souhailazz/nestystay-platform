import type { AuthController } from "../../hooks/useAuth";
import { HostAnalytics } from "./HostAnalytics";
import { HostPropertiesList } from "./HostPropertiesList";
import { HostPropertyWizard } from "./HostPropertyWizard";
import { HostPropertyEditor } from "./HostPropertyEditor";
import { HostPricingPromotions } from "./HostPricingPromotions";
import { HostReservations } from "./HostReservations";
import { HostReportsExports } from "./HostReportsExports";
import { HostReviewsBadgesSettings } from "./HostReviewsBadgesSettings";

interface HostStateContainerProps {
  view: string;
  auth: AuthController;
  propertyId?: string;
}

export function HostStateContainer({ view, auth, propertyId }: HostStateContainerProps) {
  const token = auth.session?.accessToken || "";

  if (view === "analytics" || view === "dashboard" || view === "metrics") {
    return <HostAnalytics token={token} hostUserId={auth.session?.userId || ""} />;
  }

  if (view === "properties" || view === "archived") {
    return <HostPropertiesList view={view} token={token} />;
  }

  if (view === "properties-new" || view === "wizard" || view === "new") {
    return (
      <HostPropertyWizard
        token={token}
        hostUserId={auth.session?.userId || ""}
        hostName={auth.session?.displayName || ""}
        hostEmail={auth.session?.email || ""}
        onFinished={() => window.location.href = "/host/properties"}
      />
    );
  }

  if (view === "properties-edit" || view === "edit") {
    return <HostPropertyEditor token={token} propertyId={propertyId} />;
  }

  if (view === "pricing" || view === "promotions" || view === "discounts") {
    return <HostPricingPromotions view={view} token={token} />;
  }

  if (view.includes("reservation") || view === "bookings") {
    return <HostReservations token={token} />;
  }

  if (view === "reports" || view === "exports" || view === "statements") {
    return <HostReportsExports view={view} token={token} />;
  }

  if (view === "reviews" || view === "badges" || view === "settings") {
    return <HostReviewsBadgesSettings view={view} token={token} hostUserId={auth.session?.userId || ""} />;
  }

  return <HostAnalytics token={token} hostUserId={auth.session?.userId || ""} />;
}
