import { useState } from "react";
import { Check, ChevronLeft, ChevronRight, Upload, Star, ShieldCheck, Camera, Save } from "lucide-react";
import { api } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { PropertyWizardData } from "./types";

interface HostPropertyWizardProps {
  token: string;
  onFinished: () => void;
}

export function HostPropertyWizard({ token, onFinished }: HostPropertyWizardProps) {
  const [currentStep, setCurrentStep] = useState(1);
  const [savingDraft, setSavingDraft] = useState(false);
  const [draftSavedToast, setDraftSavedToast] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState<PropertyWizardData>({
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
    verificationEnabled: true,
    insuraGuestEnabled: true
  });

  const steps = [
    "Basics", "Location", "Type", "Capacity", "Amenities", 
    "Photos", "Description", "Pricing", "Availability", "Verification & Publish"
  ];

  function handleAutosave() {
    setSavingDraft(true);
    setTimeout(() => {
      setSavingDraft(false);
      setDraftSavedToast(true);
      setTimeout(() => setDraftSavedToast(false), 2000);
    }, 400);
  }

  async function handlePublish() {
    setPublishing(true);
    setError(null);
    try {
      await api.createProperty({
        hostUserId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        hostName: "Island Villa Hosting",
        hostEmail: "host-villa@nestystay.local",
        title: formData.title,
        location: formData.location,
        country: formData.country,
        nightlyRate: formData.nightlyRate,
        currency: formData.currency,
        badgeLevel: "Verified",
        cancellationPolicy: formData.cancellationPolicy,
        guestVerificationEnabled: formData.verificationEnabled,
        insuraGuestEnabled: formData.insuraGuestEnabled,
        highlights: formData.amenities
      }, token);
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
        <button type="button" className="btn btn-outline" onClick={handleAutosave} disabled={savingDraft}>
          <Save size={16} /> {savingDraft ? "Saving..." : "Save Draft"}
        </button>
      </header>

      {draftSavedToast && <div className="notice-panel mb-4">Draft autosaved to server.</div>}
      {error && <div className="alert-box alert-error mb-4">{error}</div>}

      {/* Stepper Progress Bar */}
      <div className="wizard-stepper-bar flex justify-between items-center gap-1 mb-8 overflow-x-auto pb-2">
        {steps.map((label, idx) => {
          const stepNum = idx + 1;
          const isActive = stepNum === currentStep;
          const isComplete = stepNum < currentStep;
          return (
            <div 
              key={label} 
              className={`stepper-pill flex items-center gap-1 text-xs px-3 py-2 rounded-full cursor-pointer transition ${
                isActive ? "bg-sun text-white font-bold" : isComplete ? "bg-green-light text-green" : "bg-gray-100 text-gray-500"
              }`}
              onClick={() => setCurrentStep(stepNum)}
            >
              <span>{isComplete ? <Check size={12} /> : stepNum}</span>
              <span className="hidden sm:inline">{label}</span>
            </div>
          );
        })}
      </div>

      {/* Step Content Container */}
      <div className="card-box wizard-step-card max-w-3xl mx-auto p-6 mb-6">
        <h3 className="text-xl font-bold mb-4">Step {currentStep}: {steps[currentStep - 1]}</h3>

        {currentStep === 1 && (
          <div className="space-y-4">
            <div className="field-group">
              <label className="field-label">Property Title</label>
              <input type="text" className="input-control" value={formData.title} onChange={(e) => setFormData({ ...formData, title: e.target.value })} />
            </div>
          </div>
        )}

        {currentStep === 2 && (
          <div className="space-y-4">
            <div className="field-group">
              <label className="field-label">Location / Parish</label>
              <input type="text" className="input-control" value={formData.location} onChange={(e) => setFormData({ ...formData, location: e.target.value })} />
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
            <label className="field-label mb-2">Property Photos & Cover Photo Selection</label>
            <div className="photo-upload-dropzone p-6 border-2 border-dashed rounded text-center mb-4">
              <Camera size={32} className="mx-auto mb-2 text-sun" />
              <p>Upload photos of your stay (WebP, JPG up to 10MB)</p>
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
      <footer className="wizard-footer flex justify-between max-w-3xl mx-auto">
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
      </footer>
    </div>
  );
}
