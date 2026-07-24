import { useState, useEffect } from "react";
import { Heart, Plus, Trash2, Edit2, GripVertical, MapPin } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { WishlistCollection } from "./types";

interface TravelerWishlistsProps {
  userId: string;
  token: string;
}

export function TravelerWishlists({ userId, token }: TravelerWishlistsProps) {
  const [collections, setCollections] = useState<WishlistCollection[]>([
    {
      id: "col-1",
      name: "Jamaican Dream Villas",
      createdAt: new Date().toISOString(),
      sortOrder: 0,
      items: [
        {
          id: "item-1",
          propertyId: "11111111-1111-4111-8111-111111111111",
          title: "Ocho Rios Verified Villa",
          location: "Ocho Rios, St. Ann",
          nightlyRate: 185,
          currency: "USD",
          addedAt: new Date().toISOString(),
          sortOrder: 0
        }
      ]
    }
  ]);
  const [newColName, setNewColName] = useState("");
  const [showAddModal, setShowAddModal] = useState(false);

  function handleCreateCollection() {
    if (!newColName.trim()) return;
    const newCol: WishlistCollection = {
      id: `col-${Date.now()}`,
      name: newColName.trim(),
      createdAt: new Date().toISOString(),
      sortOrder: collections.length,
      items: []
    };
    setCollections([...collections, newCol]);
    setNewColName("");
    setShowAddModal(false);
  }

  function handleDeleteCollection(id: string) {
    setCollections(collections.filter(c => c.id !== id));
  }

  return (
    <div className="page-container container py-6" data-testid="trav-07-page" id="TRAV-07">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">TRAV-07 / TRAV-08</span>
          <h2>Saved Stays & Collections</h2>
          <PatoisPhrase phrase="Yuh Favorite Spot Dem" translation="Organize your favorite Jamaican stays into custom collections." />
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setShowAddModal(true)}>
          <Plus size={16} /> New Collection
        </button>
      </header>

      {/* Collection Creation Modal */}
      {showAddModal && (
        <div className="modal-backdrop">
          <div className="modal-card">
            <h3>Create New Wishlist Collection</h3>
            <input 
              type="text" 
              className="input-control my-3" 
              placeholder="e.g. Honeymoon Beach Stays" 
              value={newColName} 
              onChange={(e) => setNewColName(e.target.value)} 
            />
            <div className="flex justify-end gap-2">
              <button type="button" className="btn btn-ghost" onClick={() => setShowAddModal(false)}>Cancel</button>
              <button type="button" className="btn btn-primary" onClick={handleCreateCollection}>Create</button>
            </div>
          </div>
        </div>
      )}

      {/* Collections Grid */}
      <div className="space-y-6">
        {collections.map((col) => (
          <div key={col.id} className="card-box" id="TRAV-08" data-testid="trav-08-collection">
            <div className="flex justify-between items-center mb-4">
              <div className="flex items-center gap-2">
                <GripVertical size={18} className="cursor-grab text-gray-400" />
                <h3 className="font-bold text-xl">{col.name}</h3>
                <span className="badge badge-sun">{col.items.length} stays</span>
              </div>
              <button type="button" className="btn btn-ghost text-coral" onClick={() => handleDeleteCollection(col.id)}>
                <Trash2 size={16} /> Delete Collection
              </button>
            </div>

            {col.items.length === 0 ? (
              <p className="subtext text-center py-4">No saved stays in this collection yet.</p>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {col.items.map((item) => (
                  <div key={item.id} className="card-box bg-white border hover:shadow-sm">
                    <h4 className="font-bold">{item.title}</h4>
                    <p className="subtext"><MapPin size={14} className="inline" /> {item.location}</p>
                    <div className="flex justify-between items-center mt-3 pt-2 border-t">
                      <strong className="text-sun">{formatMoney(item.nightlyRate, item.currency)} / night</strong>
                      <a href={`/properties/${item.propertyId}`} className="btn btn-outline btn-sm">View</a>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
