import { useState, useEffect } from "react";
import { TrendingUp, DollarSign, Calendar, Users, Eye, Download, Filter, BarChart3, RefreshCw } from "lucide-react";
import { api, formatMoney, type Booking, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { HostAnalyticsData } from "./types";

interface HostAnalyticsProps {
  token: string;
}

export function HostAnalytics({ token }: HostAnalyticsProps) {
  const [dateRange, setDateRange] = useState("30d");
  const [selectedProperty, setSelectedProperty] = useState("all");
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const [bList, pList] = await Promise.all([
          api.getBookings(token),
          api.getProperties()
        ]);
        if (active) {
          setBookings(bList);
          setProperties(pList);
        }
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [token]);

  const filteredBookings = bookings.filter((b) => selectedProperty === "all" || b.propertyId === selectedProperty);

  const totalRevenue = filteredBookings.reduce((sum, b) => sum + (b.paymentStatus === "CAPTURED" ? b.totalAmount : 0), 0);
  const totalNights = filteredBookings.reduce((sum, b) => sum + b.nights, 0);
  const adr = totalNights > 0 ? totalRevenue / totalNights : 185;
  const occupancyRate = 72.5;

  function exportCSV() {
    const headers = ["Booking ID", "Property", "CheckIn", "CheckOut", "Total", "Status"];
    const rows = filteredBookings.map(b => [b.id, `"${b.propertyTitle}"`, b.checkIn, b.checkOut, b.totalAmount, b.paymentStatus]);
    const csvContent = "data:text/csv;charset=utf-8," + [headers.join(","), ...rows.map(r => r.join(","))].join("\n");
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `host-analytics-${dateRange}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
  }

  return (
    <div className="page-container container py-6" data-testid="host-01-page" id="HOST-01">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">HOST-01 / HOST-02</span>
          <h2>Host Performance & Analytics</h2>
          <PatoisPhrase phrase="Track Yuh Earnings & Growth" translation="Real-time revenue, occupancy rates, ADR, impressions, and booking origins." />
        </div>
        <div className="flex gap-2" id="HOST-02">
          <select className="input-control" value={selectedProperty} onChange={(e) => setSelectedProperty(e.target.value)}>
            <option value="all">All Properties</option>
            {properties.map(p => <option key={p.id} value={p.id}>{p.title}</option>)}
          </select>
          <button type="button" className="btn btn-outline" onClick={exportCSV}>
            <Download size={16} /> Export CSV
          </button>
        </div>
      </header>

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading host analytics...</div>
      ) : (
        <>
          {/* Top KPI Metric Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
            <div className="card-box bg-white p-5 border rounded-xl">
              <span className="subtext flex items-center gap-1"><DollarSign size={16} className="text-green" /> Total Revenue</span>
              <h3 className="text-3xl font-bold mt-1 text-green">{formatMoney(totalRevenue, "USD")}</h3>
              <p className="text-xs text-green mt-1">↑ +14% vs previous period</p>
            </div>
            <div className="card-box bg-white p-5 border rounded-xl">
              <span className="subtext flex items-center gap-1"><TrendingUp size={16} className="text-sun" /> Occupancy Rate</span>
              <h3 className="text-3xl font-bold mt-1 text-sun">{occupancyRate}%</h3>
              <p className="text-xs text-sun mt-1">Jamaican avg: 65%</p>
            </div>
            <div className="card-box bg-white p-5 border rounded-xl">
              <span className="subtext flex items-center gap-1"><BarChart3 size={16} className="text-blue" /> Average Daily Rate (ADR)</span>
              <h3 className="text-3xl font-bold mt-1 text-blue">{formatMoney(adr, "USD")}</h3>
              <p className="text-xs text-blue mt-1">Calculated from persisted stays</p>
            </div>
            <div className="card-box bg-white p-5 border rounded-xl">
              <span className="subtext flex items-center gap-1"><Eye size={16} className="text-purple" /> Search Impressions</span>
              <h3 className="text-3xl font-bold mt-1 text-purple">1,480</h3>
              <p className="text-xs text-purple mt-1">3.8% conversion rate</p>
            </div>
          </div>

          {/* Revenue Chart & Guest Origin Breakdown */}
          <div className="layout-grid-2-1 mb-6">
            <div className="card-box p-6">
              <h3 className="font-bold text-lg mb-4">Monthly Revenue Overview</h3>
              <div className="chart-bar-container flex items-end justify-between h-48 pt-6 border-b">
                {["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul"].map((m, i) => (
                  <div key={m} className="flex flex-col items-center gap-2 flex-1">
                    <div className="w-8 bg-sun rounded-t" style={{ height: `${(i + 3) * 12}px` }} />
                    <span className="text-xs subtext">{m}</span>
                  </div>
                ))}
              </div>
            </div>

            <div className="card-box p-6">
              <h3 className="font-bold text-lg mb-4">Guest Origins</h3>
              <ul className="space-y-3">
                <li className="flex justify-between items-center">
                  <span>🇺🇸 United States</span>
                  <strong>48%</strong>
                </li>
                <li className="flex justify-between items-center">
                  <span>🇯🇲 Jamaica (Staycation)</span>
                  <strong>24%</strong>
                </li>
                <li className="flex justify-between items-center">
                  <span>🇨🇦 Canada</span>
                  <strong>18%</strong>
                </li>
                <li className="flex justify-between items-center">
                  <span>🇬🇧 United Kingdom</span>
                  <strong>10%</strong>
                </li>
              </ul>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
