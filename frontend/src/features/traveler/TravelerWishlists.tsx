import { useEffect, useState } from "react";
import { Heart, MapPin, Plus, Trash2 } from "lucide-react";
import { api, formatMoney, type PropertyListing, type WishlistCollection } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface TravelerWishlistsProps {
  userId: string;
  token: string;
}

/** TRAV-07/TRAV-08 — saved stays backed by the authenticated wishlist API. */
export function TravelerWishlists({ userId, token }: TravelerWishlistsProps) {
  const [collections, setCollections] = useState<WishlistCollection[]>([]);
  const [propertyDetails, setPropertyDetails] = useState<Record<string, PropertyListing>>({});
  const [newColName, setNewColName] = useState("");
  const [showAddModal, setShowAddModal] = useState(false);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadWorkspace() {
    setLoading(true);
    setError(null);
    try {
      const workspace = await api.getTravelerWorkspace(userId, token);
      const nextCollections = [...workspace.wishlistCollections].sort((left, right) => left.sortOrder - right.sortOrder);
      setCollections(nextCollections);

      const propertyIds = [...new Set(nextCollections.flatMap((collection) => collection.items.map((item) => item.propertyId)))];
      const details = await Promise.all(propertyIds.map(async (propertyId) => {
        try { return [propertyId, await api.getProperty(propertyId)] as const; }
        catch { return null; }
      }));
      setPropertyDetails(Object.fromEntries(details.filter((item): item is readonly [string, PropertyListing] => item !== null)));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Saved stays could not be loaded.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void loadWorkspace(); }, [userId, token]);

  async function handleCreateCollection() {
    const name = newColName.trim();
    if (!name) return;
    setBusy(true);
    setError(null);
    try {
      const created = await api.createWishlistCollection(userId, token, { name, sortOrder: collections.length });
      setCollections((current) => [...current, created]);
      setNewColName("");
      setShowAddModal(false);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Collection could not be created.");
    } finally {
      setBusy(false);
    }
  }

  async function handleDeleteCollection(collectionId: string) {
    setBusy(true);
    setError(null);
    try {
      await api.deleteWishlistCollection(userId, collectionId, token);
      setCollections((current) => current.filter((collection) => collection.id !== collectionId));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Collection could not be deleted.");
    } finally {
      setBusy(false);
    }
  }

  async function handleRemoveItem(itemId: string) {
    setBusy(true);
    setError(null);
    try {
      await api.removeWishlistItem(userId, itemId, token);
      setCollections((current) => current.map((collection) => ({
        ...collection,
        items: collection.items.filter((item) => item.id !== itemId),
      })));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Saved stay could not be removed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="page-container container py-6" data-testid="trav-07-page" id="TRAV-07">
      <header className="page-header mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <span className="badge badge-sun">TRAV-07 / TRAV-08</span>
          <h2>Saved Stays &amp; Collections</h2>
          <PatoisPhrase phrase="Yuh Favorite Spot Dem" translation="Organize your favorite Jamaican stays into custom collections." />
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setShowAddModal(true)}>
          <Plus size={16} /> New Collection
        </button>
      </header>

      {showAddModal && (
        <div className="modal-backdrop" role="presentation">
          <div aria-labelledby="wishlist-create-title" aria-modal="true" className="modal-card" role="dialog">
            <h3 id="wishlist-create-title">Create New Wishlist Collection</h3>
            <input
              aria-label="Collection name"
              type="text"
              className="input-control my-3"
              placeholder="e.g. Honeymoon Beach Stays"
              value={newColName}
              onChange={(event) => setNewColName(event.target.value)}
            />
            <div className="flex flex-wrap justify-end gap-2">
              <button type="button" className="btn btn-ghost" onClick={() => setShowAddModal(false)}>Cancel</button>
              <button type="button" className="btn btn-primary" disabled={busy || !newColName.trim()} onClick={() => void handleCreateCollection()}>Create</button>
            </div>
          </div>
        </div>
      )}

      {loading && <div className="card-box">Loading your saved stays…</div>}
      {error && <div className="notice-panel" role="alert">{error}</div>}
      {!loading && !error && collections.length === 0 && (
        <div className="card-box flex flex-col items-center gap-3 py-12 text-center">
          <Heart aria-hidden="true" className="text-coral" size={28} />
          <h3 className="m-0">No saved stays yet</h3>
          <p className="subtext m-0 max-w-md">Tap the heart on any Explore card or property page and your stays will appear here.</p>
        </div>
      )}

      {!loading && collections.length > 0 && (
        <div className="space-y-6">
          {collections.map((collection) => (
            <section key={collection.id} className="card-box" id="TRAV-08" data-testid="trav-08-collection">
              <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h3 className="m-0 font-bold text-xl">{collection.name}</h3>
                  <span className="badge badge-sun mt-2">{collection.items.length} {collection.items.length === 1 ? "stay" : "stays"}</span>
                </div>
                <button type="button" className="btn btn-ghost text-coral" disabled={busy} onClick={() => void handleDeleteCollection(collection.id)}>
                  <Trash2 size={16} /> Delete Collection
                </button>
              </div>

              {collection.items.length === 0 ? (
                <p className="subtext py-4 text-center">No saved stays in this collection yet.</p>
              ) : (
                <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                  {collection.items.map((item) => {
                    const detail = propertyDetails[item.propertyId];
                    return (
                      <article key={item.id} className="card-box bg-white">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <h4 className="m-0 font-bold">{detail?.title ?? item.propertyTitle}</h4>
                            <p className="subtext"><MapPin size={14} className="inline" /> {detail?.location ?? "NestyStay property"}</p>
                          </div>
                          <button aria-label={`Remove ${item.propertyTitle} from saved stays`} className="btn btn-ghost text-coral" disabled={busy} onClick={() => void handleRemoveItem(item.id)} type="button"><Trash2 size={16} /></button>
                        </div>
                        <div className="mt-3 flex items-center justify-between gap-2 border-t pt-2">
                          <strong className="text-sun">{detail ? `${formatMoney(detail.nightlyRate, detail.currency)} / night` : item.status}</strong>
                          <a href={`/properties/${item.propertyId}`} className="btn btn-outline btn-sm">View</a>
                        </div>
                      </article>
                    );
                  })}
                </div>
              )}
            </section>
          ))}
        </div>
      )}
    </div>
  );
}
