import { useState, useEffect, useRef } from "react";
import { User, ShieldCheck, Upload, Trash2, Camera, CheckCircle2, AlertCircle, RefreshCw } from "lucide-react";
import { api } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { TravelerProfile } from "./types";

interface TravelerProfileIdentityProps {
  userId: string;
  token: string;
  sessionExpiresAt?: string;
  onLogout?: () => Promise<void>;
}

export function TravelerProfileIdentity({ userId, token, sessionExpiresAt, onLogout }: TravelerProfileIdentityProps) {
  const [profile, setProfile] = useState<TravelerProfile>({
    userId,
    displayName: "Traveler Guest",
    email: "guest@nestystay.local",
    phone: "+1-876-555-0199",
    patoisPreference: true,
    accessibilityPreferences: ["wheelchair"],
    notificationPreferences: { email: true, sms: false, push: true },
    identityStatus: "Verified",
    identityVerifiedAt: "2026-06-01",
    identityExpiresAt: "2027-06-01"
  });

  const [displayName, setDisplayName] = useState(profile.displayName);
  const [email, setEmail] = useState(profile.email);
  const [phone, setPhone] = useState(profile.phone || "");
  const [notice, setNotice] = useState<string | null>(null);
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [twoFactorEnabled, setTwoFactorEnabled] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const u = await api.getProfile(token);
        if (active) {
          setDisplayName(u.displayName);
          setEmail(u.email);
          setPhone(u.phone ?? "");
          setTwoFactorEnabled(u.isTwoFactorEnabled);
        }
      } catch (err) {
        console.error(err);
      }
    }
    load();
    return () => { active = false; };
  }, [userId, token]);

  async function handleSaveProfile() {
    setNotice(null);
    try {
      const updated = await api.updateProfile(token, { displayName, phone: phone || null });
      setProfile((current) => ({ ...current, displayName: updated.displayName, email: updated.email, phone }));
      setNotice("Profile updated successfully.");
    } catch (err) {
      setNotice(`Profile update failed: ${err instanceof Error ? err.message : "Error"}`);
    }
  }

  async function handlePhotoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploadingPhoto(true);
    setNotice(null);
    try {
      const prepared = await api.prepareProfilePhotoUpload(token, {
        fileName: file.name,
        contentType: file.type || "image/jpeg",
        sizeBytes: file.size
      });

      await api.uploadProfilePhotoContent(token, prepared.id, file);
      setNotice("Profile photo uploaded successfully.");
    } catch (err) {
      setNotice(`Photo upload failed: ${err instanceof Error ? err.message : "Error"}`);
    } finally {
      setUploadingPhoto(false);
    }
  }

  return (
    <div className="page-container container py-6" data-testid="trav-12-page" id="TRAV-12">
      <header className="page-header mb-6">
        <span className="badge badge-sun">TRAV-12 / TRAV-13 / TRAV-14</span>
        <h2>Profile & Identity Verification</h2>
        <PatoisPhrase phrase="Keep Yuh Info Fresh" translation="Manage your account profile, preferences, and Alibaba Cloud eKYC verification." />
      </header>

      <div className="layout-grid-2-1">
        {/* Main Settings Form */}
        <div className="space-y-6">
          {/* Profile Photo & Info */}
          <div className="card-box">
            <h3>Personal Information</h3>
            <div className="profile-photo-picker flex items-center gap-4 my-4">
              <div className="avatar-preview-box w-20 h-20 bg-sun-light rounded-full flex items-center justify-center border-2 border-sun relative">
                <User size={40} className="text-sun" />
                <button
                  type="button"
                  className="absolute bottom-0 right-0 p-1 bg-sun text-white rounded-full shadow"
                  aria-label="Upload profile photo"
                  onClick={() => fileInputRef.current?.click()}
                  title="Upload profile photo"
                >
                  <Camera size={14} />
                </button>
              </div>
              <input 
                type="file" 
                ref={fileInputRef} 
                className="hidden" 
                accept="image/jpeg,image/png,image/webp" 
                onChange={handlePhotoUpload} 
              />
              <div>
                <strong>Profile photo</strong>
                <p className="subtext">JPG, PNG or WebP up to 10MB.</p>
                {uploadingPhoto && <span className="text-xs text-sun">Uploading photo...</span>}
              </div>
            </div>

            <div className="form-grid">
              <div className="field-group">
                <label className="field-label" htmlFor="traveler-display-name">Display Name</label>
                <input id="traveler-display-name" type="text" className="input-control" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
              </div>
              <div className="field-group">
                <label className="field-label" htmlFor="traveler-email">Email Address</label>
                <input id="traveler-email" type="email" className="input-control" readOnly value={email} />
                <span className="subtext">Email changes require a verified account flow.</span>
              </div>
              <div className="field-group">
                <label className="field-label" htmlFor="traveler-phone">Phone Number (Jamaica default +1-876)</label>
                <input id="traveler-phone" type="tel" className="input-control" value={phone} onChange={(e) => setPhone(e.target.value)} />
              </div>
            </div>

            <button type="button" className="btn btn-primary mt-4" onClick={handleSaveProfile}>Save Changes</button>
            {notice && <div className="notice-panel mt-3">{notice}</div>}
          </div>

          {/* Preferences (TRAV-14) */}
          <div className="card-box" id="TRAV-14" data-testid="trav-14-preferences">
            <h3>Preferences & Language</h3>
            <label className="checkbox-card mb-3">
              <input 
                type="checkbox" 
                checked={profile.patoisPreference} 
                onChange={(e) => setProfile({ ...profile, patoisPreference: e.target.checked })} 
              />
              <span>Enable Jamaican Patois language subtitles & greetings</span>
            </label>

            <h4>Notification Preferences</h4>
            <div className="space-y-2 mt-2">
              <label className="checkbox-card">
                <input type="checkbox" checked={profile.notificationPreferences.email} onChange={(e) => setProfile({ ...profile, notificationPreferences: { ...profile.notificationPreferences, email: e.target.checked } })} />
                <span>Email Notifications for Booking Confirmations & Invoices</span>
              </label>
              <label className="checkbox-card">
                <input type="checkbox" checked={profile.notificationPreferences.sms} onChange={(e) => setProfile({ ...profile, notificationPreferences: { ...profile.notificationPreferences, sms: e.target.checked } })} />
                <span>SMS Alerts for Host Messages & Check-in Reminders</span>
              </label>
            </div>
          </div>

          <div className="card-box" aria-labelledby="session-security-heading">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h3 id="session-security-heading">Account security</h3>
                <p className="subtext">Review this device session before you leave a shared computer.</p>
              </div>
              <span className={`badge ${twoFactorEnabled ? "badge-green" : "badge-sun"}`}>{twoFactorEnabled ? "2FA enabled" : "2FA not enabled"}</span>
            </div>
            <div className="info-row mt-3">
              <span>Current session:</span>
              <span>This device</span>
            </div>
            <div className="info-row">
              <span>Session expires:</span>
              <span>{sessionExpiresAt ? new Date(sessionExpiresAt).toLocaleString() : "Not available"}</span>
            </div>
            {onLogout && (
              <button
                className="btn btn-outline mt-4"
                disabled={loggingOut}
                onClick={async () => {
                  if (!confirm("Sign out of this device?")) return;
                  setLoggingOut(true);
                  try { await onLogout(); window.location.assign("/"); } finally { setLoggingOut(false); }
                }}
                type="button"
              >
                {loggingOut ? "Signing out…" : "Sign out this device"}
              </button>
            )}
          </div>
        </div>

        {/* Identity Verification Sidebar (TRAV-13) */}
        <div className="card-box sticky-top" id="TRAV-13" data-testid="trav-13-identity">
          <h3>Alibaba eKYC Verification</h3>
          <div className="status-badge-row my-3">
            <span className="badge badge-green flex items-center gap-1">
              <ShieldCheck size={16} /> Verified Guest Status
            </span>
          </div>

          <div className="info-row">
            <span>Verified On:</span>
            <span>{profile.identityVerifiedAt}</span>
          </div>
          <div className="info-row">
            <span>Expires On:</span>
            <span>{profile.identityExpiresAt}</span>
          </div>

          <hr className="my-4" />

          <button type="button" className="btn btn-outline w-full" onClick={() => setNotice("Start verification from an eligible booking so the secure eKYC session is tied to that stay.")}>
            <RefreshCw size={16} /> Re-verify Document
          </button>

          <hr className="my-4" />

          <div className="danger-zone-box border-t pt-4">
            <h4 className="text-coral">Account Safety</h4>
            <p className="subtext mb-3">Permanently delete your NestyStay traveler account.</p>
            <button type="button" className="btn btn-ghost text-coral btn-sm" onClick={() => confirm("Are you sure you want to delete your account?")}>
              <Trash2 size={16} /> Delete Account
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
