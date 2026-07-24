import type { AuthController } from "../../hooks/useAuth";
import { MessagingCenter } from "./MessagingCenter";

interface MessagingStateContainerProps {
  auth: AuthController;
}

export function MessagingStateContainer({ auth }: MessagingStateContainerProps) {
  const token = auth.session?.accessToken || "";
  return <MessagingCenter token={token} />;
}
