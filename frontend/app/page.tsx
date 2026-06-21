"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5019";

type PropertyListing = {
  id: string;
  title: string;
  location: string;
  country: string;
  nightlyRate: number;
  currency: string;
  badgeLevel: string;
  guestVerificationEnabled: boolean;
  insuraGuestEnabled: boolean;
  cancellationPolicy: string;
  highlights: string[];
};

type RegisteredUser = {
  userId: string;
  email: string;
  displayName: string;
  requiresTwoFactor: boolean;
  twoFactorCode: string;
};

type LoginChallenge = {
  userId: string;
  email: string;
  requiresTwoFactor: boolean;
  challengeId: string;
};

type Booking = {
  id: string;
  propertyId: string;
  guestUserId: string;
  checkIn: string;
  checkOut: string;
  status: string;
  requiresGuestVerification: boolean;
  holdExpiresAt: string | null;
  totalAmount: number;
  currency: string;
  ekycProvider: string | null;
  paymentProvider: string | null;
  checkoutReference: string | null;
  timeline: string[];
};

const fallbackProperties: PropertyListing[] = [
  {
    id: "11111111-1111-4111-8111-111111111111",
    title: "Ocho Rios Verified Villa",
    location: "Ocho Rios, St. Ann",
    country: "Jamaica",
    nightlyRate: 185,
    currency: "USD",
    badgeLevel: "Verified",
    guestVerificationEnabled: true,
    insuraGuestEnabled: true,
    cancellationPolicy: "Moderate",
    highlights: ["Alibaba eKYC", "QR gate access", "InsuraGuest available", "Emergency 119 displayed"]
  }
];

export default function Home() {
  const [properties, setProperties] = useState<PropertyListing[]>(fallbackProperties);
  const [selectedProperty, setSelectedProperty] = useState<PropertyListing | null>(null);
  const [registeredUser, setRegisteredUser] = useState<RegisteredUser | null>(null);
  const [loginChallenge, setLoginChallenge] = useState<LoginChallenge | null>(null);
  const [activeUserId, setActiveUserId] = useState<string>("");
  const [booking, setBooking] = useState<Booking | null>(null);
  const [notice, setNotice] = useState("Phase 1 web flow is ready.");

  useEffect(() => {
    fetch(`${API_BASE_URL}/api/properties`)
      .then((response) => response.ok ? response.json() : fallbackProperties)
      .then(setProperties)
      .catch(() => setProperties(fallbackProperties));
  }, []);

  const statusTone = useMemo(() => {
    if (!booking) return "bg-[#e7f0ea] text-[#173f35]";
    if (booking.status === "Rejected") return "bg-[#fee2e2] text-[#7f1d1d]";
    if (booking.status === "Confirmed") return "bg-[#d1fae5] text-[#064e3b]";
    if (booking.status === "Approved") return "bg-[#dbeafe] text-[#1e3a8a]";
    return "bg-[#fef3c7] text-[#78350f]";
  }, [booking]);

  async function post<T>(path: string, body?: unknown): Promise<T> {
    const response = await fetch(`${API_BASE_URL}${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: body ? JSON.stringify(body) : undefined
    });

    if (!response.ok) {
      throw new Error(await response.text());
    }

    return response.json() as Promise<T>;
  }

  async function handleRegister(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const response = await post<RegisteredUser>("/api/auth/register", {
      email: form.get("email"),
      password: form.get("password"),
      displayName: form.get("displayName"),
      phone: form.get("phone")
    });
    setRegisteredUser(response);
    setActiveUserId(response.userId);
    setNotice(`Registered. Demo 2FA code: ${response.twoFactorCode}`);
  }

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const response = await post<LoginChallenge>("/api/auth/login", {
      email: form.get("email"),
      password: form.get("password")
    });
    setLoginChallenge(response);
    setNotice("Login accepted. Enter the 2FA code to activate the session.");
  }

  async function handleTwoFactor(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!loginChallenge) return;
    const form = new FormData(event.currentTarget);
    const response = await post<{ userId: string; accessToken: string }>("/api/auth/2fa/verify", {
      challengeId: loginChallenge.challengeId,
      code: form.get("code")
    });
    setActiveUserId(response.userId);
    setNotice("2FA verified. You can now book.");
  }

  async function handleCreateBooking(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedProperty) return;
    const guestUserId = activeUserId || registeredUser?.userId;
    if (!guestUserId) {
      setNotice("Register or log in before booking.");
      return;
    }

    const form = new FormData(event.currentTarget);
    const response = await post<Booking>("/api/bookings", {
      propertyId: selectedProperty.id,
      guestUserId,
      checkIn: form.get("checkIn"),
      checkOut: form.get("checkOut")
    });
    setBooking(response);
    setNotice("Booking created. Dates are held while verification runs.");
  }

  async function resolveVerification(passed: boolean) {
    if (!booking) return;
    const response = await post<Booking>(`/api/bookings/${booking.id}/verification-result`, { passed });
    setBooking(response);
    setNotice(passed ? "Alibaba eKYC approved. Stripe checkout is ready." : "Alibaba eKYC rejected. Dates released.");
  }

  async function capturePayment() {
    if (!booking) return;
    const response = await post<Booking>(`/api/bookings/${booking.id}/capture-payment`);
    setBooking(response);
    setNotice("Stripe payment processed. QR access released.");
  }

  return (
    <main className="min-h-screen bg-[#f8f7f3] text-[#17201b]">
      <section className="border-b border-[#d8d5cc] bg-[#fdfbf6]">
        <div className="mx-auto max-w-7xl px-5 py-8 lg:px-8">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <p className="text-sm font-bold uppercase tracking-[0.18em] text-[#b06a2c]">NestyStay Phase 1</p>
              <h1 className="mt-2 text-4xl font-black md:text-5xl">Registration, listings, eKYC booking, and Stripe flow.</h1>
            </div>
            <div className="rounded-md border border-[#d8d5cc] bg-white px-4 py-3 text-sm font-semibold text-[#36524b]">
              {notice}
            </div>
          </div>
        </div>
      </section>

      <section className="mx-auto grid max-w-7xl gap-5 px-5 py-6 lg:grid-cols-[360px_1fr] lg:px-8">
        <aside className="space-y-4">
          <form onSubmit={handleRegister} className="rounded-md border border-[#d8d5cc] bg-white p-4">
            <h2 className="text-lg font-black">Register</h2>
            <input className="mt-4 w-full rounded border border-[#d8d5cc] px-3 py-2" name="displayName" placeholder="Full name" defaultValue="Demo Guest" />
            <input className="mt-2 w-full rounded border border-[#d8d5cc] px-3 py-2" name="email" placeholder="Email" defaultValue="guest@nestystay.test" />
            <input className="mt-2 w-full rounded border border-[#d8d5cc] px-3 py-2" name="phone" placeholder="Phone" defaultValue="254-248-2435" />
            <input className="mt-2 w-full rounded border border-[#d8d5cc] px-3 py-2" name="password" type="password" placeholder="Password" defaultValue="NestyStay123!" />
            <button className="mt-3 w-full rounded bg-[#173f35] px-4 py-2 font-bold text-white">Create account</button>
          </form>

          <form onSubmit={handleLogin} className="rounded-md border border-[#d8d5cc] bg-white p-4">
            <h2 className="text-lg font-black">Login</h2>
            <input className="mt-4 w-full rounded border border-[#d8d5cc] px-3 py-2" name="email" placeholder="Email" defaultValue={registeredUser?.email ?? "guest@nestystay.test"} />
            <input className="mt-2 w-full rounded border border-[#d8d5cc] px-3 py-2" name="password" type="password" placeholder="Password" defaultValue="NestyStay123!" />
            <button className="mt-3 w-full rounded bg-[#36524b] px-4 py-2 font-bold text-white">Login</button>
          </form>

          <form onSubmit={handleTwoFactor} className="rounded-md border border-[#d8d5cc] bg-white p-4">
            <h2 className="text-lg font-black">2FA</h2>
            <input className="mt-4 w-full rounded border border-[#d8d5cc] px-3 py-2" name="code" placeholder="2FA code" defaultValue={registeredUser?.twoFactorCode ?? "246810"} />
            <button className="mt-3 w-full rounded bg-[#b06a2c] px-4 py-2 font-bold text-white">Verify 2FA</button>
          </form>
        </aside>

        <div className="space-y-5">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {properties.map((property) => (
              <article key={property.id} className="rounded-md border border-[#d8d5cc] bg-white p-5">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-xs font-bold uppercase tracking-[0.16em] text-[#b06a2c]">{property.badgeLevel}</p>
                    <h2 className="mt-1 text-xl font-black">{property.title}</h2>
                    <p className="mt-1 text-sm text-[#66726c]">{property.location}</p>
                  </div>
                  <p className="text-right text-lg font-black">${property.nightlyRate}</p>
                </div>
                <div className="mt-4 flex flex-wrap gap-2">
                  {property.highlights.map((highlight) => (
                    <span key={highlight} className="rounded border border-[#e2dfd6] px-2 py-1 text-xs text-[#52635e]">{highlight}</span>
                  ))}
                </div>
                <button onClick={() => { setSelectedProperty(property); setBooking(null); }} className="mt-5 w-full rounded bg-[#173f35] px-4 py-2 font-bold text-white">
                  Book Now
                </button>
              </article>
            ))}
          </div>
        </div>
      </section>

      {selectedProperty && (
        <div className="fixed inset-0 z-10 grid place-items-center bg-black/40 px-4">
          <section className="max-h-[92vh] w-full max-w-2xl overflow-auto rounded-md bg-white p-5 shadow-xl">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-sm font-bold uppercase tracking-[0.16em] text-[#b06a2c]">Book Now</p>
                <h2 className="text-2xl font-black">{selectedProperty.title}</h2>
                <p className="text-sm text-[#66726c]">{selectedProperty.location}</p>
              </div>
              <button onClick={() => setSelectedProperty(null)} className="rounded border border-[#d8d5cc] px-3 py-1 font-bold">Close</button>
            </div>

            <form onSubmit={handleCreateBooking} className="mt-5 grid gap-3 sm:grid-cols-2">
              <label className="text-sm font-bold">
                Check-in
                <input className="mt-1 w-full rounded border border-[#d8d5cc] px-3 py-2 font-normal" name="checkIn" type="date" defaultValue="2026-06-10" />
              </label>
              <label className="text-sm font-bold">
                Check-out
                <input className="mt-1 w-full rounded border border-[#d8d5cc] px-3 py-2 font-normal" name="checkOut" type="date" defaultValue="2026-06-13" />
              </label>
              <button className="rounded bg-[#173f35] px-4 py-2 font-bold text-white sm:col-span-2">
                Create pending booking
              </button>
            </form>

            {booking && (
              <div className="mt-5 rounded-md border border-[#d8d5cc] bg-[#fdfbf6] p-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="text-sm text-[#66726c]">Total</p>
                    <p className="text-2xl font-black">${booking.totalAmount.toFixed(2)} {booking.currency}</p>
                  </div>
                  <span className={`rounded px-3 py-2 text-sm font-black ${statusTone}`}>{booking.status}</span>
                </div>
                <div className="mt-4 grid gap-2 sm:grid-cols-3">
                  <button onClick={() => resolveVerification(true)} className="rounded bg-[#1e3a8a] px-3 py-2 font-bold text-white">Approve eKYC</button>
                  <button onClick={() => resolveVerification(false)} className="rounded bg-[#7f1d1d] px-3 py-2 font-bold text-white">Reject eKYC</button>
                  <button onClick={capturePayment} className="rounded bg-[#064e3b] px-3 py-2 font-bold text-white">Process Stripe</button>
                </div>
                <ol className="mt-4 space-y-2">
                  {booking.timeline.map((item, index) => (
                    <li key={`${item}-${index}`} className="rounded border border-[#e2dfd6] bg-white px-3 py-2 text-sm">
                      {index + 1}. {item}
                    </li>
                  ))}
                </ol>
                {booking.checkoutReference && (
                  <p className="mt-3 rounded bg-[#e7f0ea] px-3 py-2 text-sm font-semibold text-[#173f35]">
                    Stripe reference: {booking.checkoutReference}
                  </p>
                )}
              </div>
            )}
          </section>
        </div>
      )}
    </main>
  );
}
