import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from "react";
import QRCode from "qrcode";
import {
  BadgeCheck,
  Bell,
  BookOpen,
  CalendarDays,
  Check,
  CreditCard,
  Download,
  FileText,
  LayoutDashboard,
  Lock,
  Mail,
  MessageSquare,
  Paperclip,
  RotateCcw,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Star,
  X,
  UserRound,
} from "lucide-react";
import { AppLink } from "../components/AppLink";
import { Badge } from "../components/ui/Badge";
import { Button, buttonClassName } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import { ErrorState } from "../components/ui/ErrorState";
import { Field, InlineLabel, Input, Select, Textarea } from "../components/ui/Input";
import { LoadingState } from "../components/ui/LoadingState";
import { Modal } from "../components/ui/Modal";
import { PageHeader } from "../components/ui/PageHeader";
import type { AuthController } from "../hooks/useAuth";
import { api, formatMoney, type AdminCase, type AdminOperations, type AttachmentUpload, type Booking, type Conversation, type DirectoryProvider, type Experience, type HostOperations, type HostProfile, type IdentityDocumentUpload, type JournalArticle, type MessageAttachment, type PublicContentPage, type TravelerWorkspace } from "../lib/api";
import { PatoisPhrase, PatoisToggle } from "../lib/patois";
import { getStayImage } from "../lib/stayImages";

type AsyncState<T> = {
  data: T | null;
  error: string | null;
  loading: boolean;
  reload: () => void;
};

function useAsync<T>(loader: () => Promise<T>, deps: React.DependencyList): AsyncState<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    setError(null);
    loader()
      .then((result) => mounted && setData(result))
      .catch((caught) => mounted && setError(caught instanceof Error ? caught.message : "Request failed."))
      .finally(() => mounted && setLoading(false));
    return () => {
      mounted = false;
    };
  }, [...deps, version]);

  return { data, error, loading, reload: () => setVersion((value) => value + 1) };
}

function CompletionShell({
  id,
  eyebrow,
  title,
  copy,
  actions,
  children,
}: {
  id: string;
  eyebrow: string;
  title: string;
  copy: string;
  actions?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="product-page spec-page completion-page">
      <PageHeader eyebrow={`${id} / ${eyebrow}`} title={title} copy={copy} actions={actions} />
      {children}
    </div>
  );
}

function DataGate<T>({ state, children }: { state: AsyncState<T>; children: (data: T) => ReactNode }) {
  if (state.loading) return <LoadingState />;
  if (state.error) return <ErrorState message={state.error} onRetry={state.reload} />;
  if (!state.data) return <EmptyState title="No data found." />;
  return <>{children(state.data)}</>;
}

function RequireSession({ auth, children }: { auth: AuthController; children: (session: NonNullable<AuthController["session"]>) => ReactNode }) {
  if (!auth.session) {
    return (
      <section className="product-section product-section--center">
        <EmptyState
          title="Sign in required."
          copy="This screen is connected to protected API data and needs a local session token."
          action={<AppLink className={buttonClassName("sun")} href="/login">Log in</AppLink>}
        />
      </section>
    );
  }
  return <>{children(auth.session)}</>;
}

function HeroImage({ index = 0, alt = "" }: { index?: number; alt?: string }) {
  const image = getStayImage(index);
  return <img className="completion-hero-image" src={image.src} alt={alt || image.alt} loading="lazy" />;
}

export function PublicContentRoute({ slug }: { slug: string }) {
  const state = useAsync(() => api.getPublicPage(slug), [slug]);
  const isContact = slug === "contact";
  return (
    <DataGate state={state}>
      {(page) => (
        <CompletionShell id={screenForPage(page)} eyebrow={page.kind} title={page.title} copy={page.summary}>
          <section className="product-section details-layout">
            <div className="details-copy">
              <p>{page.body}</p>
              <div className="highlight-list highlight-list--large">
                {page.sections.map((section) => (
                  <span key={section}><Check size={14} /> {section}</span>
                ))}
              </div>
              <div className="button-row">
                {page.links.map((link) => (
                  <AppLink className={buttonClassName("outline")} href={link} key={link}>{link}</AppLink>
                ))}
              </div>
            </div>
            {isContact ? <ContactForm /> : <HeroImage index={slug.length % 4} alt={page.title} />}
          </section>
        </CompletionShell>
      )}
    </DataGate>
  );
}

function ContactForm() {
  const [form, setForm] = useState({ name: "", email: "", subject: "", message: "" });
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setNotice(null);
    setError(null);
    try {
      await api.createContactRequest(form);
      setNotice("Contact request saved and queued for support.");
      setForm({ name: "", email: "", subject: "", message: "" });
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Contact request failed.");
    }
  }

  return (
    <Card className="settings-card">
      <form className="management-form" onSubmit={submit}>
        <Field label="Name"><Input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></Field>
        <Field label="Email"><Input type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></Field>
        <Field label="Subject"><Input value={form.subject} onChange={(event) => setForm({ ...form, subject: event.target.value })} /></Field>
        <Field label="Message"><Textarea value={form.message} onChange={(event) => setForm({ ...form, message: event.target.value })} /></Field>
        <Button type="submit"><Mail size={17} /> Send</Button>
        {notice && <div className="notice-panel">{notice}</div>}
        {error && <ErrorState message={error} />}
      </form>
    </Card>
  );
}

function screenForPage(page: PublicContentPage) {
  const map: Record<string, string> = {
    about: "PUB-09",
    trust: "PUB-09",
    help: "PUB-10",
    contact: "PUB-12",
    terms: "PUB-13",
    privacy: "PUB-14",
    maintenance: "ERR-05",
  };
  return map[page.slug] ?? "PUB-10";
}

export function AuthSpecFlowPage({ kind, auth }: { kind: string; auth: AuthController }) {
  const social = useAsync(() => api.getSocialAuthConfig(), []);
  const [flow, setFlow] = useState<{ id: string; deliveryChannel: string; expiresAt: string; status: string; attemptsRemaining: number } | null>(null);
  const [destination, setDestination] = useState(auth.session?.email ?? "guest@nestystay.local");
  const [code, setCode] = useState("");
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [resetRequestId, setResetRequestId] = useState(() => new URLSearchParams(window.location.search).get("requestId") ?? "");
  const [resetToken, setResetToken] = useState(() => new URLSearchParams(window.location.search).get("token") ?? "");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const canUseDevelopmentDelivery = import.meta.env.DEV || import.meta.env.MODE === "test";

  async function start(flowType = kind) {
    setError(null);
    const started = await api.startAuthFlow({ userId: auth.session?.userId, flowType, destination });
    setFlow({
      id: started.id,
      deliveryChannel: started.deliveryChannel,
      expiresAt: started.expiresAt,
      status: started.status,
      attemptsRemaining: started.attemptsRemaining,
    });
    setCode("");
    setNotice(`Code sent by ${started.deliveryChannel.toLowerCase()}.`);
  }

  async function complete() {
    if (!flow) return;
    setError(null);
    const completed = await api.completeAuthFlow({ flowId: flow.id, code });
    setFlow({
      id: completed.id,
      deliveryChannel: completed.deliveryChannel,
      expiresAt: completed.expiresAt,
      status: completed.status,
      attemptsRemaining: completed.attemptsRemaining,
    });
    setNotice(`${completed.flowType} completed: ${completed.status}`);
  }

  async function useDevelopmentDelivery() {
    if (!flow) return;
    setError(null);
    const secret = await api.getDevelopmentAuthFlowSecret(flow.id);
    setCode(secret.code);
    setNotice(`Development delivery loaded. Expires at ${new Date(secret.expiresAt).toLocaleTimeString()}.`);
  }

  async function requestReset() {
    setError(null);
    const response = await api.requestPasswordReset(destination);
    setResetRequestId(response.requestId);
    setNotice(response.message);
  }

  async function useDevelopmentResetToken() {
    setError(null);
    const secret = await api.getDevelopmentPasswordResetToken(resetRequestId);
    setResetToken(secret.token);
    setNotice(`Development reset token loaded. Expires at ${new Date(secret.expiresAt).toLocaleTimeString()}.`);
  }

  async function completeReset() {
    setError(null);
    const response = await api.completePasswordReset({
      requestId: resetRequestId,
      token: resetToken,
      newPassword,
      confirmPassword,
    });
    setNotice(response.passwordChanged ? "Password reset completed." : response.status);
    setResetToken("");
    setNewPassword("");
    setConfirmPassword("");
  }

  function run(action: () => Promise<void>) {
    void action().catch((caught) => {
      setError(caught instanceof Error ? caught.message : "Authentication flow failed.");
    });
  }

  const titleMap: Record<string, [string, string, string]> = {
    role: ["AUTH-02", "Come Een!", "Create your guest or host account."],
    email: ["AUTH-05", "Email verification", "Respek - confirm the six-digit verification code."],
    phone: ["AUTH-06", "Phone verification", "Jamaica +1876 phone verification with retry states."],
    otp: ["AUTH-07", "OTP entry", "Easy Nuh - the code is on its way."],
    forgot: ["AUTH-08", "Forgot password", "Nuh Worry Yuhself - we will sort this out."],
    reset: ["AUTH-09", "Reset password", "Yuh Back Inna Di Mix after reset completes."],
    twofa: ["AUTH-10", "2FA setup", "QR enrollment, recovery codes, and verification."],
    recovery: ["AUTH-10", "Recovery codes", "Generate, copy, and download local recovery codes."],
    social: ["AUTH-04", "Social auth consent", "Provider consent is shown only when configured."],
  };
  const [id, title, copy] = titleMap[kind] ?? titleMap.email;

  return (
    <CompletionShell
      id={id}
      eyebrow="Authentication"
      title={title}
      copy={copy}
      actions={<PatoisToggle />}
    >
      <section className="product-section management-layout">
        <Card className="settings-card">
          {kind === "role" ? (
            <>
              <PatoisPhrase phrase="Come Een!" translation="Come in! Welcome!" />
              <div className="spec-card-grid">
                <AppLink className={buttonClassName("sun")} href="/register?role=guest">Join as Guest</AppLink>
                <AppLink className={buttonClassName("outline")} href="/register?role=host">Join as Host</AppLink>
              </div>
            </>
          ) : kind === "twofa" || kind === "recovery" ? (
            <RequireSession auth={auth}>
              {(session) => <RecoveryCodesPanel userId={session.userId} token={session.accessToken} />}
            </RequireSession>
          ) : kind === "forgot" ? (
            <form className="management-form" onSubmit={(event) => { event.preventDefault(); run(requestReset); }}>
              <PatoisPhrase phrase="Nuh Worry Yuhself" translation="Don't worry about it - we'll sort this out." />
              <Field label="Email">
                <Input type="email" value={destination} onChange={(event) => setDestination(event.target.value)} />
              </Field>
              <Button type="submit"><ShieldCheck size={17} /> Send reset link</Button>
            </form>
          ) : kind === "reset" ? (
            <form className="management-form" onSubmit={(event) => { event.preventDefault(); run(completeReset); }}>
              <PatoisPhrase phrase="Yuh Back Inna Di Mix!" translation="You're back in the mix! Welcome back!" />
              <Field label="Request ID">
                <Input value={resetRequestId} onChange={(event) => setResetRequestId(event.target.value)} />
              </Field>
              <Field label="Reset token">
                <Input value={resetToken} onChange={(event) => setResetToken(event.target.value)} />
              </Field>
              {canUseDevelopmentDelivery && resetRequestId && (
                <Button type="button" onClick={() => run(useDevelopmentResetToken)} variant="ghost">Use development token</Button>
              )}
              <Field label="New password">
                <Input type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} />
              </Field>
              <Field label="Confirm password">
                <Input type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} />
              </Field>
              <Button type="submit"><Lock size={17} /> Reset password</Button>
            </form>
          ) : (
            <form className="management-form" onSubmit={(event) => { event.preventDefault(); run(() => start()); }}>
              <PatoisPhrase phrase={kind === "forgot" ? "Nuh Worry Yuhself" : kind === "otp" ? "Easy Nuh" : "Respek!"} translation="English translation is shown directly below the patois phrase." />
              <Field label={kind === "phone" ? "Phone number" : "Email"}>
                <Input value={destination} onChange={(event) => setDestination(event.target.value)} />
              </Field>
              <Button type="submit"><ShieldCheck size={17} /> Start flow</Button>
            </form>
          )}
          {flow && (
            <div className="notice-panel">
              <span>{flow.deliveryChannel} delivery pending until {new Date(flow.expiresAt).toLocaleTimeString()}.</span>
              <Field label="Enter code"><Input value={code} onChange={(event) => setCode(event.target.value)} /></Field>
              {canUseDevelopmentDelivery && (
                <Button onClick={() => run(useDevelopmentDelivery)} variant="ghost">Use development delivery</Button>
              )}
                <Button onClick={() => run(complete)}>Verify code</Button>
            </div>
          )}
          {notice && <div className="notice-panel">{notice}</div>}
          {error && <ErrorState message={error} />}
        </Card>
        <DataGate state={social}>
          {(config) => (
            <Card className="settings-card">
              <h3>Social authentication</h3>
              <p>Google is active only when server-side OAuth validation is configured. Apple and Facebook stay unavailable until complete secure flows ship.</p>
              <div className="button-row">
                <Button disabled={!config.googleEnabled} variant="outline">Google</Button>
                <Button disabled={!config.appleEnabled} variant="outline">Apple</Button>
                <Button disabled={!config.facebookEnabled} variant="outline">Facebook</Button>
              </div>
              <small>{config.requiredEnvironmentVariables.join(", ")}</small>
            </Card>
          )}
        </DataGate>
      </section>
    </CompletionShell>
  );
}

function RecoveryCodesPanel({ userId, token }: { userId: string; token: string }) {
  const [codes, setCodes] = useState<{ code: string; used: boolean }[]>([]);
  const [enrollment, setEnrollment] = useState<{ enrollmentId: string; manualKey: string; otpAuthUri: string; expiresAt: string } | null>(null);
  const [qrDataUri, setQrDataUri] = useState("");
  const [setupCode, setSetupCode] = useState("");
  const [disableCode, setDisableCode] = useState("");
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    if (!enrollment) {
      setQrDataUri("");
      return;
    }

    QRCode.toDataURL(enrollment.otpAuthUri, { margin: 1, width: 184 })
      .then((dataUri) => {
        if (!cancelled) setQrDataUri(dataUri);
      })
      .catch(() => {
        if (!cancelled) setError("Authenticator QR could not be rendered.");
      });

    return () => {
      cancelled = true;
    };
  }, [enrollment]);

  async function generate() {
    await run(async () => {
      setCodes(await api.generateRecoveryCodes(userId, token));
      setNotice("Recovery codes regenerated. Store them now; they are shown once.");
    });
  }

  async function beginEnrollment() {
    await run(async () => {
      const started = await api.beginTwoFactorEnrollment(token);
      setEnrollment(started);
      setCodes([]);
      setSetupCode("");
      setNotice("Authenticator setup started.");
    });
  }

  async function confirmEnrollment() {
    if (!enrollment) return;
    await run(async () => {
      const confirmed = await api.confirmTwoFactorEnrollment(token, {
        enrollmentId: enrollment.enrollmentId,
        code: setupCode,
      });
      setCodes(confirmed.recoveryCodes.map((code) => ({ code, used: false })));
      setEnrollment(null);
      setSetupCode("");
      setNotice("Authenticator enabled. Recovery codes are shown once.");
    });
  }

  async function disableTwoFactor() {
    await run(async () => {
      if (!disableCode.trim()) {
        throw new Error("Enter an authenticator or recovery code before disabling 2FA.");
      }

      const disabled = await api.disableTwoFactor(token, { code: disableCode });
      if (disabled.disabled) {
        setCodes([]);
        setEnrollment(null);
        setDisableCode("");
        setSetupCode("");
        setNotice("Authenticator disabled. Restart setup to require 2FA again.");
      }
    });
  }

  async function run(action: () => Promise<void>) {
    setError(null);
    setNotice(null);
    try {
      await action();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Two-factor action failed.");
    }
  }

  function download() {
    const blob = new Blob([codes.map((item) => item.code).join("\n")], { type: "text/plain" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "nestystay-recovery-codes.txt";
    link.click();
    URL.revokeObjectURL(url);
  }

  return (
    <>
      <Button onClick={beginEnrollment}><ShieldCheck size={17} /> Start authenticator setup</Button>
      {enrollment && (
        <div className="notice-panel">
          {qrDataUri && <img className="auth-qr-image" src={qrDataUri} alt="Authenticator QR code" />}
          <Field label="Manual setup key">
            <Input readOnly value={enrollment.manualKey} />
          </Field>
          <Field label="Authenticator code">
            <Input inputMode="numeric" value={setupCode} onChange={(event) => setSetupCode(event.target.value)} />
          </Field>
          <small>Expires at {new Date(enrollment.expiresAt).toLocaleTimeString()}.</small>
          <Button onClick={confirmEnrollment}><Lock size={17} /> Enable authenticator</Button>
        </div>
      )}
      <Button onClick={generate} variant="outline"><Lock size={17} /> Regenerate recovery codes</Button>
      <div className="notice-panel">
        <Field label="Authenticator or recovery code">
          <Input value={disableCode} onChange={(event) => setDisableCode(event.target.value)} />
        </Field>
        <Button onClick={disableTwoFactor} variant="ghost"><Lock size={17} /> Disable 2FA</Button>
      </div>
      {notice && <div className="notice-panel">{notice}</div>}
      {error && <ErrorState message={error} />}
      {codes.length > 0 && (
        <div className="spec-table-wrap">
          <table className="spec-table"><tbody>{codes.map((item) => <tr key={item.code}><td>{item.code}</td><td>{item.used ? "Used" : "Unused"}</td></tr>)}</tbody></table>
          <Button onClick={() => void navigator.clipboard.writeText(codes.map((item) => item.code).join("\n"))} variant="outline">Copy</Button>
          <Button onClick={download} variant="ghost"><Download size={17} /> Download</Button>
        </div>
      )}
    </>
  );
}

export function ExperiencesPage({ slug }: { slug?: string }) {
  const [query, setQuery] = useState("");
  const list = useAsync(() => api.getExperiences({ query }), [query]);
  const detail = useAsync(() => (slug ? api.getExperience(slug) : Promise.resolve(null)), [slug]);

  if (slug) {
    return (
      <DataGate state={detail}>
        {(experience) => experience && <ExperienceDetail experience={experience} />}
      </DataGate>
    );
  }

  return (
    <CompletionShell id="PUB-05" eyebrow="Experiences" title="Di Riddim Right" copy="Book food, music, water, and wellness experiences with verified local providers.">
      <section className="product-section">
        <div className="search-panel">
          <Field label="Search"><Input placeholder="Food, music, wellness, water" value={query} onChange={(event) => setQuery(event.target.value)} /></Field>
          <Button variant="dark"><Search size={17} /> Search</Button>
        </div>
        <DataGate state={list}>
          {(items) => items.length === 0 ? <EmptyState title="No experiences found." /> : (
            <div className="stay-result-grid">
              {items.map((experience, index) => <ExperienceCard experience={experience} index={index} key={experience.id} />)}
            </div>
          )}
        </DataGate>
      </section>
    </CompletionShell>
  );
}

function ExperienceCard({ experience, index }: { experience: Experience; index: number }) {
  return (
    <Card className="stay-result-card">
      <HeroImage index={index} alt={experience.name} />
      <div className="stay-result-card__body">
        <Badge tone="green">{experience.category}</Badge>
        <h3>{experience.name}</h3>
        <p>{experience.parish} - {experience.providerName}</p>
        <strong>{formatMoney(experience.price, experience.currency)} / guest</strong>
        <AppLink className={buttonClassName("outline")} href={`/experiences/${experience.slug}`}>View details</AppLink>
      </div>
    </Card>
  );
}

function ExperienceDetail({ experience }: { experience: Experience }) {
  return (
    <CompletionShell id="PUB-08" eyebrow="Experience detail" title={experience.name} copy={experience.summary}>
      <section className="product-section details-layout">
        <HeroImage index={2} alt={experience.name} />
        <div className="details-copy">
          <Badge tone="sun">{experience.rating} rating</Badge>
          <p>{experience.description}</p>
          <div className="highlight-list highlight-list--large">
            {[...experience.included, ...experience.rules, ...experience.availability].map((item) => <span key={item}><Check size={14} /> {item}</span>)}
          </div>
          <div className="booking-sidebar-lite">
            <strong>{formatMoney(experience.price, experience.currency)}</strong>
            <Field label="Guests"><Input min="1" defaultValue="2" type="number" /></Field>
            <Field label="Date"><Input type="date" /></Field>
            <Button><CalendarDays size={17} /> Request experience</Button>
          </div>
        </div>
      </section>
    </CompletionShell>
  );
}

export function JournalPage({ slug }: { slug?: string }) {
  const [query, setQuery] = useState("");
  const list = useAsync(() => api.getJournal({ query }), [query]);
  const detail = useAsync(() => (slug ? api.getJournalArticle(slug) : Promise.resolve(null)), [slug]);

  if (slug) {
    return <DataGate state={detail}>{(article) => article && <JournalDetail article={article} />}</DataGate>;
  }

  return (
    <CompletionShell id="PUB-11" eyebrow="Journal" title="Island stories and hosting guidance." copy="A database-backed journal with categories, featured articles, and responsive detail pages.">
      <section className="product-section">
        <div className="search-panel"><Field label="Search"><Input value={query} onChange={(event) => setQuery(event.target.value)} /></Field></div>
        <DataGate state={list}>
          {(articles) => (
            <div className="spec-card-grid spec-card-grid--three">
              {articles.map((article) => <ArticleCard article={article} key={article.id} />)}
            </div>
          )}
        </DataGate>
      </section>
    </CompletionShell>
  );
}

function ArticleCard({ article }: { article: JournalArticle }) {
  return (
    <Card className="spec-card">
      <BookOpen size={22} />
      <Badge tone="slate">{article.category}</Badge>
      <h3>{article.title}</h3>
      <p>{article.summary}</p>
      <AppLink className={buttonClassName("outline")} href={`/journal/${article.slug}`}>Read article</AppLink>
    </Card>
  );
}

function JournalDetail({ article }: { article: JournalArticle }) {
  return (
    <CompletionShell id="PUB-11" eyebrow={article.category} title={article.title} copy={`${article.author} - ${new Date(article.publishedAt).toLocaleDateString()}`}>
      <section className="product-section">
        <Card className="article-body-card">
          <p>{article.body}</p>
          <div className="highlight-list">{article.tags.map((tag) => <span key={tag}>{tag}</span>)}</div>
        </Card>
      </section>
    </CompletionShell>
  );
}

export function BookingSpecStatePage({ state, auth, bookingId }: { state: string; auth: AuthController; bookingId?: string }) {
  const bookings = useAsync(() => auth.session ? api.getBookings(auth.session.accessToken) : Promise.resolve([]), [auth.session?.accessToken, state]);
  const [notice, setNotice] = useState<string | null>(null);
  const booking = useMemo(() => bookings.data?.find((item) => item.id === bookingId) ?? bookings.data?.[0] ?? null, [bookings.data, bookingId]);
  const ids: Record<string, string> = { review: "BOOK-02", checkout: "BOOK-03", success: "BOOK-04", failure: "BOOK-05", rejected: "BOOK-06", pending: "BOOK-07", cancelled: "BOOK-08", invoice: "BOOK-09", receipt: "BOOK-10" };

  async function capture() {
    if (!booking) return;
    if (!auth.session) throw new Error("A signed host or admin session is required.");
    const updated = await api.capturePayment(booking.id, auth.session.accessToken);
    setNotice(`Payment status: ${updated.paymentStatus}`);
    bookings.reload();
  }

  async function downloadDocument(kind: "invoice" | "receipt") {
    if (!booking) return;
    if (!auth.session) throw new Error("A signed session is required.");
    const file = kind === "invoice"
      ? await api.downloadBookingInvoice(booking.id, auth.session.accessToken)
      : await api.downloadBookingReceipt(booking.id, auth.session.accessToken);
    const url = URL.createObjectURL(file.blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = file.fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    setNotice(`${kind === "invoice" ? "Invoice" : "Receipt"} ready: ${file.fileName}`);
  }

  return (
    <CompletionShell id={ids[state] ?? "BOOK-01"} eyebrow="Booking flow" title={`Booking ${state}`} copy="Connected booking/payment state page using persisted booking data.">
      <DataGate state={bookings}>
        {() => booking ? (
          <section className="product-section management-layout">
            <Card className="payment-card">
              <Badge tone={booking.status === "APPROVED" ? "green" : booking.status === "REJECTED" ? "coral" : "sun"}>{booking.status}</Badge>
              <h2>{booking.propertyTitle}</h2>
              <p>{booking.checkIn} to {booking.checkOut} - {booking.nights} nights</p>
              <strong>{formatMoney(booking.totalAmount, booking.currency)}</strong>
              <div className="spec-table-wrap">
                <table className="spec-table"><tbody>{booking.priceBreakdown.map((line) => <tr key={line.code}><td>{line.description}</td><td>{formatMoney(line.amount, line.currency)}</td></tr>)}</tbody></table>
              </div>
              {state === "checkout" && <Button onClick={capture}><CreditCard size={17} /> Confirm and capture</Button>}
              {state === "invoice" && <Button onClick={() => downloadDocument("invoice")}><Download size={17} /> Download invoice</Button>}
              {state === "receipt" && <Button onClick={() => downloadDocument("receipt")}><Download size={17} /> Download receipt</Button>}
              {notice && <div className="notice-panel">{notice}</div>}
            </Card>
            <Card className="settings-card">
              <PatoisPhrase phrase={state === "pending" ? "Nuh Fret" : state === "success" ? "Irie!" : state === "failure" ? "Dutty Tough" : state === "cancelled" ? "Suh It Guh" : "Straight!"} translation="The matching English confirmation stays visible in the standard copy." />
              <h3>Status timeline</h3>
              <ul>{booking.timeline.map((item, index) => <li key={`${item}-${index}`}>{item}</li>)}</ul>
            </Card>
          </section>
        ) : <EmptyState title="No booking is available yet." action={<AppLink className={buttonClassName("sun")} href="/explore">Create a booking</AppLink>} />}
      </DataGate>
    </CompletionShell>
  );
}

export function TravelerSpecPage({ view, auth }: { view: string; auth: AuthController }) {
  return (
    <RequireSession auth={auth}>
      {(session) => <TravelerWorkspaceView view={view} userId={session.userId} token={session.accessToken} />}
    </RequireSession>
  );
}

function TravelerWorkspaceView({ view, userId, token }: { view: string; userId: string; token: string }) {
  const workspace = useAsync(() => api.getTravelerWorkspace(userId, token), [userId, token]);
  const bookings = useAsync(() => api.getBookings(token), [token]);

  return (
    <CompletionShell id={travelerScreenId(view)} eyebrow="Traveler portal" title={travelerTitle(view)} copy="Dedicated traveler route connected to persisted traveler APIs.">
      <DataGate state={workspace}>
        {(data) => (
          <section className="product-section">
            {view.includes("reservation") || view === "qr" ? <ReservationPanel bookings={bookings.data ?? []} view={view} /> : null}
            {view === "wishlist" || view === "collections" ? <WishlistPanel data={data} userId={userId} token={token} reload={workspace.reload} /> : null}
            {view === "payment-methods" ? <PaymentMethodsPanel data={data} userId={userId} token={token} reload={workspace.reload} /> : null}
            {view === "payment-history" ? <PaymentHistoryPanel bookings={bookings} token={token} /> : null}
            {view === "invoices" ? <InvoiceListPanel bookings={bookings} token={token} /> : null}
            {view === "preferences" || view === "profile" ? <PreferencesPanel /> : null}
            {view === "identity" ? <IdentityPanel data={data} userId={userId} token={token} reload={workspace.reload} /> : null}
            {view === "reviews-given" || view === "reviews-pending" ? <ReviewsPanel data={data} userId={userId} token={token} reload={workspace.reload} /> : null}
            {view === "notifications" ? <NotificationsPanel data={data} userId={userId} token={token} reload={workspace.reload} /> : null}
          </section>
        )}
      </DataGate>
    </CompletionShell>
  );
}

type BookingDocumentDownload = {
  blob: Blob;
  fileName: string;
};

type PaymentHistoryRow = {
  id: string;
  bookingId: string;
  label: string;
  propertyTitle: string;
  reference: string;
  amount: number;
  currency: string;
  status: "AUTHORIZED" | "CAPTURED" | "REFUNDED";
  canDownloadReceipt: boolean;
};

function ReservationPanel({ bookings, view }: { bookings: Booking[]; view: string }) {
  const filtered = bookings.filter((booking) =>
    view === "reservations-cancelled" ? booking.status === "REJECTED" :
    view === "reservations-past" ? booking.paymentStatus === "CAPTURED" :
    true,
  );
  return filtered.length === 0 ? <EmptyState title="No reservations found." /> : (
    <div className="compact-list">{filtered.map((booking) => (
      <Card className="compact-list__item" key={booking.id}>
        <CalendarDays size={20} />
        <div><strong>{booking.propertyTitle}</strong><span>{booking.checkIn} - {booking.checkOut}</span></div>
        <Badge tone={booking.status === "APPROVED" ? "green" : "sun"}>{booking.status}</Badge>
        <AppLink className={buttonClassName("outline")} href={`/booking/${booking.id}/invoice`}>Invoice</AppLink>
      </Card>
    ))}</div>
  );
}

function PaymentHistoryPanel({ bookings, token }: { bookings: AsyncState<Booking[]>; token: string }) {
  const [status, setStatus] = useState("all");
  const rows = useMemo(() => {
    const transactions = (bookings.data ?? []).flatMap((booking) => buildPaymentRows(booking));
    return status === "all" ? transactions : transactions.filter((item) => item.status === status);
  }, [bookings.data, status]);

  return (
    <DataGate state={bookings}>
      {() => (
        <>
          <div className="search-panel spec-filter-bar">
            <Field label="Status">
              <Select value={status} onChange={(event) => setStatus(event.target.value)}>
                <option value="all">All</option>
                <option value="AUTHORIZED">Authorized</option>
                <option value="CAPTURED">Captured</option>
                <option value="REFUNDED">Refunded</option>
              </Select>
            </Field>
          </div>
          {rows.length === 0 ? (
            <EmptyState title="No payment activity yet." />
          ) : (
            <div className="compact-list">
              {rows.map((row) => (
                <Card className="compact-list__item" key={row.id}>
                  <CreditCard size={20} />
                  <div>
                    <strong>{row.label}</strong>
                    <span>{row.propertyTitle} - {row.reference}</span>
                  </div>
                  <Badge tone={row.status === "REFUNDED" ? "coral" : row.status === "CAPTURED" ? "green" : "sun"}>{row.status}</Badge>
                  <strong>{row.amount < 0 ? "-" : ""}{formatMoney(Math.abs(row.amount), row.currency)}</strong>
                  {row.canDownloadReceipt && (
                    <Button variant="outline" onClick={() => downloadBookingDocument(() => api.downloadBookingReceipt(row.bookingId, token))}>
                      <Download size={16} /> Receipt
                    </Button>
                  )}
                </Card>
              ))}
            </div>
          )}
        </>
      )}
    </DataGate>
  );
}

function InvoiceListPanel({ bookings, token }: { bookings: AsyncState<Booking[]>; token: string }) {
  const [year, setYear] = useState("all");
  const years = useMemo(() => {
    const values = new Set((bookings.data ?? []).map((booking) => booking.checkIn.slice(0, 4)));
    return Array.from(values).sort().reverse();
  }, [bookings.data]);
  const invoices = useMemo(() => {
    const all = bookings.data ?? [];
    return year === "all" ? all : all.filter((booking) => booking.checkIn.startsWith(year));
  }, [bookings.data, year]);

  async function downloadVisible() {
    for (const booking of invoices) {
      await downloadBookingDocument(() => api.downloadBookingInvoice(booking.id, token));
    }
  }

  return (
    <DataGate state={bookings}>
      {() => (
        <>
          <div className="search-panel spec-filter-bar">
            <Field label="Year">
              <Select value={year} onChange={(event) => setYear(event.target.value)}>
                <option value="all">All</option>
                {years.map((item) => <option key={item} value={item}>{item}</option>)}
              </Select>
            </Field>
            <Button variant="dark" disabled={invoices.length === 0} onClick={downloadVisible}>
              <Download size={17} /> Download visible
            </Button>
          </div>
          {invoices.length === 0 ? (
            <EmptyState title="No invoices for this filter." />
          ) : (
            <div className="compact-list">
              {invoices.map((booking) => (
                <Card className="compact-list__item" key={booking.id}>
                  <FileText size={20} />
                  <div>
                    <strong>NST-{booking.checkIn.slice(0, 4)}-{booking.id.slice(0, 8).toUpperCase()}</strong>
                    <span>{booking.propertyTitle ?? booking.propertyId} - {booking.checkIn} to {booking.checkOut}</span>
                  </div>
                  <Badge tone={booking.paymentStatus === "REFUNDED" ? "coral" : booking.paymentStatus === "CAPTURED" ? "green" : "sun"}>{booking.paymentStatus}</Badge>
                  <strong>{formatMoney(booking.totalAmount, booking.currency)}</strong>
                  <Button variant="outline" onClick={() => downloadBookingDocument(() => api.downloadBookingInvoice(booking.id, token))}>
                    <Download size={16} /> Invoice
                  </Button>
                </Card>
              ))}
            </div>
          )}
        </>
      )}
    </DataGate>
  );
}

function WishlistPanel({ data, userId, token, reload }: { data: TravelerWorkspace; userId: string; token: string; reload: () => void }) {
  const [name, setName] = useState("");
  async function add() {
    await api.createWishlistCollection(userId, token, { name: name || "New Collection" });
    setName("");
    reload();
  }
  return (
    <>
      <div className="search-panel"><Field label="Collection"><Input value={name} onChange={(event) => setName(event.target.value)} /></Field><Button onClick={add}>Create collection</Button></div>
      <div className="spec-card-grid">{data.wishlistCollections.map((collection) => <Card key={collection.id}><h3>{collection.name}</h3><p>{collection.items.length} saved stays</p><div className="compact-list">{collection.items.map((item) => <span key={item.id}>{item.propertyTitle} - {item.status}</span>)}</div></Card>)}</div>
    </>
  );
}

function PaymentMethodsPanel({ data, userId, token, reload }: { data: TravelerWorkspace; userId: string; token: string; reload: () => void }) {
  const [isAdding, setIsAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function addCard() {
    setIsAdding(true);
    setError(null);
    try {
      const setupIntent = await api.createPaymentMethodSetupIntent(userId, token);
      await api.addPaymentMethod(userId, token, { setupIntentReference: setupIntent.setupIntentReference, isDefault: data.paymentMethods.length === 0 });
      reload();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Payment method could not be saved.");
    } finally {
      setIsAdding(false);
    }
  }
  return (
    <>
      {error && <ErrorState message={error} />}
      <Button disabled={isAdding} onClick={addCard}><CreditCard size={17} /> {isAdding ? "Preparing secure setup" : "Add secure card"}</Button>
      <div className="compact-list">{data.paymentMethods.map((method) => <Card className="compact-list__item" key={method.id}><CreditCard size={20} /><div><strong>{method.brand} ending {method.last4}</strong><span>{method.expMonth}/{method.expYear}</span></div><Badge tone={method.isDefault ? "green" : "slate"}>{method.isDefault ? "Default" : "Saved"}</Badge></Card>)}</div>
    </>
  );
}

function PreferencesPanel() {
  return <Card className="settings-card"><PatoisToggle /><Field label="Currency"><Select defaultValue="USD"><option>USD</option><option>JMD</option></Select></Field><Field label="Communication"><Select defaultValue="email"><option value="email">Email</option><option value="sms">SMS</option></Select></Field></Card>;
}

type IdentityUploadStatus = "queued" | "uploading" | "uploaded" | "failed" | "cancelled";

type IdentityUploadItem = {
  id: string;
  file: File;
  progress: number;
  status: IdentityUploadStatus;
  upload?: IdentityDocumentUpload;
  error?: string;
};

const maximumIdentityDocumentBytes = 10 * 1024 * 1024;

function IdentityPanel({ data, userId, token, reload }: { data: TravelerWorkspace; userId: string; token: string; reload: () => void }) {
  const [documentType, setDocumentType] = useState("Passport");
  const [issuingCountry, setIssuingCountry] = useState("JM");
  const [expiresOn, setExpiresOn] = useState("");
  const [uploads, setUploads] = useState<IdentityUploadItem[]>([]);
  const [notice, setNotice] = useState<string | null>(null);
  const uploadControllers = useRef<Record<string, AbortController>>({});
  const identityDocuments = data.identityDocuments ?? [];

  useEffect(() => () => {
    Object.values(uploadControllers.current).forEach((controller) => controller.abort());
  }, []);

  function updateUpload(id: string, patch: Partial<IdentityUploadItem>) {
    setUploads((items) => items.map((item) => item.id === id ? { ...item, ...patch } : item));
  }

  async function uploadFile(id: string, file: File) {
    setNotice(null);
    if (file.size > maximumIdentityDocumentBytes) {
      updateUpload(id, { status: "failed", error: "Identity documents must be 10 MB or smaller." });
      return;
    }

    const controller = new AbortController();
    uploadControllers.current[id] = controller;

    try {
      const contentType = resolveMessageAttachmentContentType(file);
      const prepared = await api.prepareIdentityDocumentUpload(userId, token, {
        documentType,
        fileName: file.name,
        contentType,
        sizeBytes: file.size,
        issuingCountry,
        expiresOn: expiresOn || null,
      });
      updateUpload(id, { upload: prepared, progress: 5, status: "uploading", error: undefined });
      const uploaded = await api.uploadIdentityDocumentContent(userId, prepared.id, token, file, {
        signal: controller.signal,
        onProgress: (progress) => updateUpload(id, { progress, status: "uploading" }),
      });
      updateUpload(id, { upload: uploaded, progress: 100, status: "uploaded", error: undefined });
      setNotice(`${uploaded.fileName} uploaded and verified.`);
      reload();
    } catch (caught) {
      updateUpload(id, {
        status: controller.signal.aborted ? "cancelled" : "failed",
        error: caught instanceof Error ? caught.message : "Identity document upload failed.",
      });
    } finally {
      delete uploadControllers.current[id];
    }
  }

  function addFiles(files: FileList | null) {
    if (!files?.length) return;
    Array.from(files).forEach((file) => {
      const id = createUploadId();
      const isTooLarge = file.size > maximumIdentityDocumentBytes;
      setUploads((items) => [...items, {
        id,
        file,
        progress: 0,
        status: isTooLarge ? "failed" : "queued",
        error: isTooLarge ? "Identity documents must be 10 MB or smaller." : undefined,
      }]);
      if (!isTooLarge) {
        void uploadFile(id, file);
      }
    });
  }

  function cancelUpload(id: string) {
    uploadControllers.current[id]?.abort();
    updateUpload(id, { status: "cancelled", error: "Identity document upload cancelled." });
  }

  function retryUpload(item: IdentityUploadItem) {
    updateUpload(item.id, { upload: undefined, progress: 0, status: "queued", error: undefined });
    void uploadFile(item.id, item.file);
  }

  function removeUpload(id: string) {
    uploadControllers.current[id]?.abort();
    setUploads((items) => items.filter((item) => item.id !== id));
  }

  return (
    <Card className="settings-card identity-document-card">
      <ShieldCheck size={28} />
      <h3>Identity verification</h3>
      <p>Alibaba eKYC status: Verified / Pending / Action required. Re-verification launches through the protected booking and auth flow.</p>
      <div className="form-grid form-grid--two">
        <Field label="Document type">
          <Select value={documentType} onChange={(event) => setDocumentType(event.target.value)}>
            <option value="Passport">Passport</option>
            <option value="DriverLicense">Driver license</option>
            <option value="NationalId">National ID</option>
          </Select>
        </Field>
        <Field label="Issuing country">
          <Input maxLength={2} value={issuingCountry} onChange={(event) => setIssuingCountry(event.target.value.toUpperCase())} />
        </Field>
        <Field label="Expires on" className="form-grid__full">
          <Input type="date" value={expiresOn} onChange={(event) => setExpiresOn(event.target.value)} />
        </Field>
      </div>
      <div className="message-upload-bar">
        <label className={buttonClassName("outline", "message-file-picker")}>
          <Paperclip size={17} /> Upload document
          <input accept="image/jpeg,image/png,image/webp,application/pdf" multiple onChange={(event) => { addFiles(event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
        </label>
      </div>
      {uploads.length > 0 && (
        <div className="message-upload-list">
          {uploads.map((upload) => (
            <div className="message-upload-item" key={upload.id}>
              <FileText size={16} />
              <div>
                <strong>{upload.file.name}</strong>
                <small>{upload.status === "uploading" ? `${upload.progress}%` : upload.error ?? upload.upload?.scanStatus ?? upload.status}</small>
                <div className="message-upload-progress"><span style={{ width: `${upload.status === "uploaded" ? 100 : upload.progress}%` }} /></div>
              </div>
              {(upload.status === "uploading" || upload.status === "queued") && <Button onClick={() => cancelUpload(upload.id)} title="Cancel upload" variant="ghost"><X size={16} /></Button>}
              {(upload.status === "failed" || upload.status === "cancelled") && <Button onClick={() => retryUpload(upload)} title="Retry upload" variant="ghost"><RotateCcw size={16} /></Button>}
              {upload.status !== "uploading" && <Button onClick={() => removeUpload(upload.id)} title="Remove document" variant="ghost"><X size={16} /></Button>}
            </div>
          ))}
        </div>
      )}
      {identityDocuments.length > 0 && (
        <div className="compact-list">
          {identityDocuments.map((document) => (
            <div className="compact-list__item identity-document-row" key={document.id}>
              <FileText size={18} />
              <div>
                <strong>{document.documentType}</strong>
                <span>{document.fileName} · {document.scanStatus}</span>
              </div>
              <Badge tone="green">{document.status}</Badge>
            </div>
          ))}
        </div>
      )}
      {notice && <div className="notice-panel">{notice}</div>}
    </Card>
  );
}

function ReviewsPanel({ data, userId, token, reload }: { data: TravelerWorkspace; userId: string; token: string; reload: () => void }) {
  async function submit() {
    await api.submitReview(userId, token, { subjectTitle: "Ocho Rios Verified Villa", propertyId: "11111111-1111-4111-8111-111111111111", rating: 5, text: "Straight! Verified stay and clear check-in." });
    reload();
  }
  return <><Button onClick={submit}>Submit review</Button><div className="compact-list">{data.reviews.map((review) => <Card className="compact-list__item" key={review.id}><Star size={18} /><div><strong>{review.subjectTitle}</strong><span>{review.text}</span></div><Badge tone="green">{review.rating}/5</Badge></Card>)}</div></>;
}

function NotificationsPanel({ data, userId, token, reload }: { data: TravelerWorkspace; userId: string; token: string; reload: () => void }) {
  async function readAll() {
    await api.markAllNotificationsRead(userId, token);
    reload();
  }
  return <><Button onClick={readAll}><Bell size={17} /> Mark all as read</Button><div className="compact-list">{data.notifications.map((item) => <Card className={item.isRead ? "compact-list__item" : "compact-list__item is-unread"} key={item.id}><Bell size={18} /><div><strong>{item.title}</strong><span>{item.body}</span></div><AppLink href={item.deepLink}>Open</AppLink></Card>)}</div></>;
}

function travelerScreenId(view: string) {
  const map: Record<string, string> = { wishlist: "TRAV-07", collections: "TRAV-08", "payment-methods": "TRAV-09", "payment-history": "TRAV-10", invoices: "TRAV-11", profile: "TRAV-12", identity: "TRAV-13", preferences: "TRAV-14", "reviews-given": "TRAV-15", "reviews-pending": "TRAV-16", notifications: "TRAV-16", "reservations-upcoming": "TRAV-03", "reservations-past": "TRAV-04", "reservations-cancelled": "TRAV-05", "reservation-detail": "TRAV-06", qr: "TRAV-06" };
  return map[view] ?? "TRAV-01";
}

function travelerTitle(view: string) {
  return view.replace(/-/g, " ").replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function buildPaymentRows(booking: Booking): PaymentHistoryRow[] {
  const rows: PaymentHistoryRow[] = [];
  if (booking.paymentAuthorizationReference) {
    rows.push({
      id: `${booking.id}-authorization`,
      bookingId: booking.id,
      label: "Authorization",
      propertyTitle: booking.propertyTitle ?? booking.propertyId,
      reference: booking.paymentAuthorizationReference,
      amount: booking.totalAmount,
      currency: booking.currency,
      status: "AUTHORIZED",
      canDownloadReceipt: false,
    });
  }

  if (booking.paymentCaptureReference) {
    rows.push({
      id: `${booking.id}-capture`,
      bookingId: booking.id,
      label: "Payment received",
      propertyTitle: booking.propertyTitle ?? booking.propertyId,
      reference: booking.paymentCaptureReference,
      amount: booking.totalAmount,
      currency: booking.currency,
      status: "CAPTURED",
      canDownloadReceipt: booking.paymentStatus === "CAPTURED" || booking.paymentStatus === "REFUNDED",
    });
  }

  if (booking.paymentRefundReference && (booking.refundedAmount ?? 0) > 0) {
    rows.push({
      id: `${booking.id}-refund`,
      bookingId: booking.id,
      label: "Refund issued",
      propertyTitle: booking.propertyTitle ?? booking.propertyId,
      reference: booking.paymentRefundReference,
      amount: -booking.refundedAmount,
      currency: booking.currency,
      status: "REFUNDED",
      canDownloadReceipt: false,
    });
  }

  return rows;
}

async function downloadBookingDocument(load: () => Promise<BookingDocumentDownload>) {
  const file = await load();
  const url = URL.createObjectURL(file.blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = file.fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function MessagesPage({ auth, conversationId }: { auth: AuthController; conversationId?: string }) {
  return <RequireSession auth={auth}>{(session) => <MessagesWorkspace userId={session.userId} token={session.accessToken} conversationId={conversationId} />}</RequireSession>;
}

function MessagesWorkspace({ userId, token, conversationId }: { userId: string; token: string; conversationId?: string }) {
  const inbox = useAsync(() => api.getInbox(userId, token), [userId, token]);
  const selectedId = conversationId ?? inbox.data?.conversations[0]?.id;
  const conversation = useAsync(() => selectedId ? api.getConversation(selectedId, userId, token) : Promise.resolve(null), [selectedId, userId, token]);
  return (
    <CompletionShell id="MSG-01-09" eyebrow="Messaging" title="Platform messaging." copy="Persisted inbox, conversation threads, read receipts, attachments, support thread, and polling-ready data.">
      <section className="product-section message-layout">
        <DataGate state={inbox}>
          {(data) => <Card className="thread-list">{data.conversations.map((item) => <AppLink href={`/messages/${item.id}`} key={item.id}><strong>{item.participantLabel}</strong><span>{item.lastMessage}</span><Badge tone={item.unreadCount ? "sun" : "slate"}>{item.unreadCount}</Badge></AppLink>)}</Card>}
        </DataGate>
        <DataGate state={conversation}>
          {(data) => data ? <ConversationPanel conversation={data} userId={userId} token={token} reload={conversation.reload} /> : <EmptyState title="Your inbox is clear." />}
        </DataGate>
      </section>
    </CompletionShell>
  );
}

type MessageUploadStatus = "queued" | "uploading" | "uploaded" | "failed" | "cancelled";

type MessageUploadItem = {
  id: string;
  file: File;
  progress: number;
  status: MessageUploadStatus;
  attachment?: AttachmentUpload;
  error?: string;
};

const maximumMessageAttachmentBytes = 10 * 1024 * 1024;

function ConversationPanel({ conversation, userId, token, reload }: { conversation: Conversation; userId: string; token: string; reload: () => void }) {
  const [body, setBody] = useState("");
  const [uploads, setUploads] = useState<MessageUploadItem[]>([]);
  const [notice, setNotice] = useState("");
  const uploadControllers = useRef<Record<string, AbortController>>({});

  useEffect(() => () => {
    Object.values(uploadControllers.current).forEach((controller) => controller.abort());
  }, []);

  function updateUpload(id: string, patch: Partial<MessageUploadItem>) {
    setUploads((items) => items.map((item) => item.id === id ? { ...item, ...patch } : item));
  }

  async function uploadFile(id: string, file: File) {
    setNotice("");

    if (file.size > maximumMessageAttachmentBytes) {
      updateUpload(id, { status: "failed", error: "Attachments must be 10 MB or smaller." });
      return;
    }

    const controller = new AbortController();
    uploadControllers.current[id] = controller;

    try {
      const contentType = resolveMessageAttachmentContentType(file);
      const prepared = await api.prepareMessageAttachmentUpload(conversation.id, userId, token, {
        fileName: file.name,
        contentType,
        sizeBytes: file.size,
      });
      updateUpload(id, { attachment: prepared, progress: 5, status: "uploading" });
      const uploaded = await api.uploadMessageAttachmentContent(conversation.id, prepared.id, userId, token, file, {
        signal: controller.signal,
        onProgress: (progress) => updateUpload(id, { progress, status: "uploading" }),
      });
      updateUpload(id, { attachment: uploaded, progress: 100, status: "uploaded", error: undefined });
    } catch (error) {
      updateUpload(id, {
        status: controller.signal.aborted ? "cancelled" : "failed",
        error: error instanceof Error ? error.message : "Attachment upload failed.",
      });
    } finally {
      delete uploadControllers.current[id];
    }
  }

  function addFiles(files: FileList | null) {
    if (!files?.length) return;
    Array.from(files).forEach((file) => {
      const id = createUploadId();
      const isTooLarge = file.size > maximumMessageAttachmentBytes;
      setUploads((items) => [...items, {
        id,
        file,
        progress: 0,
        status: isTooLarge ? "failed" : "queued",
        error: isTooLarge ? "Attachments must be 10 MB or smaller." : undefined,
      }]);
      if (!isTooLarge) {
        void uploadFile(id, file);
      }
    });
  }

  function cancelUpload(id: string) {
    uploadControllers.current[id]?.abort();
    updateUpload(id, { status: "cancelled", error: "Attachment upload cancelled." });
  }

  function retryUpload(item: MessageUploadItem) {
    updateUpload(item.id, { attachment: undefined, progress: 0, status: "queued", error: undefined });
    void uploadFile(item.id, item.file);
  }

  function removeUpload(id: string) {
    uploadControllers.current[id]?.abort();
    setUploads((items) => items.filter((item) => item.id !== id));
  }

  async function downloadAttachment(file: MessageAttachment) {
    if (!file.attachmentId) return;
    try {
      const download = await api.getMessageAttachmentDownload(conversation.id, file.attachmentId, userId, token);
      window.open(download.url, "_blank", "noopener,noreferrer");
    } catch (error) {
      setNotice(error instanceof Error ? error.message : "Attachment download failed.");
    }
  }

  async function send() {
    const attachments = uploads
      .filter((upload): upload is MessageUploadItem & { attachment: AttachmentUpload } => upload.status === "uploaded" && Boolean(upload.attachment))
      .map((upload) => ({
        attachmentId: upload.attachment.id,
        fileName: upload.attachment.fileName,
        contentType: upload.attachment.contentType,
        sizeBytes: upload.attachment.sizeBytes,
        url: null,
        status: upload.attachment.status,
        objectKey: upload.attachment.objectKey,
        expiresAt: upload.attachment.expiresAt,
        scanStatus: upload.attachment.scanStatus,
        thumbnailUrl: upload.attachment.thumbnailUrl,
      }));

    try {
      await api.sendMessage(conversation.id, userId, token, { body, attachments });
      setBody("");
      setUploads([]);
      setNotice("");
      reload();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : "Message could not be sent.");
    }
  }

  const hasActiveUploads = uploads.some((upload) => upload.status === "queued" || upload.status === "uploading");
  const canSend = body.trim().length > 0 && !hasActiveUploads;

  return (
    <Card className="message-thread">
      <h3>{conversation.subject}</h3>
      {conversation.messages.map((message) => <div className={message.senderUserId === userId ? "message-bubble message-bubble--reply" : "message-bubble"} key={message.id}><p>{message.body}</p><small>{message.status} - {new Date(message.sentAt).toLocaleTimeString()}</small>{message.attachments.map((file) => <button className="message-attachment-link" disabled={!file.attachmentId} key={file.attachmentId ?? file.fileName} onClick={() => void downloadAttachment(file)} type="button"><FileText size={14} /> <span>{file.fileName}</span><small>{file.scanStatus ?? file.status}</small></button>)}</div>)}
      <Field label="Message"><Textarea value={body} onChange={(event) => setBody(event.target.value)} /></Field>
      <div className="message-upload-bar">
        <label className={buttonClassName("outline", "message-file-picker")}>
          <Paperclip size={17} /> Attach
          <input accept="image/jpeg,image/png,image/webp,application/pdf" multiple onChange={(event) => { addFiles(event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
        </label>
        <Button disabled={!canSend} onClick={send}><MessageSquare size={17} /> Send</Button>
      </div>
      {uploads.length > 0 && (
        <div className="message-upload-list">
          {uploads.map((upload) => (
            <div className="message-upload-item" key={upload.id}>
              <FileText size={16} />
              <div>
                <strong>{upload.file.name}</strong>
                <small>{upload.status === "uploading" ? `${upload.progress}%` : upload.error ?? upload.status}</small>
                <div className="message-upload-progress"><span style={{ width: `${upload.status === "uploaded" ? 100 : upload.progress}%` }} /></div>
              </div>
              {(upload.status === "uploading" || upload.status === "queued") && <Button onClick={() => cancelUpload(upload.id)} title="Cancel upload" variant="ghost"><X size={16} /></Button>}
              {(upload.status === "failed" || upload.status === "cancelled") && <Button onClick={() => retryUpload(upload)} title="Retry upload" variant="ghost"><RotateCcw size={16} /></Button>}
              {upload.status !== "uploading" && <Button onClick={() => removeUpload(upload.id)} title="Remove attachment" variant="ghost"><X size={16} /></Button>}
            </div>
          ))}
        </div>
      )}
      {notice && <div className="notice-panel">{notice}</div>}
    </Card>
  );
}

function createUploadId() {
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

function resolveMessageAttachmentContentType(file: File) {
  if (file.type) return file.type;
  const name = file.name.toLowerCase();
  if (name.endsWith(".pdf")) return "application/pdf";
  if (name.endsWith(".png")) return "image/png";
  if (name.endsWith(".webp")) return "image/webp";
  if (name.endsWith(".jpg") || name.endsWith(".jpeg")) return "image/jpeg";
  return "application/octet-stream";
}

export function DirectorySpecPage({ kind, slug, auth }: { kind?: string; slug?: string; auth: AuthController }) {
  const list = useAsync(() => kind === "Provider" || kind === "ProviderDashboard" ? Promise.resolve([]) : api.getDirectoryProviders({ kind }), [kind]);
  const detail = useAsync(() => slug ? api.getDirectoryProvider(slug) : Promise.resolve(null), [slug]);
  if (slug) return <DataGate state={detail}>{(provider) => provider && <ProviderDetail provider={provider} />}</DataGate>;
  if (kind === "Provider" || kind === "ProviderDashboard") {
    return <RequireSession auth={auth}>{(session) => <ProviderPortal session={session} mode={kind} />}</RequireSession>;
  }

  return (
    <CompletionShell id={kind === "Custodian" ? "DIR-01" : kind === "Trades" ? "DIR-02" : kind === "Verification" ? "DIR-06" : "DIR-03"} eyebrow="Directory" title={`${kind ?? "Local business"} directory`} copy="Provider cards, filters, platform messaging, onboarding, and persisted profile data.">
      <section className="product-section">
        <div className="search-panel"><Button variant="dark"><SlidersHorizontal size={17} /> Filters</Button><AppLink className={buttonClassName("outline")} href="/directory/provider/onboarding">Provider onboarding</AppLink><AppLink className={buttonClassName("ghost")} href="/directory/provider">Provider dashboard</AppLink></div>
        <DataGate state={list}>{(providers) => <div className="spec-card-grid spec-card-grid--three">{providers.map((provider) => <ProviderCard provider={provider} key={provider.id} />)}</div>}</DataGate>
      </section>
    </CompletionShell>
  );
}

function ProviderPortal({ session, mode }: { session: NonNullable<AuthController["session"]>; mode: string }) {
  const slug = `provider-${session.userId.slice(0, 8)}`;
  const provider = useAsync(() => api.getDirectoryProvider(slug).catch(() => null), [slug]);
  const [form, setForm] = useState({
    name: provider.data?.name ?? `${session.displayName} Services`,
    kind: "LocalBusiness",
    category: provider.data?.category ?? "Host services",
    parish: provider.data?.parish ?? "Kingston",
    badgeLevel: provider.data?.badgeLevel ?? "Verified",
    description: provider.data?.description ?? "Platform-approved provider profile with services, availability, requests, and messaging.",
    availabilitySummary: provider.data?.availabilitySummary ?? "Mon-Fri 8 AM-5 PM",
    contactMode: provider.data?.contactMode ?? "Platform messaging only",
    isActive: provider.data?.isActive ?? true,
  });
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!provider.data) return;
    setForm({
      name: provider.data.name,
      kind: provider.data.kind,
      category: provider.data.category,
      parish: provider.data.parish,
      badgeLevel: provider.data.badgeLevel,
      description: provider.data.description,
      availabilitySummary: provider.data.availabilitySummary,
      contactMode: provider.data.contactMode,
      isActive: provider.data.isActive,
    });
  }, [provider.data]);

  function update<K extends keyof typeof form>(key: K, value: (typeof form)[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    setNotice(null);
    setError(null);
    try {
      const saved = await api.upsertDirectoryProvider(session.accessToken, { slug, ...form });
      setNotice(`${saved.name} is saved and ${saved.isActive ? "visible" : "paused"} in the directory.`);
      provider.reload();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Provider profile could not be saved.");
    }
  }

  return (
    <CompletionShell
      id={mode === "Provider" ? "DIR-04" : "DIR-05"}
      eyebrow="Provider directory"
      title={mode === "Provider" ? "Provider onboarding." : "Provider dashboard."}
      copy="Providers manage profile details, service category, availability, contact mode, active status, badges, and platform-only requests with persisted backend data."
    >
      <section className="product-section management-layout">
        <form className="management-form" onSubmit={save}>
          <div className="form-grid form-grid--two">
            <Field label="Business name"><Input value={form.name} onChange={(event) => update("name", event.target.value)} /></Field>
            <Field label="Provider type">
              <Select value={form.kind} onChange={(event) => update("kind", event.target.value)}>
                <option value="Custodian">Custodian</option>
                <option value="Trades">Trades</option>
                <option value="LocalBusiness">Local business</option>
              </Select>
            </Field>
            <Field label="Category"><Input value={form.category} onChange={(event) => update("category", event.target.value)} /></Field>
            <Field label="Parish"><Input value={form.parish} onChange={(event) => update("parish", event.target.value)} /></Field>
            <Field label="Badge level">
              <Select value={form.badgeLevel} onChange={(event) => update("badgeLevel", event.target.value)}>
                <option>Free</option>
                <option>Verified</option>
                <option>Trusted</option>
              </Select>
            </Field>
            <Field label="Availability"><Input value={form.availabilitySummary} onChange={(event) => update("availabilitySummary", event.target.value)} /></Field>
            <Field label="Description" className="form-grid__full"><Textarea value={form.description} onChange={(event) => update("description", event.target.value)} /></Field>
            <Field label="Contact mode" className="form-grid__full"><Input value={form.contactMode} onChange={(event) => update("contactMode", event.target.value)} /></Field>
          </div>
          <InlineLabel><input checked={form.isActive} type="checkbox" onChange={(event) => update("isActive", event.target.checked)} /> Visible in directory</InlineLabel>
          <Button type="submit"><BadgeCheck size={17} /> Save provider profile</Button>
          {notice && <div className="notice-panel">{notice}</div>}
          {error && <ErrorState message={error} />}
        </form>
        <Card className="settings-card">
          <CalendarDays size={24} />
          <h2>Service controls</h2>
          <p>Availability, active status, badge level, and platform-only contact are stored on the provider profile.</p>
          <div className="highlight-list">
            <span>Requests: platform messages</span>
            <span>Services: {form.category}</span>
            <span>Status: {form.isActive ? "Active" : "Paused"}</span>
            <span>Badge: {form.badgeLevel}</span>
          </div>
          <AppLink className={buttonClassName("outline")} href={`/directory/providers/${slug}`}>Preview public listing</AppLink>
        </Card>
      </section>
    </CompletionShell>
  );
}

function ProviderCard({ provider }: { provider: DirectoryProvider }) {
  return <Card className="spec-card"><Badge tone="green">{provider.badgeLevel}</Badge><h3>{provider.name}</h3><p>{provider.parish} - {provider.category}</p><p>{provider.description}</p><AppLink className={buttonClassName("outline")} href={`/directory/providers/${provider.slug}`}>View provider</AppLink></Card>;
}

function ProviderDetail({ provider }: { provider: DirectoryProvider }) {
  return <CompletionShell id="DIR-05" eyebrow={provider.kind} title={provider.name} copy={provider.description}><section className="product-section details-layout"><HeroImage index={3} /><Card className="settings-card"><Badge tone="green">{provider.badgeLevel}</Badge><p>{provider.availabilitySummary}</p><p>{provider.contactMode}</p><Button><MessageSquare size={17} /> Connect through platform</Button></Card></section></CompletionShell>;
}

export function HostProfileSpecPage({ slug, edit, auth }: { slug?: string; edit?: boolean; auth: AuthController }) {
  const profiles = useAsync<HostProfile[] | HostProfile>(() => slug ? api.getHostProfile(slug) : api.getHostProfiles(), [slug]);
  if (edit) {
    return <RequireSession auth={auth}>{(session) => <HostProfileEditor session={session} />}</RequireSession>;
  }
  return (
    <DataGate state={profiles}>
      {(data) => Array.isArray(data) ? (
        <CompletionShell id="HPRO-01" eyebrow="Host profiles" title="Verified host profiles." copy="Public host profile directory with badges, listings, reviews, and Link Mi contact routes.">
          <section className="product-section spec-card-grid">{data.map((profile) => <HostProfileCard profile={profile} key={profile.id} />)}</section>
        </CompletionShell>
      ) : <HostProfileDetail profile={data} />}
    </DataGate>
  );
}

function HostProfileCard({ profile }: { profile: HostProfile }) {
  return <Card className="spec-card"><UserRound size={22} /><h3>{profile.displayName}</h3><p>{profile.bio}</p><Badge tone="green">{profile.rating} - {profile.reviewCount} reviews</Badge><AppLink className={buttonClassName("outline")} href={`/hosts/${profile.slug}`}>Open profile</AppLink></Card>;
}

function HostProfileDetail({ profile }: { profile: HostProfile }) {
  return <CompletionShell id="HPRO-05" eyebrow="Host profile" title={profile.displayName} copy={profile.bio}><section className="product-section details-layout"><HeroImage index={1} /><Card className="settings-card"><PatoisPhrase phrase="Link Mi" translation="Contact me through platform messaging." /><Badge tone="green">{profile.badges.join(", ")}</Badge><p>{profile.parish} - {profile.responseTime}</p><div className="highlight-list">{profile.highlights.map((item) => <span key={item}>{item}</span>)}</div><AppLink className={buttonClassName("sun")} href="/messages">Contact host</AppLink></Card></section></CompletionShell>;
}

function HostProfileEditor({ session }: { session: NonNullable<AuthController["session"]> }) {
  const [notice, setNotice] = useState<string | null>(null);
  async function save() {
    await api.updateHostProfile("my-host-profile", session.accessToken, { hostUserId: session.userId, displayName: session.displayName, parish: "St. Ann", bio: "Host profile managed by NestyStay.", responseTime: "Replies in 10 minutes", badges: ["Verified"], listingIds: [], isPublic: true, highlights: ["Verified host"] });
    setNotice("Host profile saved.");
  }
  return <CompletionShell id="HPRO-04" eyebrow="Host profile edit" title="Edit host profile." copy="Host biography, badges, privacy, preview, and Link Mi visibility settings."><section className="product-section"><Card className="settings-card"><Field label="Display name"><Input defaultValue={session.displayName} /></Field><Field label="Bio"><Textarea defaultValue="Host profile managed by NestyStay." /></Field><InlineLabel><input defaultChecked type="checkbox" /> Public profile visible</InlineLabel><Button onClick={save}>Save profile</Button>{notice && <div className="notice-panel">{notice}</div>}</Card></section></CompletionShell>;
}

export function HostSpecPage({ view, auth }: { view: string; auth: AuthController }) {
  return <RequireSession auth={auth}>{(session) => <HostOps view={view} hostUserId={session.userId} token={session.accessToken} />}</RequireSession>;
}

function HostOps({ view, hostUserId, token }: { view: string; hostUserId: string; token: string }) {
  const ops = useAsync(() => api.getHostOperations(hostUserId, token), [hostUserId, token]);
  return (
    <CompletionShell id={hostScreenId(view)} eyebrow="Host portal" title={travelerTitle(view)} copy="Host-facing analytics, seasonal pricing, promotions, exports, reviews, badges, settings, and archive controls.">
      <DataGate state={ops}>{(data) => <HostOpsPanel view={view} data={data} hostUserId={hostUserId} token={token} reload={ops.reload} />}</DataGate>
    </CompletionShell>
  );
}

function HostOpsPanel({ view, data, hostUserId, token, reload }: { view: string; data: HostOperations; hostUserId: string; token: string; reload: () => void }) {
  async function addPricing() {
    await api.saveHostPricingRule(hostUserId, token, { propertyId: "11111111-1111-4111-8111-111111111111", name: "Carnival Weekend", startsOn: "2026-04-10", endsOn: "2026-04-16", nightlyRate: 260, minimumStay: 3, isActive: true });
    reload();
  }
  async function addPromotion() {
    await api.saveHostPromotion(hostUserId, token, { propertyId: "11111111-1111-4111-8111-111111111111", name: "Early Yaad Deal", discountPercent: 10, startsOn: "2026-08-01", endsOn: "2026-09-01", minimumNights: 2, badgeLevel: "Trusted", isActive: true });
    reload();
  }
  if (view === "analytics") return <MetricCards items={[["Revenue", formatMoney(data.analytics.revenue)], ["Occupancy", `${data.analytics.occupancyPercent}%`], ["ADR", formatMoney(data.analytics.averageNightlyRate)], ["Bookings", String(data.analytics.bookingCount)]]} />;
  if (view === "pricing") return <><Button onClick={addPricing}>Add seasonal rule</Button><Table rows={data.pricingRules.map((item) => [item.name, item.startsOn, item.endsOn, formatMoney(item.nightlyRate), `${item.minimumStay} nights`])} /></>;
  if (view === "promotions") return <><Button onClick={addPromotion}>Create promotion</Button><Table rows={data.promotions.map((item) => [item.name, `${item.discountPercent}%`, item.startsOn, item.endsOn, item.isActive ? "Active" : "Off"])} /></>;
  if (view === "reviews") return <ReviewsPanel data={{ userId: hostUserId, wishlistCollections: [], paymentMethods: [], identityDocuments: [], reviews: data.reviews, notifications: [] }} userId={hostUserId} token={token} reload={reload} />;
  return <MetricCards items={[["Badge progress", "Verified -> Trusted"], ["Exports", "CSV ready"], ["Archived properties", "0"], ["Notifications", "Enabled"]]} />;
}

function hostScreenId(view: string) {
  const map: Record<string, string> = { analytics: "HOST-02", pricing: "HOST-07", promotions: "HOST-08", exports: "HOST-11", reviews: "HOST-12", badges: "HOST-13", settings: "HOST-13", archived: "HOST-04" };
  return map[view] ?? "HOST-01";
}

function MetricCards({ items }: { items: [string, string][] }) {
  return <section className="product-section metric-grid">{items.map(([label, value]) => <Card className="metric-card" key={label}><span><LayoutDashboard size={20} /></span><small>{label}</small><strong>{value}</strong></Card>)}</section>;
}

function Table({ rows }: { rows: ReactNode[][] }) {
  return <div className="spec-table-wrap"><table className="spec-table"><tbody>{rows.map((row, index) => <tr key={index}>{row.map((cell, cellIndex) => <td key={cellIndex}>{cell}</td>)}</tr>)}</tbody></table></div>;
}

export function AdminOpsSpecPage({ view }: { view: string }) {
  const [token, setToken] = useState("");
  const ops = useAsync(() => api.getAdminOperations(token), [token, view]);
  return (
    <CompletionShell id={adminScreenId(view)} eyebrow="Admin operations" title={travelerTitle(view)} copy="Admin queues, sensitive actions, mandatory reasons, and audit log entries are persisted.">
      <section className="product-section">
        <Field label="Admin token"><Input value={token} onChange={(event) => setToken(event.target.value)} /></Field>
        <DataGate state={ops}>{(data) => <AdminOpsPanel data={data} token={token} reload={ops.reload} view={view} />}</DataGate>
      </section>
    </CompletionShell>
  );
}

function AdminOpsPanel({ data, token, reload, view }: { data: AdminOperations; token: string; reload: () => void; view: string }) {
  const [caseToResolve, setCaseToResolve] = useState<AdminCase | null>(null);
  async function create() {
    await api.createAdminCase(token, { caseType: travelerTitle(view), subjectType: "User", priority: "Normal", reason: "Manual admin review opened from UI.", assignedTo: "Ops" });
    reload();
  }
  async function resolve() {
    if (!caseToResolve) return;
    await api.resolveAdminCase(token, caseToResolve.id, { resolutionNotes: "Reviewed and resolved with audit record." });
    setCaseToResolve(null);
    reload();
  }
  return (
    <>
      <MetricCards items={data.metrics.map((item) => [item.label, item.value]) as [string, string][]} />
      <Button onClick={create}>Create admin case</Button>
      <Table rows={data.cases.map((item) => [item.caseType, item.priority, item.status, item.reason, <Button key={item.id} onClick={() => setCaseToResolve(item)} variant="outline">Resolve</Button>])} />
      <h3 className="section-subtitle">Audit log</h3>
      <Table rows={data.auditEvents.slice(0, 10).map((item) => [item.action, item.subjectType, item.reason, new Date(item.createdAt).toLocaleString()])} />
      <Modal open={Boolean(caseToResolve)} title="Resolve admin case" onClose={() => setCaseToResolve(null)}>
        <p>Every sensitive action requires a reason and writes an audit record.</p>
        <Button onClick={resolve}>Confirm resolution</Button>
      </Modal>
    </>
  );
}

function adminScreenId(view: string) {
  const map: Record<string, string> = { users: "ADM-03", moderation: "ADM-04", reservations: "ADM-05", payments: "ADM-06", disputes: "ADM-07", support: "ADM-08", reports: "ADM-09", fraud: "ADM-10", flagged: "ADM-11", audit: "ADM-11" };
  return map[view] ?? "ADM-01";
}
