import { useState, useEffect } from "react";
import { Calendar, MapPin, Sparkles, Bell, Heart, CreditCard, ChevronRight, Star, AlertCircle, RefreshCw } from "lucide-react";
import { api, formatMoney, type Booking, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface TravelerDashboardProps {
  userId: string;
  token: string;
}

export function TravelerDashboard({ userId, token }: TravelerDashboardProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [suggestions, setSuggestions] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    async function loadData() {
      try {
        const [bList, pList] = await Promise.all([
          api.getBookings(token),
          api.getProperties()
        ]);
        if (active) {
          setBookings(bList);
          setSuggestions(pList.slice(0, 3));
        }
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    loadData();
    return () => { active = false; };
  }, [token]);

  const upcomingTrip = bookings.find(b => b.status === "APPROVED" || b.status === "CONFIRMED" || b.paymentStatus === "CAPTURED");

  if (loading) {
    return (
      <div className="container py-6" data-testid="trav-01-loading">
        <div className="loading-shimmer p-6 text-center">
          <RefreshCw size={24} className="spin mb-2" />
          <p>Loading your traveler dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page-container container py-6" data-testid="trav-01-page" id="TRAV-01">
      <header className="page-header mb-6">
        <span className="badge badge-sun">TRAV-01</span>
        <h2>Welcome Back to NestyStay</h2>
        <PatoisPhrase phrase="Walk Good & Stay Safe" translation="Explore your upcoming trips, personalized stay recommendations, and alerts." />
      </header>

      {/* Hero Upcoming Trip Banner */}
      <div className="card-box upcoming-trip-hero mb-6 bg-sun-light p-6 rounded-xl border border-sun">
        <div className="flex items-center justify-between mb-3">
          <span className="badge badge-sun font-bold"><Calendar size={14} className="inline mr-1" /> Upcoming Stay</span>
          <span className="text-sm subtext">Booking ID: {upcomingTrip ? upcomingTrip.id.substring(0, 8) : "No active trip"}</span>
        </div>

        {upcomingTrip ? (
          <div className="grid md:grid-cols-2 gap-4 items-center">
            <div>
              <h3 className="text-2xl font-bold">{upcomingTrip.propertyTitle}</h3>
              <p className="subtext mt-1"><MapPin size={16} className="inline" /> Jamaica • {upcomingTrip.checkIn} to {upcomingTrip.checkOut}</p>
              <div className="mt-4 flex gap-3">
                <a href={`/traveler/reservations/${upcomingTrip.id}`} className="btn btn-primary">
                  View Booking Details
                </a>
                <a href="/messages" className="btn btn-outline">
                  Contact Host
                </a>
              </div>
            </div>
            <div className="trip-status-card p-4 bg-white rounded-lg shadow-sm">
              <div className="info-row">
                <span>Verification:</span>
                <span className="badge badge-green">{upcomingTrip.verificationStatus}</span>
              </div>
              <div className="info-row">
                <span>Payment:</span>
                <span className="badge badge-green">{upcomingTrip.paymentStatus}</span>
              </div>
              <div className="info-row">
                <span>Total Amount:</span>
                <strong>{formatMoney(upcomingTrip.totalAmount, upcomingTrip.currency)}</strong>
              </div>
            </div>
          </div>
        ) : (
          <div className="text-center py-4">
            <p className="text-lg font-medium">No upcoming reservations scheduled right now.</p>
            <p className="subtext mt-1">Ready for your next Jamaican getaway?</p>
            <a href="/explore" className="btn btn-primary mt-3">Explore Stays</a>
          </div>
        )}
      </div>

      {/* Dashboard KPI Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div className="card-box">
          <div className="flex items-center gap-3">
            <div className="p-3 bg-sun-light rounded-lg"><Calendar size={24} className="text-sun" /></div>
            <div>
              <span className="subtext">Total Bookings</span>
              <h4 className="text-2xl font-bold">{bookings.length}</h4>
            </div>
          </div>
        </div>
        <div className="card-box">
          <div className="flex items-center gap-3">
            <div className="p-3 bg-green-light rounded-lg"><Heart size={24} className="text-green" /></div>
            <div>
              <span className="subtext">Wishlist Collections</span>
              <h4 className="text-2xl font-bold">2 Saved</h4>
            </div>
          </div>
        </div>
        <div className="card-box">
          <div className="flex items-center gap-3">
            <div className="p-3 bg-blue-light rounded-lg"><Bell size={24} className="text-blue" /></div>
            <div>
              <span className="subtext">Notifications</span>
              <h4 className="text-2xl font-bold">0 Unread</h4>
            </div>
          </div>
        </div>
      </div>

      {/* TRAV-02 AI Trip Suggestions Section */}
      <section className="suggestions-section mb-6" id="TRAV-02" data-testid="trav-02-section">
        <div className="flex items-center justify-between mb-4">
          <div>
            <span className="badge badge-sun">TRAV-02</span>
            <h3 className="text-xl font-bold"><Sparkles size={18} className="inline text-sun" /> Curated Stays For You</h3>
          </div>
          <a href="/explore" className="btn btn-ghost">View All Stays <ChevronRight size={16} /></a>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {suggestions.map((prop) => (
            <div key={prop.id} className="card-box property-card-compact hover:shadow-md transition">
              <div className="property-card-header mb-2">
                <span className="badge badge-green">{prop.badgeLevel}</span>
                <h4 className="font-bold text-lg mt-1">{prop.title}</h4>
                <p className="subtext"><MapPin size={14} className="inline" /> {prop.location}, {prop.country}</p>
              </div>
              <div className="flex items-center justify-between mt-3 pt-3 border-t">
                <span className="font-bold text-sun">{formatMoney(prop.nightlyRate, prop.currency)} <span className="text-xs font-normal">/ night</span></span>
                <a href={`/properties/${prop.id}`} className="btn btn-outline btn-sm">Details</a>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
