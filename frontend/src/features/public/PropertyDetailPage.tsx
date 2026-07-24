import { useState, useEffect } from "react";
import { MapPin, ShieldCheck, Heart, Star, Calendar, Users, Check, AlertCircle } from "lucide-react";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import { BookingModal } from "../booking/BookingModal";

interface PropertyDetailPageProps {
  propertyId?: string;
}

export function PropertyDetailPage({ propertyId }: PropertyDetailPageProps) {
  const [property, setProperty] = useState<PropertyListing | null>(null);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getProperties();
        const found = list.find(p => p.id === propertyId) || list[0];
        if (active) setProperty(found);
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [propertyId]);

  if (loading || !property) {
    return <div className="loading-shimmer p-6 text-center">Loading property details...</div>;
  }

  return (
    <div className="page-container container py-6" data-testid="pub-06-page" id="PUB-06">
      <header className="mb-6">
        <div className="flex items-center gap-2 mb-1">
          <span className="badge badge-green flex items-center gap-1"><ShieldCheck size={14} /> {property.badgeLevel} Badge</span>
          <span className="badge badge-sun">{property.cancellationPolicy} Cancellation</span>
        </div>
        <h1 className="text-3xl font-bold">{property.title}</h1>
        <p className="subtext mt-1"><MapPin size={16} className="inline" /> {property.location}, {property.country}</p>
      </header>

      {/* Photo Gallery Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6 rounded-xl overflow-hidden">
        <div className="bg-sun-light h-64 md:col-span-2 rounded-lg flex items-center justify-center font-bold text-sun text-xl">
          Main Photo Showcase
        </div>
        <div className="space-y-4">
          <div className="bg-green-light h-30 rounded-lg flex items-center justify-center font-bold text-green">Interior View</div>
          <div className="bg-blue-light h-30 rounded-lg flex items-center justify-center font-bold text-blue">Pool & Patio</div>
        </div>
      </div>

      <div className="layout-grid-2-1">
        {/* Left Main Content */}
        <div className="space-y-6">
          {/* Host Card */}
          <div className="card-box">
            <div className="flex items-center gap-4">
              <div className="w-14 h-14 bg-sun rounded-full flex items-center justify-center font-bold text-white text-xl">
                {property.hostName.substring(0, 1)}
              </div>
              <div>
                <strong className="text-lg">Hosted by {property.hostName}</strong>
                <p className="subtext">Verified NestyStay Host • Fast response rate</p>
              </div>
            </div>
          </div>

          {/* InsuraGuest Protection Banner (PUB-07) */}
          <div className="card-box bg-sun-light border-sun" id="PUB-07" data-testid="pub-07-insuraguest">
            <h3 className="font-bold text-lg flex items-center gap-2 text-sun"><ShieldCheck size={20} /> InsuraGuest $15/night Protection Included</h3>
            <p className="subtext mt-1">Covers accidental property damage, medical accidents, and personal liability during your stay.</p>
          </div>

          {/* Highlights & Amenities */}
          <div className="card-box">
            <h3>Amenities & Highlights</h3>
            <div className="grid grid-cols-2 gap-2 my-3">
              {property.highlights.map(h => (
                <div key={h} className="flex items-center gap-2">
                  <Check size={16} className="text-sun" /> <span>{h}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Right Sticky Booking Widget */}
        <div className="card-box sticky-top">
          <div className="flex justify-between items-baseline mb-4">
            <strong className="text-2xl font-bold text-sun">{formatMoney(property.nightlyRate, property.currency)}</strong>
            <span className="subtext">/ night</span>
          </div>

          <button type="button" className="btn btn-primary w-full py-3 font-bold text-lg" onClick={() => setShowModal(true)}>
            Reserve This Stay
          </button>
        </div>
      </div>

      {showModal && (
        <BookingModal 
          property={property} 
          onClose={() => setShowModal(false)} 
          onProceedToReview={() => window.location.href = "/booking/11111111-1111-4111-8111-111111111111/review"}
        />
      )}
    </div>
  );
}
