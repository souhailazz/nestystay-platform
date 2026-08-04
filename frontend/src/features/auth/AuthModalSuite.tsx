import { useState } from "react";
import { X } from "lucide-react";
import { api } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthModalMode } from "./types";
import type { AuthController } from "../../hooks/useAuth";

interface AuthModalSuiteProps {
  initialMode?: AuthModalMode;
  auth: AuthController;
  onClose?: () => void;
}

export function AuthModalSuite({ initialMode = "login", auth, onClose }: AuthModalSuiteProps) {
  const [mode, setMode] = useState<AuthModalMode>(initialMode);
  const [email, setEmail] = useState("guest@nestystay.local");
  const [password, setPassword] = useState("Password123!");
  const [registerDisplayName, setRegisterDisplayName] = useState("Nesty Guest");
  const [registerPhone, setRegisterPhone] = useState("+18765550123");
  const [registerConfirmPassword, setRegisterConfirmPassword] = useState("Password123!");
  const [registerRole, setRegisterRole] = useState<"Guest" | "Host">("Guest");
  const [acceptedTerms, setAcceptedTerms] = useState(true);
  const [acceptedPrivacy, setAcceptedPrivacy] = useState(true);
  const [otpCode, setOtpCode] = useState("");
  const [totpCode, setTotpCode] = useState("");
  const [totpQrUrl, setTotpQrUrl] = useState<string | null>(null);
  const [enrollmentId, setEnrollmentId] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleLogin(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setNotice(null);
    try {
      const result = await auth.login(email, password);
      if ("challengeId" in result) {
        setMode("2fa-verify");
        setNotice("Enter your authenticator code to finish signing in.");
        return;
      }

      setNotice("Logged in successfully!");
      if (onClose) onClose();
    } catch (err) {
      setNotice(`Login failed: ${err instanceof Error ? err.message : "Error"}`);
    } finally {
      setLoading(false);
    }
  }

  async function handleRegister(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setNotice(null);

    if (password !== registerConfirmPassword) {
      setNotice("Passwords must match.");
      setLoading(false);
      return;
    }

    if (!acceptedTerms || !acceptedPrivacy) {
      setNotice("Accept the terms and privacy policy to create an account.");
      setLoading(false);
      return;
    }

    try {
      await auth.register({
        email,
        password,
        displayName: registerDisplayName,
        phone: registerPhone,
        confirmPassword: registerConfirmPassword,
        acceptedTerms,
        acceptedPrivacy,
        role: registerRole,
      });
      setMode("2fa-verify");
      setNotice("Account created. Enter the 2FA code to finish signing in.");
    } catch (err) {
      setNotice(`Signup failed: ${err instanceof Error ? err.message : "Error"}`);
    } finally {
      setLoading(false);
    }
  }

  async function handleBegin2FA() {
    setLoading(true);
    try {
      const token = auth.session?.accessToken || "";
      const res = await api.beginTwoFactorEnrollment(token);
      setTotpQrUrl(res.otpAuthUri);
      setEnrollmentId(res.enrollmentId);
      setMode("2fa-enroll");
    } catch {
      setNotice("Failed to begin 2FA enrollment.");
    } finally {
      setLoading(false);
    }
  }

  async function handleVerifyLogin2FA(e: React.FormEvent) {
    e.preventDefault();
    if (!otpCode.trim()) return;
    setLoading(true);
    setNotice(null);
    try {
      await auth.verify(otpCode);
      setNotice("Logged in successfully!");
      if (onClose) onClose();
    } catch (err) {
      setNotice(`Verification failed: ${err instanceof Error ? err.message : "Error"}`);
    } finally {
      setLoading(false);
    }
  }

  async function handleLoadDevelopment2FA() {
    if (!auth.pendingChallenge) {
      setNotice("Start login or signup before loading a development 2FA code.");
      return;
    }

    setLoading(true);
    try {
      const challenge = await api.getDevelopmentTwoFactorCode(auth.pendingChallenge.challengeId);
      setOtpCode(challenge.code);
      setNotice("Development 2FA code loaded from the backend.");
    } catch (err) {
      setNotice(`Could not load development 2FA code: ${err instanceof Error ? err.message : "Error"}`);
    } finally {
      setLoading(false);
    }
  }

  async function handleConfirm2FA() {
    if (!enrollmentId || !totpCode) return;
    setLoading(true);
    try {
      const token = auth.session?.accessToken || "";
      await api.confirmTwoFactorEnrollment(token, { enrollmentId, code: totpCode });
      setNotice("2FA Authenticator enabled successfully!");
      setMode("login");
    } catch {
      setNotice("Invalid 2FA code.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal-card max-w-md w-full p-6" id="AUTH-01">
        <div className="flex justify-between items-center mb-4">
          <div>
            <span className="badge badge-sun">AUTH-01..10</span>
            <h3 className="text-xl font-bold">NestyStay Authentication</h3>
          </div>
          {onClose && <button type="button" className="btn btn-ghost p-1" onClick={onClose}><X size={18} /></button>}
        </div>

        <PatoisPhrase phrase="Welcome to NestyStay" translation="Secure signed session authentication with 2FA and OTP." />

        {notice && <div className="notice-panel my-3 text-xs">{notice}</div>}

        {/* Tab switcher for Login / Register */}
        {(mode === "login" || mode === "register") && (
          <div className="flex border-b mb-4 my-3">
            <button 
              type="button" 
              className={`flex-1 py-2 font-bold text-center border-b-2 ${mode === "login" ? "border-sun text-sun" : "border-transparent text-gray-500"}`}
              onClick={() => setMode("login")}
            >
              Log In
            </button>
            <button 
              type="button" 
              className={`flex-1 py-2 font-bold text-center border-b-2 ${mode === "register" ? "border-sun text-sun" : "border-transparent text-gray-500"}`}
              onClick={() => setMode("register")}
            >
              Sign Up
            </button>
          </div>
        )}

        {mode === "login" && (
          <form onSubmit={handleLogin} className="space-y-3">
            <div className="field-group">
              <label className="field-label" htmlFor="auth-login-email">Email Address</label>
              <input id="auth-login-email" type="email" className="input-control" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </div>
            <div className="field-group">
              <label className="field-label" htmlFor="auth-login-password">Password</label>
              <input id="auth-login-password" type="password" className="input-control" value={password} onChange={(e) => setPassword(e.target.value)} required />
            </div>
            <button type="submit" className="btn btn-primary w-full py-2" disabled={loading}>
              {loading ? "Signing in..." : "Log In to Account"}
            </button>
            <div className="text-center pt-2">
              <button type="button" className="btn btn-ghost text-xs" onClick={handleBegin2FA}>Enable 2FA Authenticator (TOTP)</button>
            </div>
          </form>
        )}

        {mode === "register" && (
          <form onSubmit={handleRegister} className="space-y-3">
            <div className="field-group">
              <label className="field-label" htmlFor="auth-register-name">Display name</label>
              <input
                id="auth-register-name"
                type="text"
                className="input-control"
                value={registerDisplayName}
                onChange={(e) => setRegisterDisplayName(e.target.value)}
                required
              />
            </div>
            <div className="field-group">
              <label className="field-label" htmlFor="auth-register-email">Email Address</label>
              <input id="auth-register-email" type="email" className="input-control" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </div>
            <div className="field-group">
              <label className="field-label" htmlFor="auth-register-phone">Phone</label>
              <input id="auth-register-phone" type="tel" className="input-control" value={registerPhone} onChange={(e) => setRegisterPhone(e.target.value)} />
            </div>
            <div className="field-group">
              <label className="field-label" htmlFor="auth-register-role">Account type</label>
              <select id="auth-register-role" className="input-control" value={registerRole} onChange={(e) => setRegisterRole(e.target.value as "Guest" | "Host")}>
                <option value="Guest">Guest</option>
                <option value="Host">Host</option>
              </select>
            </div>
            <div className="field-group">
              <label className="field-label" htmlFor="auth-register-password">Password</label>
              <input id="auth-register-password" type="password" className="input-control" value={password} onChange={(e) => setPassword(e.target.value)} required />
            </div>
            <div className="field-group">
              <label className="field-label" htmlFor="auth-register-confirm">Confirm password</label>
              <input
                id="auth-register-confirm"
                type="password"
                className="input-control"
                value={registerConfirmPassword}
                onChange={(e) => setRegisterConfirmPassword(e.target.value)}
                required
              />
            </div>
            <label className="checkbox-row text-xs">
              <input type="checkbox" checked={acceptedTerms} onChange={(e) => setAcceptedTerms(e.target.checked)} />
              I accept the terms.
            </label>
            <label className="checkbox-row text-xs">
              <input type="checkbox" checked={acceptedPrivacy} onChange={(e) => setAcceptedPrivacy(e.target.checked)} />
              I accept the privacy policy.
            </label>
            <button type="submit" className="btn btn-primary w-full py-2" disabled={loading || auth.isAuthBusy}>
              {loading || auth.isAuthBusy ? "Creating account..." : "Create Account"}
            </button>
          </form>
        )}

        {mode === "2fa-enroll" && (
          <div className="text-center space-y-3" id="AUTH-04">
            <p className="subtext text-xs">Scan QR Code with Google Authenticator or 1Password.</p>
            {totpQrUrl && <img src={totpQrUrl} alt="TOTP QR Code" className="w-40 h-40 mx-auto border p-2 rounded" />}
            <input 
              type="text" 
              className="input-control text-center font-bold tracking-widest text-lg" 
              placeholder="000 000" 
              value={totpCode} 
              onChange={(e) => setTotpCode(e.target.value)} 
            />
            <button type="button" className="btn btn-primary w-full" onClick={handleConfirm2FA}>
              Verify & Enable 2FA
            </button>
          </div>
        )}

        {mode === "2fa-verify" && (
          <form onSubmit={handleVerifyLogin2FA} className="space-y-3" id="AUTH-05">
            <div className="field-group">
              <label className="field-label" htmlFor="auth-login-2fa-code">Authenticator code</label>
              <input
                id="auth-login-2fa-code"
                type="text"
                className="input-control text-center font-bold tracking-widest text-lg"
                placeholder="000 000"
                value={otpCode}
                onChange={(e) => setOtpCode(e.target.value)}
                required
              />
            </div>
            <button type="submit" className="btn btn-primary w-full py-2" disabled={loading}>
              {loading ? "Verifying..." : "Verify & continue"}
            </button>
            <button type="button" className="btn btn-ghost w-full text-xs" onClick={handleLoadDevelopment2FA} disabled={loading}>
              Use development 2FA code
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
