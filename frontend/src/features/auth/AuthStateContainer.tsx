import type { AuthController } from "../../hooks/useAuth";
import { AuthModalSuite } from "./AuthModalSuite";

interface AuthStateContainerProps {
  mode?: "login" | "register" | "forgot-password";
  auth: AuthController;
}

export function AuthStateContainer({ mode = "login", auth }: AuthStateContainerProps) {
  return <AuthModalSuite initialMode={mode} auth={auth} />;
}
