import type { AuthController } from "../../hooks/useAuth";
import { HostProfileDirectory } from "./HostProfileDirectory";
import { HostProfileDetailPage } from "./HostProfileDetailPage";
import { HostProfileEditPage } from "./HostProfileEditPage";

interface HostProfileStateContainerProps {
  view: string;
  profileId?: string;
  auth: AuthController;
}

export function HostProfileStateContainer({ view, profileId, auth }: HostProfileStateContainerProps) {
  const token = auth.session?.accessToken || "";

  if (view === "directory" || view === "list") {
    return <HostProfileDirectory token={token} />;
  }

  if (view === "detail" || view === "profile-detail") {
    return <HostProfileDetailPage profileId={profileId} token={token} />;
  }

  if (view === "edit" || view === "settings") {
    return <HostProfileEditPage token={token} />;
  }

  return <HostProfileDirectory token={token} />;
}
