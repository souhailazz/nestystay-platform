import { useState } from "react";
import { User, Search, ShieldCheck, Lock, Edit2, CheckCircle2, XCircle } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";
import type { AdminUserItem } from "./types";

interface AdminUsersProps {
  view: string;
  token: string;
}

export function AdminUsers({ view, token }: AdminUsersProps) {
  const [users, setUsers] = useState<AdminUserItem[]>([
    {
      id: "u-101",
      email: "guest@nestystay.local",
      displayName: "Traveler Guest",
      role: "Traveler",
      status: "Active",
      identityStatus: "Verified",
      createdAt: "2026-05-10"
    },
    {
      id: "u-102",
      email: "host-villa@nestystay.local",
      displayName: "Island Villa Hosting",
      role: "Host",
      status: "Active",
      identityStatus: "Verified",
      createdAt: "2026-04-12"
    }
  ]);
  const [search, setSearch] = useState("");
  const [selectedUser, setSelectedUser] = useState<AdminUserItem | null>(null);

  const filtered = users.filter(u => u.email.toLowerCase().includes(search.toLowerCase()) || u.displayName.toLowerCase().includes(search.toLowerCase()));

  function toggleStatus(id: string) {
    setUsers(users.map(u => u.id === id ? { ...u, status: u.status === "Active" ? "Suspended" : "Active" } : u));
  }

  return (
    <div className="page-container container py-6" data-testid="adm-02-page" id="ADM-02">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">ADM-02 / ADM-03</span>
          <h2>User Accounts & Identity Moderation</h2>
          <PatoisPhrase phrase="Manage Platform Accounts & Roles" translation="Filter user directory, change roles, suspend accounts, and review eKYC submissions." />
        </div>
        <div className="search-box relative">
          <input 
            type="text" 
            className="input-control pl-8" 
            placeholder="Search users..." 
            value={search} 
            onChange={(e) => setSearch(e.target.value)} 
          />
        </div>
      </header>

      <div className="card-box">
        <table className="table-styled w-full">
          <thead>
            <tr>
              <th>User</th>
              <th>Role</th>
              <th>Status</th>
              <th>eKYC State</th>
              <th>Joined</th>
              <th className="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((u) => (
              <tr key={u.id}>
                <td>
                  <strong>{u.displayName}</strong>
                  <p className="subtext text-xs">{u.email}</p>
                </td>
                <td><span className="badge badge-sun">{u.role}</span></td>
                <td><span className={`badge ${u.status === "Active" ? "badge-green" : "badge-red"}`}>{u.status}</span></td>
                <td><span className="badge badge-green">{u.identityStatus}</span></td>
                <td>{u.createdAt}</td>
                <td className="text-right flex justify-end gap-2">
                  <button type="button" className="btn btn-ghost btn-sm" onClick={() => setSelectedUser(u)}>
                    Details
                  </button>
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => toggleStatus(u.id)}>
                    {u.status === "Active" ? "Suspend" : "Activate"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
