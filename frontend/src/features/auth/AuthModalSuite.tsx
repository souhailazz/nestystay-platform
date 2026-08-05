import { useEffect, useRef, useState, type FormEvent } from "react";
import { X } from "lucide-react";
import { AppLink, navigate } from "../../components/AppLink";
import { EmblemRoundel, deepPatternBackground } from "../../components/layout/PublicShell";
import { api } from "../../lib/api";
import { cx } from "../../lib/ui";
import { signInWithGoogle } from "./googleSignIn";
import type { AuthModalMode } from "./types";
import type { AuthController } from "../../hooks/useAuth";

interface AuthModalSuiteProps {
  initialMode?: AuthModalMode;
  auth: AuthController;
  onClose?: () => void;
}

/* AUTH-01 (DS v2) — split brand panel + form cards. All auth logic and API
   calls are unchanged from the previous implementation; backend errors are
   shown verbatim in the coral notice zone. */

const inputClass =
  "min-h-12 w-full rounded-field border-[1.5px] border-sand-input bg-white px-4 font-sans text-[14.5px] text-ink outline-none transition-[border-color,box-shadow] duration-200 placeholder:text-sand-500 focus:border-deep-hover focus:shadow-[0_0_0_3px_rgba(14,74,69,0.12)]";
const labelText = "font-sans text-[13px] font-semibold text-ink";
const cardClass = "flex flex-col gap-4 rounded-card border border-sand-border bg-cream p-7";
const deepPill =
  "flex min-h-[50px] cursor-pointer items-center justify-center gap-2.5 rounded-pill border-none bg-deep font-sans text-[15px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:pointer-events-none disabled:bg-shell disabled:text-sand-500";

function GoogleMark() {
  return (
    <svg aria-hidden="true" height="18" viewBox="0 0 48 48" width="18">
      <path d="M43.6 20.1H42V20H24v8h11.3C33.7 32.7 29.2 36 24 36c-6.6 0-12-5.4-12-12s5.4-12 12-12c3.1 0 5.9 1.2 8 3l5.7-5.7C34.3 6.1 29.4 4 24 4 13 4 4 13 4 24s9 20 20 20 20-9 20-20c0-1.3-.1-2.6-.4-3.9z" fill="#FFC107" />
      <path d="M6.3 14.7l6.6 4.8C14.7 15.1 19 12 24 12c3.1 0 5.9 1.2 8 3l5.7-5.7C34.3 6.1 29.4 4 24 4 16.3 4 9.7 8.3 6.3 14.7z" fill="#FF3D00" />
      <path d="M24 44c5.2 0 9.9-2 13.4-5.2l-6.2-5.2C29.2 35.1 26.7 36 24 36c-5.2 0-9.6-3.3-11.3-8l-6.5 5C9.5 39.6 16.2 44 24 44z" fill="#4CAF50" />
      <path d="M43.6 20.1H42V20H24v8h11.3c-.8 2.3-2.3 4.3-4.1 5.7l6.2 5.2C36.9 39.2 44 34 44 24c0-1.3-.1-2.6-.4-3.9z" fill="#1976D2" />
    </svg>
  );
}

function useChallengeCountdown(expiresAt?: string) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    if (!expiresAt) return;
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [expiresAt]);
  if (!expiresAt) return null;
  const remaining = Math.max(0, new Date(expiresAt).getTime() - now);
  const minutes = Math.floor(remaining / 60_000);
  const seconds = Math.floor((remaining % 60_000) / 1000);
  return { expired: remaining <= 0, label: `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}` };
}

function maskEmail(email: string) {
  const at = email.indexOf("@");
  if (at <= 0) return email;
  return `${email[0]}•••${email.slice(at)}`;
}

function CodeBoxes({ code, onChange }: { code: string; onChange: (code: string) => void }) {
  const refs = useRef<Array<HTMLInputElement | null>>([]);
  const digits = Array.from({ length: 6 }, (_, i) => code[i] ?? "");
  return (
    <div className="flex flex-wrap gap-2">
      {digits.map((digit, i) => (
        <input
          aria-label={`Digit ${i + 1}`}
          className="min-h-14 w-12 rounded-field border-[1.5px] border-sand-input bg-white text-center font-display text-[22px] text-ink outline-none transition-[border-color,box-shadow] focus:border-deep-hover focus:shadow-[0_0_0_3px_rgba(14,74,69,0.12)]"
          inputMode="numeric"
          key={i}
          maxLength={1}
          onChange={(event) => {
            const value = event.target.value.replace(/\D/g, "").slice(-1);
            const next = digits.slice();
            next[i] = value;
            onChange(next.join("").slice(0, 6));
            if (value && i < 5) refs.current[i + 1]?.focus();
          }}
          onKeyDown={(event) => {
            if (event.key === "Backspace" && !digits[i] && i > 0) refs.current[i - 1]?.focus();
          }}
          onPaste={(event) => {
            const pasted = event.clipboardData.getData("text").replace(/\D/g, "").slice(0, 6);
            if (pasted.length > 1) {
              event.preventDefault();
              onChange(pasted);
              refs.current[Math.min(pasted.length, 5)]?.focus();
            }
          }}
          ref={(el) => {
            refs.current[i] = el;
          }}
          type="text"
          value={digit}
        />
      ))}
    </div>
  );
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
  const [noticeTone, setNoticeTone] = useState<"error" | "success">("success");
  const [resetEmail, setResetEmail] = useState("");
  const [resetSent, setResetSent] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const countdown = useChallengeCountdown(auth.pendingChallenge?.expiresAt);

  function showError(message: string) {
    setNotice(message);
    setNoticeTone("error");
  }
  function showSuccess(message: string) {
    setNotice(message);
    setNoticeTone("success");
  }

  function finishSignIn() {
    onClose?.();
    navigate("/guest-dashboard");
  }

  async function handleLogin(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setNotice(null);
    try {
      const result = await auth.login(email, password);
      if ("challengeId" in result) {
        setMode("2fa-verify");
        showSuccess("Enter your authenticator code to finish signing in.");
        return;
      }
      finishSignIn();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Login failed.");
    } finally {
      setLoading(false);
    }
  }

  async function handleGoogle() {
    setLoading(true);
    setNotice(null);
    try {
      await signInWithGoogle(auth.signInWithGoogle, registerRole);
      finishSignIn();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Google sign-in failed.");
    } finally {
      setLoading(false);
    }
  }

  async function handleRegister(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setNotice(null);

    if (password !== registerConfirmPassword) {
      showError("Passwords must match.");
      setLoading(false);
      return;
    }

    if (!acceptedTerms || !acceptedPrivacy) {
      showError("Accept the terms and privacy policy to create an account.");
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
      showSuccess("Account created. Enter the 2FA code to finish signing in.");
    } catch (err) {
      showError(err instanceof Error ? err.message : "Signup failed.");
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
      showError("Failed to begin 2FA enrollment.");
    } finally {
      setLoading(false);
    }
  }

  async function handleVerifyLogin2FA(e: FormEvent) {
    e.preventDefault();
    if (!otpCode.trim()) return;
    setLoading(true);
    setNotice(null);
    try {
      await auth.verify(otpCode);
      finishSignIn();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Verification failed.");
    } finally {
      setLoading(false);
    }
  }

  async function handleLoadDevelopment2FA() {
    if (!auth.pendingChallenge) {
      showError("Start login or signup before loading a development 2FA code.");
      return;
    }

    setLoading(true);
    try {
      const challenge = await api.getDevelopmentTwoFactorCode(auth.pendingChallenge.challengeId);
      setOtpCode(challenge.code);
      showSuccess("Development 2FA code loaded from the backend.");
    } catch (err) {
      showError(`Could not load development 2FA code: ${err instanceof Error ? err.message : "Error"}`);
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
      showSuccess("2FA Authenticator enabled successfully!");
      setMode("login");
    } catch {
      showError("Invalid 2FA code.");
    } finally {
      setLoading(false);
    }
  }

  async function handleRequestReset(e: FormEvent) {
    e.preventDefault();
    if (!resetEmail.trim()) return;
    setLoading(true);
    setResetSent(null);
    setNotice(null);
    try {
      const res = await api.requestPasswordReset(resetEmail);
      setResetSent(res.message || "If that address exists, a reset link is on its way. It expires in 30 minutes.");
    } catch (err) {
      showError(err instanceof Error ? err.message : "Could not send the reset link.");
    } finally {
      setLoading(false);
    }
  }

  const passwordChecks = [
    ["8+ characters", password.length >= 8],
    ["Uppercase", /[A-Z]/.test(password)],
    ["Lowercase", /[a-z]/.test(password)],
    ["Number", /\d/.test(password)],
  ] as const;

  const noticePanel = notice && (
    <div
      className={cx(
        "rounded-field px-4 py-3 font-sans text-[13px]",
        noticeTone === "error" ? "bg-coral-tint text-coral-text" : "bg-success-tint text-success-text",
      )}
      role={noticeTone === "error" ? "alert" : "status"}
    >
      {notice}
    </div>
  );

  return (
    <div className="grid min-h-screen font-sans text-[15px] leading-[1.55] text-ink md:grid-cols-[minmax(320px,44%)_1fr]" id="AUTH-01">
      {/* Brand panel */}
      <aside className="flex flex-col justify-between gap-10 p-11 px-11" style={deepPatternBackground}>
        <AppLink className="flex items-center gap-3" href="/">
          <EmblemRoundel size={48} />
          <span className="text-[15px] font-bold tracking-[0.14em] text-sand">NESTY STAY</span>
        </AppLink>
        <div className="flex flex-col gap-3.5">
          <h1 className="m-0 font-display text-[clamp(36px,4vw,56px)] font-normal leading-[1.02] tracking-[-0.015em] text-on-dark-heading [text-wrap:balance]">
            Come back to your <em className="italic text-yellow">yard.</em>
          </h1>
          <p className="m-0 max-w-[380px] text-[14.5px] text-on-dark-muted">
            Jamaica&apos;s own trusted stays platform — verified hosts, wellness visits, and local know-how.
          </p>
        </div>
        <div className="text-[13px] text-on-dark-faint">
          nestystay.net ·{" "}
          <a className="text-on-dark-faint hover:text-on-dark-body" href="https://wa.me/17542482435">
            754-248-2435
          </a>
        </div>
      </aside>

      {/* Forms column */}
      <main className="flex max-w-[640px] flex-col gap-[22px] px-[clamp(24px,5vw,72px)] py-12">
        {onClose && (
          <button
            aria-label="Close"
            className="grid size-11 cursor-pointer place-items-center self-end rounded-pill border border-sand-border bg-transparent text-ink hover:bg-shell"
            onClick={onClose}
            type="button"
          >
            <X size={18} />
          </button>
        )}

        {(mode === "login" || mode === "register") && (
          <div className="flex gap-1 self-start rounded-pill bg-shell p-1">
            {(
              [
                ["login", "Log in"],
                ["register", "Create account"],
              ] as const
            ).map(([value, label]) => (
              <button
                className={cx(
                  "inline-flex min-h-11 cursor-pointer items-center rounded-pill border-none px-6 font-sans text-sm font-semibold transition-colors",
                  mode === value ? "bg-deep text-on-dark-heading" : "bg-transparent text-gray-600 hover:text-deep-hover",
                )}
                key={value}
                onClick={() => {
                  setMode(value);
                  setNotice(null);
                }}
                type="button"
              >
                {label}
              </button>
            ))}
          </div>
        )}

        {mode === "login" && (
          <form className={cardClass} onSubmit={handleLogin}>
            <h2 className="m-0 font-display text-[26px] font-medium">Log in</h2>
            <button
              className="flex min-h-12 cursor-pointer items-center justify-center gap-2.5 rounded-pill border-[1.5px] border-sand-input bg-white font-sans text-[14.5px] font-semibold text-ink transition-colors hover:border-deep disabled:pointer-events-none disabled:opacity-60"
              disabled={loading}
              onClick={handleGoogle}
              type="button"
            >
              <GoogleMark /> Continue with Google
            </button>
            <div className="flex items-center gap-3 text-xs text-sand-500">
              <span className="h-px flex-1 bg-sand-border" />
              or with email
              <span className="h-px flex-1 bg-sand-border" />
            </div>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Email</span>
              <input
                autoComplete="email"
                className={inputClass}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                required
                type="email"
                value={email}
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Password</span>
              <input
                autoComplete="current-password"
                className={inputClass}
                onChange={(e) => setPassword(e.target.value)}
                required
                type="password"
                value={password}
              />
              <button
                className="inline-flex min-h-11 cursor-pointer items-center self-end border-none bg-transparent font-sans text-[12.5px] font-semibold text-deep-hover hover:text-deep"
                onClick={() => {
                  setMode("forgot-password");
                  setNotice(null);
                }}
                type="button"
              >
                Forgot password?
              </button>
            </label>
            {noticePanel}
            <button className={deepPill} disabled={loading} type="submit">
              {loading ? "Signing in…" : "Log in"} <span aria-hidden="true">→</span>
            </button>
            <button
              className="cursor-pointer self-center border-none bg-transparent font-sans text-xs font-semibold text-gray-600 hover:text-deep-hover"
              onClick={handleBegin2FA}
              type="button"
            >
              Enable 2FA Authenticator (TOTP)
            </button>
          </form>
        )}

        {mode === "2fa-verify" && (
          <form className={cardClass} onSubmit={handleVerifyLogin2FA}>
            <div className="flex flex-wrap items-baseline justify-between gap-3">
              <h2 className="m-0 font-display text-[22px] font-medium">Two-factor check</h2>
              {countdown && (
                <span
                  className={cx(
                    "rounded-pill px-3 py-[5px] text-[11px] font-bold tracking-[0.08em]",
                    countdown.expired ? "bg-coral-tint text-coral-text" : "bg-amber-tint text-amber-text",
                  )}
                >
                  {countdown.expired ? "CODE EXPIRED" : `EXPIRES IN ${countdown.label}`}
                </span>
              )}
            </div>
            <p className="m-0 text-[13.5px] text-gray-600">
              Enter the 6-digit code we sent to{" "}
              <strong>{auth.pendingChallenge ? maskEmail(auth.pendingChallenge.email) : "your email"}</strong>. Codes
              expire after 10 minutes.
            </p>
            <CodeBoxes code={otpCode} onChange={setOtpCode} />
            {noticePanel}
            <div className="flex flex-wrap items-center gap-3.5">
              <button
                className="min-h-12 cursor-pointer rounded-pill border-none bg-deep px-[26px] font-sans text-[14.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:pointer-events-none disabled:bg-shell disabled:text-sand-500"
                disabled={loading || otpCode.length < 6}
                type="submit"
              >
                {loading ? "Verifying…" : "Verify code"}
              </button>
              <button
                className="inline-flex min-h-11 cursor-pointer items-center border-none bg-transparent font-sans text-[13.5px] font-semibold text-deep-hover hover:text-deep"
                disabled={loading}
                onClick={handleLoadDevelopment2FA}
                type="button"
              >
                Use development 2FA code
              </button>
            </div>
          </form>
        )}

        {mode === "register" && (
          <form className={cardClass} onSubmit={handleRegister}>
            <h2 className="m-0 font-display text-[22px] font-medium">Create account</h2>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="flex flex-col gap-1.5">
                <span className={labelText}>Display name</span>
                <input
                  className={inputClass}
                  onChange={(e) => setRegisterDisplayName(e.target.value)}
                  placeholder="Keisha Brown"
                  required
                  type="text"
                  value={registerDisplayName}
                />
              </label>
              <label className="flex flex-col gap-1.5">
                <span className={labelText}>Phone</span>
                <input
                  className={inputClass}
                  onChange={(e) => setRegisterPhone(e.target.value)}
                  placeholder="+1 876 555 0123"
                  type="tel"
                  value={registerPhone}
                />
              </label>
            </div>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Email</span>
              <input
                autoComplete="email"
                className={inputClass}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                required
                type="email"
                value={email}
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Account type</span>
              <select
                className={inputClass}
                onChange={(e) => setRegisterRole(e.target.value as "Guest" | "Host")}
                value={registerRole}
              >
                <option value="Guest">Guest</option>
                <option value="Host">Host</option>
              </select>
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Password</span>
              <input
                autoComplete="new-password"
                className={inputClass}
                onChange={(e) => setPassword(e.target.value)}
                required
                type="password"
                value={password}
              />
              <div className="flex flex-wrap gap-2 text-[11.5px] font-semibold">
                {passwordChecks.map(([label, ok]) => (
                  <span
                    className={cx(
                      "rounded-pill px-2.5 py-1",
                      ok ? "bg-success-tint text-success-text" : "bg-shell text-sand-500",
                    )}
                    key={label}
                  >
                    {ok ? "✓ " : ""}
                    {label}
                  </span>
                ))}
              </div>
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Confirm password</span>
              <input
                autoComplete="new-password"
                className={inputClass}
                onChange={(e) => setRegisterConfirmPassword(e.target.value)}
                required
                type="password"
                value={registerConfirmPassword}
              />
            </label>
            <label className="flex cursor-pointer items-center gap-2.5 font-sans text-[13px] text-ink">
              <input
                checked={acceptedTerms}
                className="size-4 accent-deep-hover"
                onChange={(e) => setAcceptedTerms(e.target.checked)}
                type="checkbox"
              />
              I accept the terms.
            </label>
            <label className="flex cursor-pointer items-center gap-2.5 font-sans text-[13px] text-ink">
              <input
                checked={acceptedPrivacy}
                className="size-4 accent-deep-hover"
                onChange={(e) => setAcceptedPrivacy(e.target.checked)}
                type="checkbox"
              />
              I accept the privacy policy.
            </label>
            {noticePanel}
            <button className={deepPill} disabled={loading || auth.isAuthBusy} type="submit">
              {loading || auth.isAuthBusy ? "Creating account…" : "Create my account"}
            </button>
            <div className="text-xs text-sand-500">By continuing you agree to the Terms. Backend errors show verbatim above the button.</div>
          </form>
        )}

        {mode === "2fa-enroll" && (
          <div className={cardClass}>
            <h2 className="m-0 font-display text-[22px] font-medium">Enable authenticator</h2>
            <p className="m-0 text-[13.5px] text-gray-600">Scan the QR code with Google Authenticator or 1Password, then enter the 6-digit code.</p>
            {totpQrUrl && (
              <img alt="TOTP QR Code" className="mx-auto size-40 rounded-field border border-sand-border bg-white p-2" src={totpQrUrl} />
            )}
            <input
              className={cx(inputClass, "text-center font-display text-lg tracking-[0.4em]")}
              onChange={(e) => setTotpCode(e.target.value)}
              placeholder="000 000"
              type="text"
              value={totpCode}
            />
            {noticePanel}
            <button className={deepPill} disabled={loading} onClick={handleConfirm2FA} type="button">
              Verify &amp; enable 2FA
            </button>
            <button
              className="cursor-pointer self-center border-none bg-transparent font-sans text-[13px] font-semibold text-deep-hover hover:text-deep"
              onClick={() => setMode("login")}
              type="button"
            >
              ← Back to log in
            </button>
          </div>
        )}

        {mode === "forgot-password" && (
          <form className={cardClass} onSubmit={handleRequestReset}>
            <h2 className="m-0 font-display text-[22px] font-medium">Reset your password</h2>
            <div className="flex flex-wrap gap-2.5">
              <input
                aria-label="Your account email"
                className={cx(inputClass, "w-auto flex-[1_1_240px]")}
                onChange={(e) => setResetEmail(e.target.value)}
                placeholder="Your account email"
                required
                type="email"
                value={resetEmail}
              />
              <button
                className="min-h-12 cursor-pointer rounded-pill border-none bg-deep px-6 font-sans text-[14.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:pointer-events-none disabled:bg-shell disabled:text-sand-500"
                disabled={loading}
                type="submit"
              >
                {loading ? "Sending…" : "Send reset link"}
              </button>
            </div>
            {resetSent && (
              <div className="rounded-field bg-success-tint px-4 py-3 font-sans text-[13px] text-success-text" role="status">
                {resetSent}
              </div>
            )}
            {noticePanel}
            <button
              className="cursor-pointer self-start border-none bg-transparent font-sans text-[13px] font-semibold text-deep-hover hover:text-deep"
              onClick={() => {
                setMode("login");
                setNotice(null);
              }}
              type="button"
            >
              ← Back to log in
            </button>
          </form>
        )}
      </main>
    </div>
  );
}
