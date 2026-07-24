import { useState, useEffect } from "react";
import { Search, MapPin, Map, List, Filter, Heart, Star, ShieldCheck } from "lucide-react";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import { BookingModal } from "../booking/BookingModal";

interface PublicSearchMapProps {
  view: string;
}

export function PublicSearchMap({ view }: PublicSearchMapProps) {
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchParish, setSearchParish] = useState("all");
  const [showMap, setShowMap] = useState(view === "map");
  const [bookingProp, setBookingProp] = useState<PropertyListing | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getProperties();
        if (active) setProperties(list);
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, []);

  const filtered = properties.filter((p) => searchParish === "all" || p.location.toLowerCase().includes(searchParish.toLowerCase()));

  return (
    <div className="page-container container py-6" data-testid={showMap ? "pub-04-page" : "pub-01-page"} id={showMap ? "PUB-04" : "PUB-01"}>
      {/* Search Header Bar (PUB-02) */}
      <div className="search-filter-hero bg-white p-4 rounded-xl shadow-sm border mb-6 flex flex-wrap items-center justify-between gap-4" id="PUB-02">
        <div className="flex items-center gap-3 flex-1 min-w-[280px]">
          <Search size={20} className="text-sun" />
          <select className="input-control border-none shadow-none font-bold" value={searchParish} onChange={(e) => setSearchParish(e.target.value)}>
            <option value="all">All Parishes (Jamaica)</option>
            <option value="St. Ann">St. Ann (Ocho Rios)</option>
            <option value="St. James">St. James (Montego Bay)</option>
            <option value="Westmoreland">Westmoreland (Negril)</option>
            <option value="Portland">Portland (Port Antonio)</option>
          </select>
        </div>

        <div className="flex gap-2">
          <button 
            type="button" 
            className={`btn btn-sm ${!showMap ? "btn-primary" : "btn-outline"}`}
            onClick={() => setShowMap(false)}
          >
            <List size={16} /> Grid View
          </button>
          <button 
            type="button" 
            className={`btn btn-sm ${showMap ? "btn-primary" : "btn-outline"}`}
            onClick={() => setShowMap(true)}
          >
            <Map size={16} /> Map View
          </button>
        </div>
      </div>

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading Jamaican stays...</div>
      ) : showMap ? (
        /* Map Split View (PUB-04) */
        <div className="layout-grid-2-1 h-[600px] border rounded-xl overflow-hidden">
          <div className="space-y-4 p-4 overflow-y-auto">
            {filtered.map(p => (
              <div key={p.id} className="card-box flex justify-between items-center cursor-pointer hover:border-sun">
                <div>
                  <span className="badge badge-green">{p.badgeLevel}</span>
                  <h4 className="font-bold">{p.title}</h4>
                  <p className="subtext">{p.location}</p>
                  <strong className="text-sun">{formatMoney(p.nightlyRate, p.currency)} / night</strong>
                </div>
                <button type="button" className="btn btn-primary btn-sm" onClick={() => setBookingProp(p)}>Book</button>
              </div>
            ))}
          </div>
          <div className="map-placeholder-box bg-blue-light flex flex-col items-center justify-center p-6 text-center">
            <Map size={48} className="text-sun mb-2" />
            <strong className="text-lg">Jamaican Interactive Map</strong>
            <p className="subtext text-xs mt-1">Showing {filtered.length} property price pins across Jamaica.</p>
          </div>
        </div>
      ) : (
        /* Property Grid View (PUB-03) */
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6" id="PUB-03">
          {filtered.map((prop) => (
            <div key={prop.id} className="card-box flex flex-col justify-between hover:shadow-md transition">
              <div>
                <div className="flex justify-between items-start mb-2">
                  <span className="badge badge-green flex items-center gap-1"><ShieldCheck size={12} /> {prop.badgeLevel}</span>
                  <button type="button" className="btn btn-ghost p-1 text-coral" title="Save to Wishlist"><Heart size={18} /></button>
                </div>
                <h3 className="font-bold text-xl">{prop.title}</h3>
                <p className="subtext mt-1"><MapPin size={14} className="inline" /> {prop.location}, {prop.country}</p>
                <div className="mt-4 text-xl font-bold text-sun">
                  {formatMoney(prop.nightlyRate, prop.currency)} <span className="text-xs font-normal text-gray-500">/ night</span>
                </div>
              </div>

              <div className="flex gap-2 mt-6 pt-3 border-t">
                <a href={`/properties/${prop.id}`} className="btn btn-outline flex-1 text-center">Details</a>
                <button type="button" className="btn btn-primary flex-1" onClick={() => setBookingProp(prop)}>Book Now</button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Booking Modal Integration (BOOK-01) */}
      {bookingProp && (
        <BookingModal 
          property={bookingProp} 
          onClose={() => setBookingProp(null)} 
          onProceedToReview={() => window.location.href = "/booking/11111111-1111-4111-8111-111111111111/review"}
        />
      )}
    </div>
  );
}
