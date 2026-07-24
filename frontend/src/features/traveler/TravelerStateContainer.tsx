import type { AuthController } from "../../hooks/useAuth";
import { TravelerDashboard } from "./TravelerDashboard";
import { TravelerReservations } from "./TravelerReservations";
import { TravelerWishlists } from "./TravelerWishlists";
import { TravelerPaymentMethods } from "./TravelerPaymentMethods";
import { TravelerPaymentHistory } from "./TravelerPaymentHistory";
import { TravelerProfileIdentity } from "./TravelerProfileIdentity";
import { TravelerReviewsNotifications } from "./TravelerReviewsNotifications";

interface TravelerStateContainerProps {
  view: string;
  auth: AuthController;
}

export function TravelerStateContainer({ view, auth }: TravelerStateContainerProps) {
  const userId = auth.session?.userId || "guest_user_1";
  const token = auth.session?.accessToken || "";

  if (view === "dashboard" || view === "suggestions") {
    return <TravelerDashboard userId={userId} token={token} />;
  }

  if (view.startsWith("reservation")) {
    return <TravelerReservations view={view} token={token} />;
  }

  if (view === "wishlist" || view === "collections" || view === "favorites") {
    return <TravelerWishlists userId={userId} token={token} />;
  }

  if (view === "payment-methods") {
    return <TravelerPaymentMethods userId={userId} token={token} />;
  }

  if (view === "payment-history" || view === "invoices") {
    return <TravelerPaymentHistory token={token} />;
  }

  if (view === "profile" || view === "preferences" || view === "identity") {
    return <TravelerProfileIdentity userId={userId} token={token} />;
  }

  if (view.includes("review") || view === "notifications") {
    return <TravelerReviewsNotifications view={view} userId={userId} token={token} />;
  }

  return <TravelerDashboard userId={userId} token={token} />;
}
