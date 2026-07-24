import { useState, useEffect } from "react";
import { Plus, Archive, RotateCcw, Edit, MapPin, Eye, AlertCircle, CheckCircle2 } from "lucide-react";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface HostPropertiesListProps {
  view: string;
  token: string;
}

export function HostPropertiesList({ view, token }: HostPropertiesListProps) {
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState<string | null>(null);

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

  const isArchivedView = view === "archived";
  const filtered = properties.filter(p => isArchivedView ? p.isArchived : !p.isArchived);

  async function handleArchiveToggle(id: string, currentArchived: boolean) {
    setNotice(null);
    try {
      if (currentArchived) {
        await api.restoreProperty(id, token);
      } else {
        await api.archiveProperty(id, token);
      }
      setProperties(properties.map(p => p.id === id ? { ...p, isArchived: !currentArchived } : p));
      setNotice(currentArchived ? "Property restored from archive." : "Property archived.");
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Action failed.");
    }
  }

  return (
    <div className="page-container container py-6" data-testid={isArchivedView ? "host-04-page" : "host-03-page"} id={isArchivedView ? "HOST-04" : "HOST-03"}>
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">{isArchivedView ? "HOST-04" : "HOST-03"}</span>
          <h2>{isArchivedView ? "Archived Properties" : "My Property Listings"}</h2>
          <PatoisPhrase phrase="Manage Yuh Yard Dem" translation="Overview of your active, draft, pending, and archived property listings." />
        </div>
        <div className="flex gap-2">
          {!isArchivedView ? (
            <>
              <a href="/host/properties/archived" className="btn btn-outline">
                <Archive size={16} /> View Archived
              </a>
              <a href="/host/properties/new" className="btn btn-primary">
                <Plus size={16} /> Create Property (10-Step Wizard)
              </a>
            </>
          ) : (
            <a href="/host/properties" className="btn btn-outline">
              Active Properties
            </a>
          )}
        </div>
      </header>

      {notice && <div className="notice-panel mb-4">{notice}</div>}

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading property listings...</div>
      ) : filtered.length === 0 ? (
        <div className="card-box text-center py-8">
          <p className="text-lg font-medium">No {isArchivedView ? "archived" : "active"} properties found.</p>
          {!isArchivedView && <a href="/host/properties/new" className="btn btn-primary mt-3">Add First Property</a>}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {filtered.map((prop) => (
            <div key={prop.id} className="card-box flex flex-col justify-between">
              <div>
                <div className="flex justify-between items-start mb-2">
                  <span className="badge badge-green">{prop.badgeLevel} Badge</span>
                  <span className="badge badge-sun">{prop.cancellationPolicy}</span>
                </div>
                <h3 className="font-bold text-xl">{prop.title}</h3>
                <p className="subtext mt-1"><MapPin size={14} className="inline" /> {prop.location}, {prop.country}</p>
                <div className="mt-3 text-lg font-bold text-sun">
                  {formatMoney(prop.nightlyRate, prop.currency)} <span className="text-xs font-normal text-gray-500">/ night</span>
                </div>
              </div>

              <div className="flex justify-between items-center mt-6 pt-3 border-t">
                <div className="flex gap-2">
                  <a href={`/properties/${prop.id}`} className="btn btn-outline btn-sm">
                    <Eye size={14} /> Preview
                  </a>
                  <a href={`/host/properties/edit?id=${prop.id}`} className="btn btn-outline btn-sm">
                    <Edit size={14} /> Edit
                  </a>
                </div>

                <button 
                  type="button" 
                  className={`btn btn-ghost btn-sm ${prop.isArchived ? "text-green" : "text-coral"}`}
                  onClick={() => handleArchiveToggle(prop.id, !!prop.isArchived)}
                >
                  {prop.isArchived ? <><RotateCcw size={14} /> Restore</> : <><Archive size={14} /> Archive</>}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
