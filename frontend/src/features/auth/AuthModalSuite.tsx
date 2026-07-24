import { useState } from "react";
import { Lock, Mail, KeyRound, ShieldCheck, QrCode, RefreshCw, X, ArrowRight } from "lucide-react";
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
      await auth.login(email, password);
      setNotice("Logged in successfully!");
      if (onClose) onClose();
    } catch (err) {
      setNotice(`Login failed: ${err instanceof Error ? err.message : "Error"}`);
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
    } catch (err) {
      setNotice("Failed to begin 2FA enrollment.");
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
    } catch (err) {
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
              <label className="field-label">Email Address</label>
              <input type="email" className="input-control" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </div>
            <div className="field-group">
              <label className="field-label">Password</label>
              <input type="password" className="input-control" value={password} onChange={(e) => setPassword(e.target.value)} required />
            </div>
            <button type="submit" className="btn btn-primary w-full py-2" disabled={loading}>
              {loading ? "Signing in..." : "Log In to Account"}
            </button>
            <div className="text-center pt-2">
              <button type="button" className="btn btn-ghost text-xs" onClick={handleBegin2FA}>Enable 2FA Authenticator (TOTP)</button>
            </div>
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
      </div>
    </div>
  );
}
