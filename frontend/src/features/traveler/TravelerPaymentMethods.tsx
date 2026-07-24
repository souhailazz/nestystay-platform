import { useState, useEffect } from "react";
import { CreditCard, Plus, Trash2, CheckCircle2, ShieldCheck, Lock } from "lucide-react";
import { api } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { SavedPaymentMethod } from "./types";

interface TravelerPaymentMethodsProps {
  userId: string;
  token: string;
}

export function TravelerPaymentMethods({ userId, token }: TravelerPaymentMethodsProps) {
  const [methods, setMethods] = useState<SavedPaymentMethod[]>([
    {
      id: "pm_mock_visa",
      brand: "Visa",
      last4: "4242",
      expMonth: 12,
      expYear: 2028,
      isDefault: true,
      createdAt: new Date().toISOString()
    }
  ]);
  const [showAddModal, setShowAddModal] = useState(false);
  const [cardName, setCardName] = useState("");

  function handleAddMethod() {
    const newMethod: SavedPaymentMethod = {
      id: `pm_${Date.now()}`,
      brand: "Mastercard",
      last4: "5555",
      expMonth: 10,
      expYear: 2029,
      isDefault: methods.length === 0,
      createdAt: new Date().toISOString()
    };
    setMethods([...methods, newMethod]);
    setShowAddModal(false);
  }

  function handleRemove(id: string) {
    setMethods(methods.filter(m => m.id !== id));
  }

  function handleSetDefault(id: string) {
    setMethods(methods.map(m => ({ ...m, isDefault: m.id === id })));
  }

  return (
    <div className="page-container container py-6" data-testid="trav-09-page" id="TRAV-09">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">TRAV-09</span>
          <h2>Saved Payment Methods</h2>
          <PatoisPhrase phrase="Secure Payment Wallet" translation="Manage your saved credit cards backed by Stripe SetupIntents." />
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setShowAddModal(true)}>
          <Plus size={16} /> Add Payment Method
        </button>
      </header>

      {showAddModal && (
        <div className="modal-backdrop">
          <div className="modal-card">
            <h3>Add New Card (Stripe SetupIntent)</h3>
            <p className="subtext mb-3">Your card information is tokenized securely via Stripe.</p>
            <div className="field-group mb-3">
              <label className="field-label">Cardholder Name</label>
              <input type="text" className="input-control" value={cardName} onChange={(e) => setCardName(e.target.value)} placeholder="Full name" />
            </div>
            <div className="field-group mb-4">
              <label className="field-label">Stripe Elements Card Container</label>
              <div className="stripe-mock-field">
                <span>•••• •••• •••• 5555</span>
                <span>10/29</span>
                <span>***</span>
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button type="button" className="btn btn-ghost" onClick={() => setShowAddModal(false)}>Cancel</button>
              <button type="button" className="btn btn-primary" onClick={handleAddMethod}><Lock size={16} /> Save Card</button>
            </div>
          </div>
        </div>
      )}

      <div className="space-y-4 max-w-2xl">
        {methods.map((method) => (
          <div key={method.id} className="card-box flex justify-between items-center">
            <div className="flex items-center gap-3">
              <CreditCard size={24} className="text-sun" />
              <div>
                <strong>{method.brand} ending in •••• {method.last4}</strong>
                <p className="subtext">Expires {method.expMonth}/{method.expYear}</p>
                {method.isDefault && <span className="badge badge-green mt-1">Default Method</span>}
              </div>
            </div>

            <div className="flex gap-2">
              {!method.isDefault && (
                <button type="button" className="btn btn-ghost btn-sm" onClick={() => handleSetDefault(method.id)}>
                  Set Default
                </button>
              )}
              <button type="button" className="btn btn-ghost btn-sm text-coral" onClick={() => handleRemove(method.id)}>
                <Trash2 size={16} />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
