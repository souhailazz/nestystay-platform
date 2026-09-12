import { useState, useEffect, useMemo } from "react";
import { Plus, Archive, RotateCcw, Edit, MapPin, Eye, Copy, Upload, History } from "lucide-react";
import { api, formatMoney, type PropertyListing, type PropertyRevision } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import { ListControls, downloadCsv } from "../../components/ui/ListControls";
import { announceFeedback } from "../../lib/feedback";
import { Modal } from "../../components/ui/Modal";
import { requestConfirmation } from "../../lib/confirmation";

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
  const [historyProperty, setHistoryProperty] = useState<PropertyListing | null>(null);
  const [historyRevisions, setHistoryRevisions] = useState<PropertyRevision[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

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
      if (!currentArchived && !skipConfirm && !(await requestConfirmation({ title: "Archive property?", message: "You can restore it later." }))) return;
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

  function toggleAllVisible() {
    const visibleIds = visible.filter((property) => !property.isArchived).map((property) => property.id);
    setSelected((items) => visibleIds.every((id) => items.includes(id)) ? items.filter((id) => !visibleIds.includes(id)) : [...new Set([...items, ...visibleIds])]);
  }

  async function bulkArchive() {
    if (selected.length === 0 || !(await requestConfirmation({ title: "Archive selected properties?", message: `Archive ${selected.length} selected propert${selected.length === 1 ? "y" : "ies"}?` }))) return;
    try {
      await api.bulkArchiveProperties(selected, token, true);
      setProperties((items) => items.map((item) => selected.includes(item.id) ? { ...item, isArchived: true } : item));
      setSelected([]);
      setNotice(`${selected.length} propert${selected.length === 1 ? "y" : "ies"} archived.`);
      announceFeedback("Selected properties archived.");
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Could not archive selected properties.");
    }
  }

  async function duplicateProperty(id: string, title: string) {
    if (!(await requestConfirmation({ title: "Duplicate property?", message: `Duplicate “${title}” as a draft?` }))) return;
    try {
      const duplicate = await api.duplicateProperty(id, token);
      setProperties((items) => [...items, duplicate]);
      setNotice(`Draft “${duplicate.title}” created. Review it before publishing.`);
      announceFeedback("Property draft duplicated.");
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Could not duplicate property.");
    }
  }

  async function publishDraft(id: string, title: string) {
    if (!(await requestConfirmation({ title: "Publish property?", message: `Publish “${title}” to the public listings?` }))) return;
    try {
      const published = await api.publishProperty(id, token);
      setProperties((items) => items.map((item) => item.id === id ? published : item));
      setNotice(`“${published.title}” is now published.`);
      announceFeedback("Property published.");
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Could not publish property.");
    }
  }

  async function openHistory(property: PropertyListing) {
    setHistoryProperty(property);
    setHistoryRevisions([]);
    setHistoryLoading(true);
    try {
      setHistoryRevisions(await api.getPropertyRevisions(property.id, token));
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Could not load property history.");
    } finally {
      setHistoryLoading(false);
    }
  }

  async function restoreRevision(revision: PropertyRevision) {
    if (!historyProperty || !(await requestConfirmation({ title: "Restore property revision?", message: `Restore version ${revision.version} as a new draft?` }))) return;
    try {
      const restored = await api.restorePropertyRevision(historyProperty.id, revision.id, token);
      setProperties((items) => items.map((item) => item.id === restored.id ? restored : item));
      setHistoryProperty(restored);
      setHistoryRevisions(await api.getPropertyRevisions(restored.id, token));
      setNotice(`Version ${revision.version} restored as a draft. Review and publish when ready.`);
      announceFeedback("Property revision restored as a draft.");
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Could not restore property revision.");
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
            allVisibleSelected={visible.length > 0 && visible.filter((property) => !property.isArchived).every((property) => selected.includes(property.id))}
            label="Filter properties"
            onExport={() => downloadCsv("nesty-properties.csv", ["Title", "Location", "Country", "Badge", "Nightly rate", "Archived"], filtered.map((prop) => [prop.title, prop.location, prop.country, prop.badgeLevel, prop.nightlyRate, prop.isArchived ? "Yes" : "No"]))}
            onPageChange={setPage}
            onQueryChange={setQuery}
            onSortChange={setSort}
            onToggleAllVisible={isArchivedView ? undefined : toggleAllVisible}
            page={page}
            pageSize={pageSize}
            query={query}
            selectable={!isArchivedView}
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
                  <span className={`badge ${prop.isDraft ? "badge-sun" : "badge-green"}`}>{prop.isDraft ? "Draft" : `${prop.badgeLevel} Badge`}</span>
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
                  {!prop.isDraft && <a href={`/properties/${prop.id}`} className="btn btn-outline btn-sm">
                    <Eye size={14} /> Preview
                  </a>}
                  <a href={`/host/properties/edit?id=${prop.id}`} className="btn btn-outline btn-sm">
                    <Edit size={14} /> Edit
                  </a>
                  {prop.isDraft && <button className="btn btn-primary btn-sm" onClick={() => void publishDraft(prop.id, prop.title)} type="button"><Upload size={14} /> Publish</button>}
                  {!prop.isDraft && <button className="btn btn-outline btn-sm" onClick={() => void duplicateProperty(prop.id, prop.title)} type="button"><Copy size={14} /> Duplicate</button>}
                  <button className="btn btn-ghost btn-sm" onClick={() => void openHistory(prop)} type="button"><History size={14} /> History</button>
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

      <Modal open={historyProperty !== null} onClose={() => setHistoryProperty(null)} title={historyProperty ? `${historyProperty.title} history` : "Property history"}>
        {historyLoading ? <div className="loading-shimmer p-5 text-center">Loading revision history…</div> : historyRevisions.length === 0 ? <p className="text-sm text-sand-600">No revisions have been recorded yet.</p> : <div className="grid gap-3">
          <p className="m-0 text-sm text-sand-600">Every save, archive, publish and restore is preserved. Restoring always creates a new draft revision.</p>
          {historyRevisions.map((revision) => <div className="flex flex-wrap items-center justify-between gap-3 rounded-field border border-sand-border bg-white p-3" key={revision.id}>
            <div><strong>Version {revision.version}</strong><div className="text-xs text-sand-600">{new Date(revision.createdAt).toLocaleString()}</div></div>
            <button className="btn btn-outline btn-sm" onClick={() => void restoreRevision(revision)} type="button">Restore as draft</button>
          </div>)}
        </div>}
      </Modal>
    </div>
  );
}
