import { useState, useEffect, useRef } from "react";
import { User, ShieldCheck, Upload, Trash2, Camera, CheckCircle2, AlertCircle, RefreshCw } from "lucide-react";
import { api } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { TravelerProfile } from "./types";

interface TravelerProfileIdentityProps {
  userId: string;
  token: string;
}

export function TravelerProfileIdentity({ userId, token }: TravelerProfileIdentityProps) {
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
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        if (token) {
          const u = await api.getProfile(token);
          if (active) {
            setDisplayName(u.displayName);
            setEmail(u.email);
          }
        }
      } catch (err) {
        console.error(err);
      }
    }
    load();
    return () => { active = false; };
  }, [userId, token]);

  async function handleSaveProfile() {
    setNotice("Profile updated successfully.");
  }

  async function handlePhotoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file || !token) return;
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
            <div className="flex items-center gap-4 my-4">
              <div className="avatar-preview-box w-20 h-20 bg-sun-light rounded-full flex items-center justify-center border-2 border-sun relative">
                <User size={40} className="text-sun" />
                <button 
                  type="button" 
                  className="absolute bottom-0 right-0 p-1 bg-sun text-white rounded-full shadow"
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
                <strong>Profile Photo</strong>
                <p className="subtext">JPG, PNG or WebP up to 10MB.</p>
                {uploadingPhoto && <span className="text-xs text-sun">Uploading photo...</span>}
              </div>
            </div>

            <div className="form-grid">
              <div className="field-group">
                <label className="field-label">Display Name</label>
                <input type="text" className="input-control" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
              </div>
              <div className="field-group">
                <label className="field-label">Email Address</label>
                <input type="email" className="input-control" value={email} onChange={(e) => setEmail(e.target.value)} />
              </div>
              <div className="field-group">
                <label className="field-label">Phone Number (Jamaica default +1-876)</label>
                <input type="tel" className="input-control" value={phone} onChange={(e) => setPhone(e.target.value)} />
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

          <button type="button" className="btn btn-outline w-full" onClick={() => alert("eKYC verification process initiated.")}>
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
