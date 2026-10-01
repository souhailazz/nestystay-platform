import { useEffect, useRef, useState, type FormEvent } from "react";
import { BriefcaseBusiness, Building2, Compass, Eye, EyeOff, Home, ShieldCheck, Store, X, type LucideIcon } from "lucide-react";
import { AppLink, navigate } from "../../components/AppLink";
import { EmblemRoundel, deepPatternBackground } from "../../components/layout/PublicShell";
import { api } from "../../lib/api";
import { loadSession } from "../../lib/auth";
import { userSafeErrorMessage } from "../../lib/errorMessages";
import { cx } from "../../lib/ui";
import { LEGAL_DETAILS, openCookieSettings } from "../../lib/legal";
import { LoadingSpinner } from "../../components/ui/LoadingSpinner";
import { setUnsavedChanges } from "../../lib/unsavedChanges";
import { signInWithGoogle } from "./googleSignIn";
import { isRoleSafeReturnTo, postAuthRoute } from "./postAuthRoute";
import type { AuthModalMode } from "./types";
import type { AuthController } from "../../hooks/useAuth";
import { PUBLIC_WORKSPACE_LOGIN_PATHS, PUBLIC_WORKSPACE_OPTIONS, type PublicWorkspaceRole } from "./workspaceOptions";

interface AuthModalSuiteProps {
  initialMode?: AuthModalMode;
  auth: AuthController;
  onClose?: () => void;
  returnTo?: string;
  workspaceRole?: PublicWorkspaceRole;
}

/* AUTH-01 (DS v2) — split brand panel + form cards. All auth logic and API
   calls are unchanged from the previous implementation; user-safe errors are
   shown in the coral notice zone. */

const inputClass =
  "min-h-12 w-full rounded-field border-[1.5px] border-sand-input bg-white px-4 font-sans text-[14.5px] text-ink outline-none transition-[border-color,box-shadow] duration-200 placeholder:text-sand-500 focus:border-deep-hover focus:shadow-[0_0_0_3px_rgba(14,74,69,0.12)]";
const labelText = "font-sans text-[13px] font-semibold text-ink";
const cardClass = "flex w-full max-w-[720px] self-center flex-col gap-4 rounded-card border border-sand-border bg-cream p-6 sm:p-7";
const deepPill =
  "flex min-h-[50px] cursor-pointer items-center justify-center gap-2.5 rounded-pill border-none bg-deep font-sans text-[15px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:pointer-events-none disabled:bg-shell disabled:text-sand-500";

const workspaceIcons: Record<PublicWorkspaceRole, LucideIcon> = {
  Guest: Compass,
  Host: Home,
  PropertyManager: Building2,
  ServiceProvider: BriefcaseBusiness,
  LocalBusiness: Store,
  Officer: ShieldCheck,
};

const workspacePanelContent: Record<PublicWorkspaceRole, {
  eyebrow: string;
  title: string;
  highlight: string;
  description: string;
  image: string;
  imageAlt: string;
}> = {
  Guest: {
    eyebrow: "For guests",
    title: "Find a stay",
    highlight: "that feels like yours.",
    description: "Discover verified homes, local know-how, and the Jamaica beyond the guidebook.",
    image: "/assets/auth/workspace-guest.webp",
    imageAlt: "Guest arriving at a Jamaican guesthouse with a rolling suitcase",
  },
  Host: {
    eyebrow: "For hosts & owners",
    title: "Share the place",
    highlight: "you call home.",
    description: "List your property, welcome guests, and manage reservations with confidence.",
    image: "/assets/auth/workspace-host.webp",
    imageAlt: "Jamaican host welcoming guests from a villa doorway",
  },
  PropertyManager: {
    eyebrow: "For property managers",
    title: "Keep the whole portfolio",
    highlight: "in rhythm.",
    description: "Manage properties, owners, finances, and daily operations from one workspace.",
    image: "/assets/auth/workspace-property-manager.webp",
    imageAlt: "Property manager reviewing a tablet and keys in a Jamaican guesthouse courtyard",
  },
  ServiceProvider: {
    eyebrow: "For custodians & service providers",
    title: "Bring your craft",
    highlight: "into every stay.",
    description: "Keep homes ready and offer trusted cleaning, maintenance, and hospitality support.",
    image: "/assets/auth/workspace-service-provider.webp",
    imageAlt: "Jamaican hospitality professional preparing fresh linens in a guest room",
  },
  LocalBusiness: {
    eyebrow: "For local businesses",
    title: "Put local",
    highlight: "on the itinerary.",
    description: "Connect your products, services, and community to people staying nearby.",
    image: "/assets/auth/workspace-local-business.webp",
    imageAlt: "Jamaican local business owner welcoming visitors from her shop",
  },
  Officer: {
    eyebrow: "For wellness officers",
    title: "Support the stay",
    highlight: "with care.",
    description: "Offer approved wellness and safety support while keeping your public role private.",
    image: "/assets/auth/workspace-wellness-officer.webp",
    imageAlt: "Jamaican wellness professional offering calm support beside a coastal garden",
  },
};

function PasswordVisibilityButton({ visible, onToggle }: { visible: boolean; onToggle: () => void }) {
  return (
    <button
      aria-label={visible ? "Hide password" : "Show password"}
      aria-pressed={visible}
      className="absolute right-2 top-1/2 inline-flex size-10 -translate-y-1/2 items-center justify-center rounded-pill border-none bg-transparent text-sand-600 transition-colors hover:bg-shell hover:text-deep focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-deep-hover"
      onClick={onToggle}
      title={visible ? "Hide password" : "Show password"}
      type="button"
    >
      {visible ? <EyeOff aria-hidden="true" size={18} /> : <Eye aria-hidden="true" size={18} />}
    </button>
  );
}

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

function isValidEmail(value: string) {
  const at = value.indexOf("@");
  return at > 0 && at === value.lastIndexOf("@") && value.indexOf(".", at + 2) > at + 1 && !value.includes(" ");
}

function CodeBoxes({ code, onChange }: { code: string; onChange: (code: string) => void }) {
  const refs = useRef<Array<HTMLInputElement | null>>([]);
  const digits = [
    { key: "otp-digit-1", value: code[0] ?? "" },
    { key: "otp-digit-2", value: code[1] ?? "" },
    { key: "otp-digit-3", value: code[2] ?? "" },
    { key: "otp-digit-4", value: code[3] ?? "" },
    { key: "otp-digit-5", value: code[4] ?? "" },
    { key: "otp-digit-6", value: code[5] ?? "" },
  ];
  return (
    <div className="flex flex-wrap gap-2">
      {digits.map(({ key, value }, i) => (
        <input
          aria-label={`Digit ${i + 1}`}
          className="min-h-14 w-12 rounded-field border-[1.5px] border-sand-input bg-white text-center font-display text-[22px] text-ink outline-none transition-[border-color,box-shadow] focus:border-deep-hover focus:shadow-[0_0_0_3px_rgba(14,74,69,0.12)]"
          inputMode="numeric"
          key={key}
          maxLength={1}
          onChange={(event) => {
            const nextValue = event.target.value.replace(/\D/g, "").slice(-1);
            const next = digits.slice();
            next[i] = { key, value: nextValue };
            onChange(next.map((digit) => digit.value).join("").slice(0, 6));
            if (nextValue && i < 5) refs.current[i + 1]?.focus();
          }}
          onKeyDown={(event) => {
            if (event.key === "Backspace" && !value && i > 0) refs.current[i - 1]?.focus();
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
          value={value}
        />
      ))}
    </div>
  );
}

export function AuthModalSuite({ initialMode = "login", auth, onClose, returnTo, workspaceRole }: AuthModalSuiteProps) {
  const [mode, setMode] = useState<AuthModalMode>(initialMode);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loginEmailError, setLoginEmailError] = useState<string | null>(null);
  const [loginCapsLock, setLoginCapsLock] = useState(false);
  const [showLoginPassword, setShowLoginPassword] = useState(false);
  const [registerDisplayName, setRegisterDisplayName] = useState("");
  const [registerPhone, setRegisterPhone] = useState("");
  const [registerConfirmPassword, setRegisterConfirmPassword] = useState("");
  const [showRegisterPassword, setShowRegisterPassword] = useState(false);
  const [showRegisterConfirmPassword, setShowRegisterConfirmPassword] = useState(false);
  const [registerCapsLock, setRegisterCapsLock] = useState(false);
  const [confirmCapsLock, setConfirmCapsLock] = useState(false);
  const [registerRole, setRegisterRole] = useState<"Guest" | "Host" | "Owner" | "PropertyManager" | "Officer" | "ServiceProvider" | "LocalBusiness">("Guest");
  const [selectedWorkspace, setSelectedWorkspace] = useState<PublicWorkspaceRole | null>(null);
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [acceptedPrivacy, setAcceptedPrivacy] = useState(false);
  const [registerErrors, setRegisterErrors] = useState<Record<string, string>>({});
  const [otpCode, setOtpCode] = useState("");
  const [smsCode, setSmsCode] = useState("");
  const [smsChallenge, setSmsChallenge] = useState<{ flowId: string; maskedPhone: string; expiresAt: string } | null>(null);
  const [rememberDevice, setRememberDevice] = useState(false);
  const [totpCode, setTotpCode] = useState("");
  const [totpQrUrl, setTotpQrUrl] = useState<string | null>(null);
  const [enrollmentId, setEnrollmentId] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [noticeTone, setNoticeTone] = useState<"error" | "success">("success");
  const [resetEmail, setResetEmail] = useState("");
  const [resetSent, setResetSent] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const countdown = useChallengeCountdown(auth.pendingChallenge?.expiresAt);

  useEffect(() => {
    setSelectedWorkspace(workspaceRole ?? null);
    if (workspaceRole) setRegisterRole(workspaceRole);
  }, [workspaceRole]);

  const registrationDirty = mode === "register" && Boolean(registerDisplayName || registerPhone || email || password || registerConfirmPassword || acceptedTerms || acceptedPrivacy);

  useEffect(() => {
    setUnsavedChanges(registrationDirty);
    if (!registrationDirty) return;
    const onBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", onBeforeUnload);
    return () => {
      window.removeEventListener("beforeunload", onBeforeUnload);
      setUnsavedChanges(false);
    };
  }, [registrationDirty]);

  function showError(message: string) {
    setNotice(message);
    setNoticeTone("error");
  }
  function showSuccess(message: string) {
    setNotice(message);
    setNoticeTone("success");
  }

  function clearRegisterError(field: string) {
    setRegisterErrors((current) => {
      if (!current[field]) return current;
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  function finishSignIn() {
    onClose?.();
    // Authentication writes the session synchronously, while React state is
    // updated asynchronously. Read the persisted session here so a newly
    // authenticated role is not routed through the guest fallback first.
    const roles = loadSession()?.roles ?? auth.session?.roles;
    if (isRoleSafeReturnTo(returnTo, roles)) {
      navigate(returnTo);
      return;
    }
    navigate(postAuthRoute(roles, registerRole));
  }

  async function handleLogin(e: FormEvent) {
    e.preventDefault();
    const normalizedEmail = email.trim().toLowerCase();
    if (!isValidEmail(normalizedEmail)) {
      setLoginEmailError("Enter a valid email address.");
      return;
    }
    setLoginEmailError(null);
    setLoading(true);
    setNotice(null);
    try {
      const result = await auth.login(normalizedEmail, password, { deviceName: typeof navigator !== "undefined" ? navigator.userAgent.slice(0, 80) : "Browser", rememberDevice });
      if ("challengeId" in result) {
        setMode("2fa-verify");
        showSuccess("Enter your authenticator code to finish signing in.");
        return;
      }
      finishSignIn();
    } catch (err) {
      showError(userSafeErrorMessage(err, "Unable to sign in. Check your email and password and try again."));
    } finally {
      setLoading(false);
    }
  }

  async function handleGoogle() {
    setLoading(true);
    setNotice(null);
    try {
      await signInWithGoogle(auth.signInWithGoogle, registerRole === "Host" ? "Host" : "Guest");
      finishSignIn();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Google sign-in failed.");
    } finally {
      setLoading(false);
    }
  }

  async function handleRequestPasswordless(e: FormEvent) {
    e.preventDefault();
    const normalizedEmail = email.trim().toLowerCase();
    if (!normalizedEmail) return;
    setLoading(true);
    setNotice(null);
    try {
      await auth.requestPasswordlessLogin(normalizedEmail);
      setMode("passwordless-request");
      showSuccess("If that email is registered, a secure sign-in link has been sent. It expires in 15 minutes.");
    } catch (err) {
      showError(err instanceof Error ? err.message : "Could not send the sign-in link.");
    } finally {
      setLoading(false);
    }
  }

  async function handleRegister(e: FormEvent) {
    e.preventDefault();
    setNotice(null);

    const errors: Record<string, string> = {};
    const normalizedEmail = email.trim().toLowerCase();
    if (!registerDisplayName.trim()) errors.displayName = "Enter your display name.";
    if (!isValidEmail(normalizedEmail)) errors.email = "Enter a valid email address.";
    if (password.length < 8 || !/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/\d/.test(password)) {
      errors.password = "Use at least 8 characters with an uppercase letter, lowercase letter, and number.";
    }
    if (password !== registerConfirmPassword) errors.confirmPassword = "Passwords must match.";
    if (!acceptedTerms) errors.terms = "Accept the Terms of Service to create an account.";
    if (!acceptedPrivacy) errors.privacy = "Acknowledge the Privacy Policy to create an account.";

    setRegisterErrors(errors);
    if (Object.keys(errors).length > 0) {
      showError("Please correct the highlighted fields.");
      return;
    }

    setLoading(true);

    try {
      const registered = await auth.register({
        email: normalizedEmail,
        password,
        displayName: registerDisplayName,
        phone: registerPhone,
        confirmPassword: registerConfirmPassword,
        acceptedTerms,
        acceptedPrivacy,
        role: registerRole,
      });
      if (registered.requiresTwoFactor) {
        setMode("2fa-verify");
        showSuccess("Account created. Enter the 2FA code to finish signing in.");
      } else {
        finishSignIn();
      }
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

  async function handleRequestSmsFallback() {
    setLoading(true);
    try {
      const challenge = await auth.requestSmsFallback();
      setSmsChallenge(challenge);
      setSmsCode("");
      showSuccess(`A verification code was sent to ${challenge.maskedPhone}.`);
    } catch (err) {
      showError(err instanceof Error ? err.message : "SMS fallback is unavailable.");
    } finally {
      setLoading(false);
    }
  }

  async function handleVerifySmsFallback(e?: FormEvent) {
    e?.preventDefault();
    if (!smsChallenge || smsCode.length < 6) return;
    setLoading(true);
    try {
      await auth.verifySmsFallback(smsChallenge.flowId, smsCode);
      finishSignIn();
    } catch (err) {
      showError(err instanceof Error ? err.message : "SMS verification failed.");
    } finally {
      setLoading(false);
    }
  }

  async function handlePasskeyLogin() {
    setLoading(true);
    setNotice(null);
    try {
      await auth.signInWithPasskey(email.trim() || undefined);
      finishSignIn();
    } catch (err) {
      showError(err instanceof Error ? err.message : "Passkey sign-in failed.");
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
      const res = await api.requestPasswordReset(resetEmail.trim().toLowerCase());
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
  const passwordScore = passwordChecks.filter(([, passed]) => passed).length;
  const passwordStrength = passwordScore === 0 ? "Start with a strong password" : passwordScore < 3 ? "Needs a little more strength" : passwordScore === 3 ? "Good password" : "Strong password";
  const confirmPasswordMismatch = registerConfirmPassword.length > 0 && password !== registerConfirmPassword;
  const activeWorkspace = workspaceRole ?? "Guest";
  const panelContent = workspacePanelContent[activeWorkspace];

  const noticePanel = notice && (
    <div
      className={cx(
        "rounded-field px-4 py-3 font-sans text-[13px]",
        noticeTone === "error" ? "bg-coral-tint text-coral-text" : "bg-success-tint text-success-text",
      )}
      aria-live={noticeTone === "error" ? "assertive" : "polite"}
    >
      {notice}
    </div>
  );

  return (
    <div className="grid min-h-screen font-sans text-[15px] leading-[1.55] text-ink md:grid-cols-[minmax(320px,44%)_1fr]" id="AUTH-01">
      {/* Brand panel */}
      <aside className="relative isolate flex min-h-[520px] flex-col justify-between gap-10 overflow-hidden p-8 sm:p-11" style={deepPatternBackground}>
        <img alt={panelContent.imageAlt} className="pointer-events-none absolute inset-0 z-0 h-full w-full object-cover object-center opacity-45 mix-blend-screen" decoding="async" fetchPriority="high" height={1536} loading="eager" sizes="(max-width: 767px) 100vw, 44vw" src={panelContent.image} width={1024} />
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 z-0 bg-[linear-gradient(180deg,rgba(6,43,43,0.84),rgba(6,43,43,0.58)_42%,rgba(6,43,43,0.96))]" />
        <AppLink aria-label="NestyStay home" className="relative z-10 flex items-center gap-3" href="/">
          <EmblemRoundel size={60} />
          <span aria-hidden="true" className="text-[18px] font-bold tracking-[0.14em] text-sand">NESTY STAY</span>
        </AppLink>
        <div className="relative z-10 flex max-w-[430px] flex-col gap-3.5">
          <span className="w-fit rounded-pill border border-yellow/50 bg-deep/40 px-3 py-1.5 text-[10px] font-bold uppercase tracking-[0.18em] text-yellow">{panelContent.eyebrow}</span>
          <h1 className="m-0 font-display text-[clamp(36px,4vw,56px)] font-normal leading-[1.02] tracking-[-0.015em] text-on-dark-heading [text-wrap:balance]">
            {panelContent.title} <em className="italic text-yellow">{panelContent.highlight}</em>
          </h1>
          <p className="m-0 max-w-[380px] text-[14.5px] text-on-dark-muted">
            {panelContent.description}
          </p>
        </div>
        <div className="relative z-10 flex flex-wrap items-center gap-x-3 gap-y-1 text-[13px] text-on-dark-faint">
          <span>nestystay.net ·{" "}
            <a className="text-on-dark-faint hover:text-on-dark-body" href={LEGAL_DETAILS.supportTel}>
              {LEGAL_DETAILS.supportPhone}
            </a>
          </span>
          <AppLink className="text-on-dark-faint underline-offset-2 hover:text-on-dark-body hover:underline" href="/privacy">Privacy</AppLink>
          <AppLink className="text-on-dark-faint underline-offset-2 hover:text-on-dark-body hover:underline" href="/cookies">Cookies</AppLink>
          <button className="text-on-dark-faint underline-offset-2 hover:text-on-dark-body hover:underline" data-cookie-settings-trigger="true" onClick={openCookieSettings} type="button">Cookie settings</button>
        </div>
      </aside>

      {/* Forms column */}
      <main className="flex min-w-0 w-full flex-col gap-[18px] px-[clamp(24px,5vw,72px)] py-8 sm:py-10 lg:py-12">
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

        {(mode === "login" || mode === "register") && (
          <section aria-labelledby="workspace-chooser-heading" className="mx-auto grid w-full max-w-[720px] gap-3 rounded-card border border-sand-border bg-shell/60 p-4 shadow-[0_8px_24px_rgba(96,74,20,0.06)] sm:p-5">
            <div>
              <span className="text-[10px] font-bold uppercase tracking-[0.18em] text-sand-600">The NestyStay ecosystem</span>
              <h2 className="m-0 mt-1 font-display text-[clamp(24px,3vw,30px)] font-medium leading-tight" id="workspace-chooser-heading">More than a place to stay.</h2>
              <p className="m-0 mt-1 text-[13px] text-gray-600">One platform for guests, hosts, property teams, local businesses, and wellness services.</p>
              <h3 className="m-0 mt-3 text-xs font-bold uppercase tracking-[0.1em] text-deep-hover">Choose your workspace</h3>
            </div>
            <div className="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
              {PUBLIC_WORKSPACE_OPTIONS.map((option) => {
                const Icon = workspaceIcons[option.role];
                const selected = selectedWorkspace === option.role;
                return (
                  <AppLink
                    aria-current={selected ? "page" : undefined}
                    className={cx("group flex min-h-[68px] items-center gap-2.5 rounded-field border bg-white p-2.5 transition-colors hover:border-deep-hover hover:bg-cream", selected ? "border-deep bg-cream shadow-[0_0_0_2px_rgba(14,74,69,0.12)]" : "border-sand-input")}
                    href={`${mode === "register" ? "/register" : PUBLIC_WORKSPACE_LOGIN_PATHS[option.role]}?workspace=${encodeURIComponent(option.role)}`}
                    key={option.role}
                  >
                    <span className={cx("grid size-8 shrink-0 place-items-center rounded-full", selected ? "bg-deep text-yellow" : "bg-shell text-deep-hover")}><Icon aria-hidden="true" size={16} /></span>
                    <span className="min-w-0"><strong className="block text-[12px] leading-tight text-ink">{option.label}</strong><span className="mt-0.5 block text-[10.5px] leading-snug text-sand-600">{option.description}</span></span>
                  </AppLink>
                );
              })}
            </div>
            <p className="m-0 text-[11.5px] leading-relaxed text-sand-600" role="note">Workspace selection sets context only. Your authenticated role and permissions always come from the server. Super Admin access is separate and is not offered here.</p>
            {selectedWorkspace && <p aria-live="polite" className="m-0 rounded-field bg-success-tint px-3 py-2 text-xs font-semibold text-success-text">Selected: {PUBLIC_WORKSPACE_OPTIONS.find((option) => option.role === selectedWorkspace)?.label}. {mode === "register" ? "This role will be used for your account request." : "Your existing account role will determine the dashboard after sign-in."}</p>}
          </section>
        )}

        {(mode === "login" || mode === "register") && <AppLink className="self-start text-xs font-semibold text-deep-hover underline underline-offset-4" href="/explore">Explore as a guest →</AppLink>}

        {mode === "login" && (
          <form className={cardClass} noValidate onSubmit={handleLogin}>
            <h2 className="m-0 font-display text-[26px] font-medium">Log in</h2>
            <button
              className="flex min-h-12 cursor-pointer items-center justify-center gap-2.5 rounded-pill border-[1.5px] border-sand-input bg-white font-sans text-[14.5px] font-semibold text-ink transition-colors hover:border-deep disabled:pointer-events-none disabled:opacity-60"
              disabled={loading}
              onClick={handleGoogle}
              type="button"
            >
              {loading && <LoadingSpinner label="Signing in with Google" />}<GoogleMark /> Continue with Google
            </button>
            <div className="flex items-center gap-3 text-xs text-sand-500">
              <span className="h-px flex-1 bg-sand-border" />
              <span>or with email</span>
              <span className="h-px flex-1 bg-sand-border" />
            </div>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Email</span>
              <input
                autoComplete="email"
                aria-describedby={loginEmailError ? "login-email-error" : undefined}
                aria-invalid={Boolean(loginEmailError)}
                className={inputClass}
                id="login-email"
                name="email"
                onChange={(e) => { setEmail(e.target.value); if (loginEmailError) setLoginEmailError(null); }}
                placeholder="you@example.com"
                required
                type="email"
                value={email}
              />
              {loginEmailError && <p className="m-0 text-xs text-coral-text" id="login-email-error" role="alert">{loginEmailError}</p>}
            </label>
            <label className="flex cursor-pointer items-center gap-2.5 text-[12.5px] text-gray-600">
              <input checked={rememberDevice} className="size-4 accent-deep-hover" onChange={(event) => setRememberDevice(event.target.checked)} type="checkbox" />{" "}
              Remember me on this device for 30 days
            </label>
            <div className="flex flex-col gap-1.5">
              <label className={labelText} htmlFor="login-password">Password</label>
              <div className="relative">
                <input
                  autoComplete="current-password"
                  className={`${inputClass} pr-12`}
                  id="login-password"
                  name="password"
                  onChange={(e) => setPassword(e.target.value)}
                  onKeyDown={(event) => setLoginCapsLock(event.getModifierState?.("CapsLock") ?? false)}
                  onKeyUp={(event) => setLoginCapsLock(event.getModifierState?.("CapsLock") ?? false)}
                  required
                  type={showLoginPassword ? "text" : "password"}
                  value={password}
                />
                <PasswordVisibilityButton visible={showLoginPassword} onToggle={() => setShowLoginPassword((visible) => !visible)} />
              </div>
              {loginCapsLock && <p className="m-0 text-xs text-amber-text" role="status">Caps Lock is on.</p>}
            </div>
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
            {noticePanel}
            <button aria-busy={loading} className={deepPill} disabled={loading} type="submit">
              {loading && <LoadingSpinner label="Signing in" />} {loading ? "Signing in…" : "Log in"} <span aria-hidden="true">→</span>
            </button>
            <button
              className="cursor-pointer self-center border-none bg-transparent font-sans text-xs font-semibold text-deep-hover hover:text-deep"
              disabled={loading}
              onClick={() => {
                setMode("passwordless-request");
                setNotice(null);
              }}
              type="button"
            >
              Email me a passwordless sign-in link
            </button>
            <button
              className="cursor-pointer self-center border-none bg-transparent font-sans text-xs font-semibold text-deep-hover hover:text-deep disabled:opacity-60"
              disabled={loading || typeof window === "undefined" || !("PublicKeyCredential" in window)}
              onClick={handlePasskeyLogin}
              type="button"
            >
              Sign in with a passkey
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

        {mode === "passwordless-request" && (
          <form className={cardClass} onSubmit={handleRequestPasswordless}>
            <h2 className="m-0 font-display text-[26px] font-medium">Sign in without a password</h2>
            <p className="m-0 text-[13.5px] text-gray-600">
              We&apos;ll email a single-use link to open your NestyStay session. Links expire after 15 minutes.
            </p>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Email</span>
              <input
                autoComplete="email"
                className={inputClass}
                id="passwordless-email"
                name="email"
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                required
                type="email"
                value={email}
              />
            </label>
            {noticePanel}
            <button className={deepPill} disabled={loading || auth.isAuthBusy} type="submit">
              {loading || auth.isAuthBusy ? "Sending…" : "Send secure link"} <span aria-hidden="true">→</span>
            </button>
            <button
              className="cursor-pointer self-center border-none bg-transparent font-sans text-xs font-semibold text-gray-600 hover:text-deep-hover"
              onClick={() => {
                setMode("login");
                setNotice(null);
              }}
              type="button"
            >
              Back to password login
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
              {import.meta.env.DEV && (
                <button
                  className="inline-flex min-h-11 cursor-pointer items-center border-none bg-transparent font-sans text-[13.5px] font-semibold text-deep-hover hover:text-deep"
                  disabled={loading}
                  onClick={handleLoadDevelopment2FA}
                  type="button"
                >
                  Use development 2FA code
                </button>
              )}
              <button className="inline-flex min-h-11 cursor-pointer items-center border-none bg-transparent font-sans text-[13.5px] font-semibold text-deep-hover hover:text-deep" disabled={loading} onClick={handleRequestSmsFallback} type="button">
                Use SMS instead
              </button>
            </div>
            {smsChallenge && (
              <div className="mt-2 flex flex-col gap-3 rounded-field border border-sand-border bg-white p-4">
                <p className="m-0 text-[13px] text-gray-600">SMS code sent to <strong>{smsChallenge.maskedPhone}</strong>.</p>
                <CodeBoxes code={smsCode} onChange={setSmsCode} />
                <button className={deepPill} disabled={loading || smsCode.length < 6} onClick={() => void handleVerifySmsFallback()} type="button">{loading ? "Verifying…" : "Verify SMS code"}</button>
              </div>
            )}
          </form>
        )}

        {mode === "register" && (
          <form className={cardClass} noValidate onSubmit={handleRegister}>
            <h2 className="m-0 font-display text-[22px] font-medium">Create account</h2>
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="flex flex-col gap-1.5">
                <span className={labelText}>Display name</span>
                <input
                  aria-describedby={registerErrors.displayName ? "register-display-name-error" : undefined}
                  aria-invalid={Boolean(registerErrors.displayName)}
                  className={inputClass}
                  id="register-display-name"
                  name="name"
                  autoComplete="name"
                  onChange={(e) => { setRegisterDisplayName(e.target.value); clearRegisterError("displayName"); }}
                  placeholder="Keisha Brown"
                  required
                  type="text"
                  value={registerDisplayName}
                />
                {registerErrors.displayName && <p className="m-0 text-xs text-coral-text" id="register-display-name-error" role="alert">{registerErrors.displayName}</p>}
              </label>
              <label className="flex flex-col gap-1.5">
                <span className={labelText}>Phone</span>
                <input
                  autoComplete="tel"
                  className={inputClass}
                  id="register-phone"
                  name="tel"
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
                aria-describedby={registerErrors.email ? "register-email-error" : undefined}
                aria-invalid={Boolean(registerErrors.email)}
                className={inputClass}
                id="register-email"
                onChange={(e) => { setEmail(e.target.value); clearRegisterError("email"); }}
                placeholder="you@example.com"
                required
                type="email"
                value={email}
              />
              {registerErrors.email && <p className="m-0 text-xs text-coral-text" id="register-email-error" role="alert">{registerErrors.email}</p>}
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={labelText}>Account type</span>
              <select
                className={inputClass}
                onChange={(e) => setRegisterRole(e.target.value as "Guest" | "Host" | "Owner" | "PropertyManager" | "Officer" | "ServiceProvider" | "LocalBusiness")}
                value={registerRole}
              >
                <option value="Guest">Guest</option>
                <option value="Host">Host</option>
                <option value="PropertyManager">Property manager</option>
                <option value="Owner">Property owner</option>
                <option value="Officer">Wellness officer</option>
                <option value="ServiceProvider">Service provider</option>
                <option value="LocalBusiness">Local business</option>
              </select>
            </label>
            <div className="flex flex-col gap-1.5">
              <label className={labelText} htmlFor="register-password">Password</label>
              <div className="relative">
                <input
                  autoComplete="new-password"
                  aria-describedby={registerErrors.password ? "register-password-error" : undefined}
                  aria-invalid={Boolean(registerErrors.password)}
                   className={`${inputClass} pr-12`}
                   id="register-password"
                   name="new-password"
                   onChange={(e) => { setPassword(e.target.value); clearRegisterError("password"); }}
                   onKeyDown={(event) => setRegisterCapsLock(event.getModifierState?.("CapsLock") ?? false)}
                   onKeyUp={(event) => setRegisterCapsLock(event.getModifierState?.("CapsLock") ?? false)}
                   required
                  type={showRegisterPassword ? "text" : "password"}
                  value={password}
                />
                <PasswordVisibilityButton visible={showRegisterPassword} onToggle={() => setShowRegisterPassword((visible) => !visible)} />
              </div>
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
               <p className="m-0 text-xs text-sand-600" aria-live="polite">{passwordStrength}</p>
               {registerCapsLock && <p className="m-0 text-xs text-amber-text" role="status">Caps Lock is on.</p>}
               {registerErrors.password && <p className="m-0 text-xs text-coral-text" id="register-password-error" role="alert">{registerErrors.password}</p>}
            </div>
            <div className="flex flex-col gap-1.5">
              <label className={labelText} htmlFor="register-confirm-password">Confirm password</label>
              <div className="relative">
                <input
                  autoComplete="new-password"
                   aria-describedby={registerErrors.confirmPassword || confirmPasswordMismatch ? "register-confirm-password-error" : undefined}
                   aria-invalid={Boolean(registerErrors.confirmPassword || confirmPasswordMismatch)}
                   className={`${inputClass} pr-12`}
                   id="register-confirm-password"
                   name="password-confirmation"
                   onChange={(e) => { setRegisterConfirmPassword(e.target.value); clearRegisterError("confirmPassword"); }}
                   onKeyDown={(event) => setConfirmCapsLock(event.getModifierState?.("CapsLock") ?? false)}
                   onKeyUp={(event) => setConfirmCapsLock(event.getModifierState?.("CapsLock") ?? false)}
                  required
                  type={showRegisterConfirmPassword ? "text" : "password"}
                  value={registerConfirmPassword}
                 />
                 <PasswordVisibilityButton visible={showRegisterConfirmPassword} onToggle={() => setShowRegisterConfirmPassword((visible) => !visible)} />
               </div>
               {confirmCapsLock && <p className="m-0 text-xs text-amber-text" role="status">Caps Lock is on.</p>}
               {confirmPasswordMismatch && <p className="m-0 text-xs text-coral-text" id="register-confirm-password-error" role="alert">Passwords do not match yet.</p>}
               {registerErrors.confirmPassword && !confirmPasswordMismatch && <p className="m-0 text-xs text-coral-text" id="register-confirm-password-error" role="alert">{registerErrors.confirmPassword}</p>}
             </div>
            <label className="flex cursor-pointer items-start gap-2.5 font-sans text-[13px] text-ink">
              <input
                aria-describedby={registerErrors.terms ? "register-terms-error" : undefined}
                aria-invalid={Boolean(registerErrors.terms)}
                checked={acceptedTerms}
                className="size-4 accent-deep-hover"
                id="register-terms"
                onChange={(e) => { setAcceptedTerms(e.target.checked); clearRegisterError("terms"); }}
                type="checkbox"
              />
              <span>I agree to the <AppLink className="font-semibold text-deep-hover underline" href="/terms" target="_blank">Terms of Service</AppLink>.</span>
              {registerErrors.terms && <span className="text-xs text-coral-text" id="register-terms-error" role="alert">{registerErrors.terms}</span>}
             </label>
             <label className="flex cursor-pointer items-start gap-2.5 font-sans text-[13px] text-ink">
              <input
                aria-describedby={registerErrors.privacy ? "register-privacy-error" : undefined}
                aria-invalid={Boolean(registerErrors.privacy)}
                checked={acceptedPrivacy}
                className="size-4 accent-deep-hover"
                id="register-privacy"
                onChange={(e) => { setAcceptedPrivacy(e.target.checked); clearRegisterError("privacy"); }}
                type="checkbox"
              />
              <span>I acknowledge the <AppLink className="font-semibold text-deep-hover underline" href="/privacy" target="_blank">Privacy Policy</AppLink>.</span>
              {registerErrors.privacy && <span className="text-xs text-coral-text" id="register-privacy-error" role="alert">{registerErrors.privacy}</span>}
            </label>
            {noticePanel}
            <button aria-busy={loading || auth.isAuthBusy} className={deepPill} disabled={loading || auth.isAuthBusy} type="submit">
              {(loading || auth.isAuthBusy) && <LoadingSpinner label="Creating account" />} {loading || auth.isAuthBusy ? "Creating account…" : "Create my account"}
            </button>
            <div className="text-xs text-sand-500">By continuing you agree to the Terms. Backend errors show verbatim above the button.</div>
          </form>
        )}

        {mode === "2fa-enroll" && (
          <div className={cardClass}>
            <h2 className="m-0 font-display text-[22px] font-medium">Enable authenticator</h2>
            <p className="m-0 text-[13.5px] text-gray-600">Scan the QR code with Google Authenticator or 1Password, then enter the 6-digit code.</p>
            {totpQrUrl && (
              <img alt="TOTP QR Code" className="mx-auto size-40 rounded-field border border-sand-border bg-white p-2" height={160} src={totpQrUrl} width={160} />
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
                {loading && <LoadingSpinner label="Sending reset link" />} {loading ? "Sending…" : "Send reset link"}
              </button>
            </div>
            {resetSent && (
              <div className="rounded-field bg-success-tint px-4 py-3 font-sans text-[13px] text-success-text" aria-live="polite" role="status">
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
