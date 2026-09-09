import { useEffect, useRef, useState } from "react";
import { Check, ChevronLeft, ChevronRight, Camera, Save } from "lucide-react";
import { api } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { PropertyWizardData } from "./types";
import { ActionBar } from "../../components/ui/ActionBar";

interface HostPropertyWizardProps {
  token: string;
  hostUserId: string;
  hostName: string;
  hostEmail: string;
  onFinished: () => void;
}

const DRAFT_KEY_PREFIX = "nesty.property-draft.";
const MAX_PHOTO_SIZE = 10 * 1024 * 1024;

function getDraftKey(hostUserId: string) {
  return `${DRAFT_KEY_PREFIX}${hostUserId}`;
}

function readDraft(hostUserId: string): { step: number; data: PropertyWizardData } | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(getDraftKey(hostUserId));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { step?: number; data?: PropertyWizardData };
    if (!parsed.data || typeof parsed.data.title !== "string") return null;
    return {
      step: Math.min(10, Math.max(1, Number(parsed.step) || 1)),
      data: parsed.data
    };
  } catch {
    return null;
  }
}

function getInitialData(hostUserId: string): { step: number; data: PropertyWizardData } {
  const defaultData: PropertyWizardData = {
    title: "Jamaican Coastal Villa",
    location: "Montego Bay, St. James",
    country: "Jamaica",
    propertyType: "Villa",
    capacityAdults: 4,
    capacityChildren: 2,
    bedrooms: 3,
    bathrooms: 2,
    nightlyRate: 220,
    currency: "USD",
    amenities: ["WiFi", "Swimming Pool", "Air Conditioning", "Ocean View", "Security Gate"],
    description: "Beautiful beachfront villa overlooking Montego Bay with private pool and full security.",
    houseRules: "No smoking indoors. Quiet hours after 10 PM. No unauthorized parties.",
    minimumNights: 2,
    photos: [
      { id: "p1", url: "https://images.unsplash.com/photo-1540555700478-4be289fbecef", isCover: true, sortOrder: 0 }
    ],
    cancellationPolicy: "Flexible",
    verificationEnabled: false,
    insuraGuestEnabled: true
  };
  return readDraft(hostUserId) ?? { step: 1, data: defaultData };
}

function getCompleteness(data: PropertyWizardData) {
  const checks = [
    Boolean(data.title.trim()),
    Boolean(data.location.trim() && data.country.trim()),
    Boolean(data.propertyType),
    data.capacityAdults > 0 && data.bedrooms > 0 && data.bathrooms > 0,
    data.amenities.length > 0,
    data.photos.length > 0,
    Boolean(data.description.trim()),
    data.nightlyRate > 0,
    Boolean(data.houseRules.trim()),
    data.minimumNights > 0
  ];
  return Math.round((checks.filter(Boolean).length / checks.length) * 100);
}

function readImage(file: File) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(new Error("Could not read this image."));
    reader.readAsDataURL(file);
  });
}

export function HostPropertyWizard({ token, hostUserId, hostName, hostEmail, onFinished }: HostPropertyWizardProps) {
  const initial = getInitialData(hostUserId);
  const [currentStep, setCurrentStep] = useState(initial.step);
  const [savingDraft, setSavingDraft] = useState(false);
  const [draftSavedToast, setDraftSavedToast] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [locationSuggestions, setLocationSuggestions] = useState<string[]>([]);
  const photoInputRef = useRef<HTMLInputElement>(null);
  const [formData, setFormData] = useState<PropertyWizardData>(initial.data);
  const completeness = getCompleteness(formData);

  const steps = [
    "Basics", "Location", "Type", "Capacity", "Amenities", 
    "Photos", "Description", "Pricing", "Availability", "Verification & Publish"
  ];

  function persistDraft() {
    if (typeof window === "undefined") return;
    try {
      window.localStorage.setItem(getDraftKey(hostUserId), JSON.stringify({ step: currentStep, data: formData, savedAt: new Date().toISOString() }));
    } catch {
      setError("This draft is too large for local storage. Remove a few photos and try again.");
    }
  }

  function handleAutosave() {
    setSavingDraft(true);
    persistDraft();
    window.setTimeout(() => {
      setSavingDraft(false);
      setDraftSavedToast(true);
      window.setTimeout(() => setDraftSavedToast(false), 2000);
    }, 400);
  }

  useEffect(() => {
    const timer = window.setTimeout(persistDraft, 700);
    return () => window.clearTimeout(timer);
  }, [currentStep, formData, hostUserId]);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") setPreviewOpen(false);
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  useEffect(() => {
    const query = formData.location.trim();
    if (currentStep !== 2 || query.length < 3) {
      setLocationSuggestions([]);
      return;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      const geocoderUrl = (import.meta.env.VITE_GEOCODER_URL ?? "https://nominatim.openstreetmap.org/search").replace(/\/$/, "");
      void fetch(`${geocoderUrl}?format=jsonv2&addressdetails=1&limit=5&countrycodes=jm&q=${encodeURIComponent(query)}`, {
        headers: { Accept: "application/json" },
        signal: controller.signal,
      }).then(async (response) => {
        if (!response.ok) return [] as Array<{ display_name?: string }>;
        return await response.json() as Array<{ display_name?: string }>;
      }).then((results) => {
        setLocationSuggestions(results.map((item) => item.display_name?.trim()).filter((item): item is string => Boolean(item)));
      }).catch(() => {
        // Geocoder outages never block manual location entry.
        setLocationSuggestions([]);
      });
    }, 350);

    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [currentStep, formData.location]);

  async function handlePhotoFiles(files: FileList | File[]) {
    setPhotoError(null);
    const accepted = Array.from(files).filter((file) => file.type.startsWith("image/") && file.size <= MAX_PHOTO_SIZE);
    const rejected = Array.from(files).length - accepted.length;
    if (rejected > 0) setPhotoError(`${rejected} file${rejected === 1 ? " was" : "s were"} skipped. Use JPG, PNG, or WebP images up to 10MB.`);
    if (!accepted.length) return;
    try {
      const newPhotos = await Promise.all(accepted.map(async (file, index) => ({
        id: `photo-${Date.now()}-${index}`,
        url: await readImage(file),
        isCover: false,
        sortOrder: formData.photos.length + index
      })));
      setFormData((current) => ({
        ...current,
        photos: [...current.photos, ...newPhotos]
      }));
    } catch {
      setPhotoError("One or more images could not be read. Please try again.");
    }
  }

  function setCoverPhoto(photoId: string) {
    setFormData((current) => ({
      ...current,
      photos: current.photos.map((photo, index) => ({ ...photo, isCover: photo.id === photoId, sortOrder: index }))
    }));
  }

  function removePhoto(photoId: string) {
    setFormData((current) => {
      const photos = current.photos.filter((photo) => photo.id !== photoId).map((photo, index) => ({ ...photo, sortOrder: index }));
      if (photos.length > 0 && !photos.some((photo) => photo.isCover)) photos[0] = { ...photos[0], isCover: true };
      return { ...current, photos };
    });
  }

  async function handlePublish() {
    setPublishing(true);
    setError(null);
    try {
      await api.createProperty({
        hostUserId,
        hostName,
        hostEmail,
        title: formData.title,
        location: formData.location,
        country: formData.country,
        nightlyRate: formData.nightlyRate,
        currency: formData.currency,
        badgeLevel: "Free",
        cancellationPolicy: formData.cancellationPolicy,
        guestVerificationEnabled: formData.verificationEnabled,
        insuraGuestEnabled: formData.insuraGuestEnabled,
        highlights: formData.amenities
      }, token);
      if (typeof window !== "undefined") window.localStorage.removeItem(getDraftKey(hostUserId));
      onFinished();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to publish property.");
    } finally {
      setPublishing(false);
    }
  }

  return (
    <div className="page-container container py-6" data-testid="host-05-page" id="HOST-05">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">HOST-05</span>
          <h2>10-Step Property Wizard</h2>
          <PatoisPhrase phrase="Build Yuh Yard Listing Step-by-Step" translation="Complete all 10 required listing steps with draft autosave." />
        </div>
        <div className="flex gap-2 flex-wrap justify-end">
          <button type="button" className="btn btn-ghost" onClick={() => setPreviewOpen(true)} data-testid="preview-listing-button">Preview listing</button>
          <button type="button" className="btn btn-outline" onClick={handleAutosave} disabled={savingDraft}>
            <Save size={16} /> {savingDraft ? "Saving..." : "Save Draft"}
          </button>
        </div>
      </header>

      {draftSavedToast && <div className="notice-panel mb-4" role="status">Draft saved on this device. You can safely return to it later.</div>}
      {error && <div className="alert-box alert-error mb-4">{error}</div>}

      <section className="card-box mb-6" aria-label="Listing completeness">
        <div className="flex justify-between items-center gap-3 mb-2">
          <div><strong>Listing completeness</strong><span className="subtext ml-2">Finish each section before publishing</span></div>
          <strong>{completeness}%</strong>
        </div>
        <div className="progress-track" aria-hidden="true"><div className="progress-fill" style={{ width: `${completeness}%` }} /></div>
      </section>

      {/* Stepper Progress Bar */}
      <div className="wizard-stepper-bar flex justify-between items-center gap-1 mb-8 overflow-x-auto pb-2">
        {steps.map((label, idx) => {
          const stepNum = idx + 1;
          const isActive = stepNum === currentStep;
          const isComplete = stepNum < currentStep;
          return (
            <button
              type="button"
              aria-current={isActive ? "step" : undefined}
              aria-label={`Step ${stepNum}: ${label}`}
              key={label} 
              className={`stepper-pill flex items-center gap-1 text-xs px-3 py-2 rounded-full cursor-pointer transition ${
                isActive ? "bg-sun text-white font-bold" : isComplete ? "bg-green-light text-green" : "bg-gray-100 text-gray-500"
              }`}
              onClick={() => setCurrentStep(stepNum)}
            >
              <span>{isComplete ? <Check size={12} /> : stepNum}</span>
              <span className="hidden sm:inline">{label}</span>
            </button>
          );
        })}
      </div>

      {/* Step Content Container */}
      <div className="card-box wizard-step-card max-w-3xl mx-auto p-6 mb-6">
        <h3 className="text-xl font-bold mb-4">Step {currentStep}: {steps[currentStep - 1]}</h3>

        {currentStep === 1 && (
          <div className="space-y-4">
            <div className="field-group">
              <label className="field-label" htmlFor="wizard-property-title">Property Title</label>
              <input id="wizard-property-title" type="text" className="input-control" value={formData.title} onChange={(e) => setFormData({ ...formData, title: e.target.value })} />
            </div>
          </div>
        )}

        {currentStep === 2 && (
          <div className="space-y-4">
            <div className="field-group">
              <label className="field-label" htmlFor="wizard-property-location">Location / Parish</label>
              <div className="relative">
                <input id="wizard-property-location" type="text" autoComplete="address-level2" className="input-control" value={formData.location} onChange={(e) => setFormData({ ...formData, location: e.target.value })} aria-autocomplete="list" aria-controls="wizard-location-suggestions" />
                {locationSuggestions.length > 0 && <ul id="wizard-location-suggestions" className="absolute z-20 mt-1 max-h-56 w-full overflow-auto rounded-field border border-sand-border bg-white p-1 shadow-lg" role="listbox" aria-label="Location suggestions">
                  {locationSuggestions.map((suggestion) => <li key={suggestion}><button className="w-full rounded-field px-3 py-2 text-left text-sm hover:bg-shell focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-deep-hover" onClick={() => { setFormData({ ...formData, location: suggestion }); setLocationSuggestions([]); }} role="option" type="button">{suggestion}</button></li>)}
                </ul>}
              </div>
              <small className="text-xs text-sand-600">Search suggestions use an OpenStreetMap-compatible geocoder. You can always enter the parish manually.</small>
            </div>
            <div className="field-group">
              <label className="field-label">Country</label>
              <input type="text" className="input-control" value={formData.country} readOnly />
            </div>
          </div>
        )}

        {currentStep === 3 && (
          <div className="space-y-4">
            <div className="field-group">
              <label className="field-label">Property Category</label>
              <select className="input-control" value={formData.propertyType} onChange={(e) => setFormData({ ...formData, propertyType: e.target.value })}>
                <option value="Villa">Coastal Villa</option>
                <option value="Apartment">City Apartment</option>
                <option value="Cottage">Mountain Cottage</option>
                <option value="House">Residential House</option>
              </select>
            </div>
          </div>
        )}

        {currentStep === 4 && (
          <div className="grid grid-cols-2 gap-4">
            <div className="field-group">
              <label className="field-label">Adults Capacity</label>
              <input type="number" className="input-control" value={formData.capacityAdults} onChange={(e) => setFormData({ ...formData, capacityAdults: parseInt(e.target.value) || 1 })} />
            </div>
            <div className="field-group">
              <label className="field-label">Bedrooms</label>
              <input type="number" className="input-control" value={formData.bedrooms} onChange={(e) => setFormData({ ...formData, bedrooms: parseInt(e.target.value) || 1 })} />
            </div>
          </div>
        )}

        {currentStep === 5 && (
          <div>
            <label className="field-label mb-2">Amenities</label>
            <div className="grid grid-cols-2 gap-2">
              {["WiFi", "Swimming Pool", "Air Conditioning", "Security Gate", "Ocean View", "Kitchen"].map((a) => (
                <label key={a} className="checkbox-card">
                  <input 
                    type="checkbox" 
                    checked={formData.amenities.includes(a)} 
                    onChange={(e) => {
                      if (e.target.checked) setFormData({ ...formData, amenities: [...formData.amenities, a] });
                      else setFormData({ ...formData, amenities: formData.amenities.filter(x => x !== a) });
                    }} 
                  />
                  <span>{a}</span>
                </label>
              ))}
            </div>
          </div>
        )}

        {currentStep === 6 && (
          <div>
            <label className="field-label mb-2" htmlFor="property-photo-input">Property Photos & Cover Photo Selection</label>
            <div
              className="photo-upload-dropzone p-6 border-2 border-dashed rounded text-center mb-4"
              data-testid="photo-dropzone"
              onDragOver={(event) => event.preventDefault()}
              onDrop={(event) => { event.preventDefault(); void handlePhotoFiles(event.dataTransfer.files); }}
            >
              <Camera size={32} className="mx-auto mb-2 text-sun" />
              <p className="mb-2">Drag and drop photos here, or choose files (JPG, PNG, WebP up to 10MB)</p>
              <input ref={photoInputRef} id="property-photo-input" type="file" accept="image/jpeg,image/png,image/webp" multiple className="sr-only" onChange={(event) => { if (event.target.files) void handlePhotoFiles(event.target.files); event.target.value = ""; }} />
              <button type="button" className="btn btn-outline" onClick={() => photoInputRef.current?.click()}>Choose photos</button>
            </div>
            {photoError && <div className="alert-box alert-error mb-4" role="alert">{photoError}</div>}
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3" aria-label="Uploaded photos">
              {formData.photos.map((photo) => (
                <figure key={photo.id} className={`relative rounded overflow-hidden border ${photo.isCover ? "border-sun" : "border-gray-200"}`}>
                  <img src={photo.url} alt={photo.isCover ? "Cover photo" : "Property photo"} className="w-full h-28 object-cover" />
                  <figcaption className="p-2 text-xs flex items-center justify-between gap-1">
                    <button type="button" className="text-link" onClick={() => setCoverPhoto(photo.id)} disabled={photo.isCover}>{photo.isCover ? "Cover photo" : "Make cover"}</button>
                    <button type="button" className="text-link text-danger" onClick={() => removePhoto(photo.id)} aria-label={`Remove ${photo.isCover ? "cover " : ""}photo`}>Remove</button>
                  </figcaption>
                </figure>
              ))}
            </div>
          </div>
        )}

        {currentStep === 7 && (
          <div className="space-y-4">
            <div className="field-group">
              <label className="field-label">Detailed Description</label>
              <textarea className="input-control" rows={4} value={formData.description} onChange={(e) => setFormData({ ...formData, description: e.target.value })} />
            </div>
            <div className="field-group">
              <label className="field-label">House Rules</label>
              <textarea className="input-control" rows={3} value={formData.houseRules} onChange={(e) => setFormData({ ...formData, houseRules: e.target.value })} />
            </div>
          </div>
        )}

        {currentStep === 8 && (
          <div className="grid grid-cols-2 gap-4">
            <div className="field-group">
              <label className="field-label">Nightly Rate ($ USD)</label>
              <input type="number" className="input-control" value={formData.nightlyRate} onChange={(e) => setFormData({ ...formData, nightlyRate: parseFloat(e.target.value) || 100 })} />
            </div>
            <div className="field-group">
              <label className="field-label">Cancellation Policy</label>
              <select className="input-control" value={formData.cancellationPolicy} onChange={(e) => setFormData({ ...formData, cancellationPolicy: e.target.value })}>
                <option value="Flexible">Flexible</option>
                <option value="Moderate">Moderate</option>
                <option value="Strict">Strict</option>
              </select>
            </div>
          </div>
        )}

        {currentStep === 9 && (
          <div className="space-y-4">
            <div className="field-group">
              <label className="field-label">Minimum Stay (Nights)</label>
              <input type="number" className="input-control" value={formData.minimumNights} onChange={(e) => setFormData({ ...formData, minimumNights: parseInt(e.target.value) || 1 })} />
            </div>
          </div>
        )}

        {currentStep === 10 && (
          <div className="space-y-4">
            <label className="checkbox-card">
              <input type="checkbox" checked={formData.verificationEnabled} onChange={(e) => setFormData({ ...formData, verificationEnabled: e.target.checked })} />
              <span>Enable Alibaba Cloud eKYC verification for guests</span>
            </label>
            <label className="checkbox-card">
              <input type="checkbox" checked={formData.insuraGuestEnabled} onChange={(e) => setFormData({ ...formData, insuraGuestEnabled: e.target.checked })} />
              <span>Include InsuraGuest damage protection option</span>
            </label>
          </div>
        )}
      </div>

      {/* Stepper Navigation Controls */}
      <ActionBar className="wizard-footer mx-auto max-w-3xl justify-between" sticky>
        <button 
          type="button" 
          className="btn btn-ghost" 
          disabled={currentStep === 1}
          onClick={() => setCurrentStep(currentStep - 1)}
        >
          <ChevronLeft size={16} /> Previous Step
        </button>

        {currentStep < 10 ? (
          <button 
            type="button" 
            className="btn btn-primary"
            onClick={() => { handleAutosave(); setCurrentStep(currentStep + 1); }}
          >
            Next Step <ChevronRight size={16} />
          </button>
        ) : (
          <button 
            type="button" 
            className="btn btn-primary" 
            disabled={publishing}
            onClick={handlePublish}
          >
            {publishing ? "Publishing Property..." : "Publish Listing"}
          </button>
        )}
      </ActionBar>

      {previewOpen && (
        <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setPreviewOpen(false); }}>
          <section className="modal-card max-w-2xl" role="dialog" aria-modal="true" aria-labelledby="listing-preview-title">
            <div className="flex justify-between items-start gap-3 mb-4">
              <div><span className="badge badge-sun">Preview</span><h3 id="listing-preview-title">{formData.title || "Untitled listing"}</h3><p className="subtext">{formData.location}, {formData.country}</p></div>
              <button type="button" className="btn btn-ghost" onClick={() => setPreviewOpen(false)} aria-label="Close listing preview">Close</button>
            </div>
            {formData.photos.find((photo) => photo.isCover) && <img src={formData.photos.find((photo) => photo.isCover)?.url} alt="Listing cover preview" className="w-full h-56 object-cover rounded mb-4" />}
            <div className="grid grid-cols-2 gap-3 text-sm mb-4"><div><strong>From</strong><br />${formData.nightlyRate} {formData.currency} / night</div><div><strong>Guests</strong><br />Up to {formData.capacityAdults + formData.capacityChildren}</div><div><strong>Stay</strong><br />{formData.bedrooms} bedrooms · {formData.bathrooms} bathrooms</div><div><strong>Policy</strong><br />{formData.cancellationPolicy} cancellation</div></div>
            <p>{formData.description || "Add a description to help guests understand your stay."}</p>
          </section>
        </div>
      )}
    </div>
  );
}
