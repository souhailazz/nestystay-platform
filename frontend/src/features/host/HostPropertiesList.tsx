import { useState, useEffect, useMemo } from "react";
import { Plus, Archive, RotateCcw, Edit, MapPin, Eye } from "lucide-react";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import { ListControls, downloadCsv } from "../../components/ui/ListControls";
import { announceFeedback } from "../../lib/feedback";

interface HostPropertiesListProps {
  view: string;
  token: string;
}

export function HostPropertiesList({ view, token }: HostPropertiesListProps) {
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [sort, setSort] = useState("name");
  const [page, setPage] = useState(0);
  const [selected, setSelected] = useState<string[]>([]);
  const [undo, setUndo] = useState<{ id: string; archived: boolean } | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getOwnedProperties(token);
        if (active) setProperties(list);
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [token]);

  const isArchivedView = view === "archived";
  const filtered = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    return properties
      .filter((p) => (isArchivedView ? p.isArchived : !p.isArchived))
      .filter((p) => !normalized || `${p.title} ${p.location} ${p.country} ${p.badgeLevel}`.toLowerCase().includes(normalized))
      .sort((left, right) => sort === "price" ? left.nightlyRate - right.nightlyRate : left.title.localeCompare(right.title));
  }, [isArchivedView, properties, query, sort]);
  useEffect(() => setPage(0), [isArchivedView, query, sort]);
  const pageSize = 6;
  const visible = filtered.slice(page * pageSize, (page + 1) * pageSize);

  async function handleArchiveToggle(id: string, currentArchived: boolean, skipConfirm = false) {
    setNotice(null);
    try {
      if (!currentArchived && !skipConfirm && !window.confirm("Archive this property? You can restore it later.")) return;
      if (currentArchived) {
        await api.restoreProperty(id, token);
      } else {
        await api.archiveProperty(id, token);
      }
      setProperties((items) => items.map(p => p.id === id ? { ...p, isArchived: !currentArchived } : p));
      setUndo({ id, archived: !currentArchived });
      setNotice(currentArchived ? "Property restored from archive." : "Property archived.");
      announceFeedback(currentArchived ? "Property restored." : "Property archived.");
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Action failed.");
    }
  }

  async function undoArchive() {
    if (!undo) return;
    await handleArchiveToggle(undo.id, undo.archived);
    setUndo(null);
  }

  function toggleSelected(id: string) {
    setSelected((items) => items.includes(id) ? items.filter((item) => item !== id) : [...items, id]);
  }

  async function bulkArchive() {
    if (selected.length === 0 || !window.confirm(`Archive ${selected.length} selected propert${selected.length === 1 ? "y" : "ies"}?`)) return;
    await Promise.all(selected.map((id) => handleArchiveToggle(id, false, true)));
    setSelected([]);
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

      {notice && <div className="notice-panel mb-4 flex flex-wrap items-center justify-between gap-2" role="status"><span>{notice}</span>{undo && <button className="btn btn-outline btn-sm" onClick={() => void undoArchive()} type="button">Undo</button>}</div>}

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading property listings...</div>
      ) : filtered.length === 0 ? (
        <div className="card-box text-center py-8">
          <p className="text-lg font-medium">No {isArchivedView ? "archived" : "active"} properties found.</p>
          {!isArchivedView && <a href="/host/properties/new" className="btn btn-primary mt-3">Add First Property</a>}
        </div>
      ) : (
        <>
          <ListControls
            className="mb-5"
            label="Filter properties"
            onExport={() => downloadCsv("nesty-properties.csv", ["Title", "Location", "Country", "Badge", "Nightly rate", "Archived"], filtered.map((prop) => [prop.title, prop.location, prop.country, prop.badgeLevel, prop.nightlyRate, prop.isArchived ? "Yes" : "No"]))}
            onPageChange={setPage}
            onQueryChange={setQuery}
            onSortChange={setSort}
            page={page}
            pageSize={pageSize}
            query={query}
            sort={sort}
            sortOptions={[{ value: "name", label: "Name" }, { value: "price", label: "Nightly price" }]}
            total={filtered.length}
          />
          {!isArchivedView && selected.length > 0 && <div className="mb-4 flex flex-wrap items-center gap-3 rounded-field border border-sand-border bg-shell px-3 py-2 text-sm"><span>{selected.length} selected</span><button className="btn btn-outline btn-sm" onClick={() => void bulkArchive()} type="button"><Archive size={14} /> Archive selected</button></div>}
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {visible.map((prop) => (
            <div key={prop.id} className="card-box flex flex-col justify-between">
              <div>
                {!isArchivedView && <label className="mb-3 inline-flex items-center gap-2 text-xs font-semibold"><input aria-label={`Select ${prop.title}`} checked={selected.includes(prop.id)} onChange={() => toggleSelected(prop.id)} type="checkbox" /> Select property</label>}
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
        </>
      )}
    </div>
  );
}
