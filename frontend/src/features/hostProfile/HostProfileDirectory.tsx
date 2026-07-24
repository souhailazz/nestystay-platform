import { useState, useEffect } from "react";
import { User, Search, MapPin, ShieldCheck, Award, ExternalLink, Star } from "lucide-react";
import { api } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { HostProfileItem } from "./types";

interface HostProfileDirectoryProps {
  token: string;
}

export function HostProfileDirectory({ token }: HostProfileDirectoryProps) {
  const [hosts, setHosts] = useState<HostProfileItem[]>([
    {
      id: "hp-1",
      hostUserId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      displayName: "Island Villa Hosting",
      parish: "St. Ann",
      bio: "Premier vacation villa host in Ocho Rios & Montego Bay with 5+ years of verified Jamaican hospitality.",
      responseTime: "Replies in 10 minutes",
      badges: ["Verified", "Trusted"],
      listingIds: ["11111111-1111-4111-8111-111111111111"],
      totalBookings: 142,
      ratingAverage: 4.9,
      isPublic: true,
      linkMiUrl: "https://linkmi.jamaica/islandvilla"
    }
  ]);
  const [search, setSearch] = useState("");

  const filtered = hosts.filter(h => h.displayName.toLowerCase().includes(search.toLowerCase()) || h.parish.toLowerCase().includes(search.toLowerCase()));

  return (
    <div className="page-container container py-6" data-testid="hpro-01-page" id="HPRO-01">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">HPRO-01 / DIR-01..06</span>
          <h2>Verified Host Directory</h2>
          <PatoisPhrase phrase="Meet Yuh Jamaican Hosts" translation="Browse accredited local hosts, response rates, ratings, and Link Mi profiles." />
        </div>
        <div className="search-box relative">
          <input 
            type="text" 
            className="input-control pl-8" 
            placeholder="Search host by name or parish..." 
            value={search} 
            onChange={(e) => setSearch(e.target.value)} 
          />
        </div>
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {filtered.map((host) => (
          <div key={host.id} className="card-box flex flex-col justify-between" id="DIR-01">
            <div>
              <div className="flex items-center gap-4 mb-3">
                <div className="w-16 h-16 bg-sun rounded-full flex items-center justify-center font-bold text-white text-2xl">
                  {host.displayName.substring(0, 1)}
                </div>
                <div>
                  <h3 className="font-bold text-xl">{host.displayName}</h3>
                  <p className="subtext text-xs"><MapPin size={12} className="inline" /> {host.parish}, Jamaica • {host.responseTime}</p>
                  <div className="flex items-center gap-1 text-sun text-xs mt-1">
                    <Star size={12} fill="currentColor" /> <strong>{host.ratingAverage}</strong> ({host.totalBookings} stays)
                  </div>
                </div>
              </div>

              <div className="flex gap-2 mb-3">
                {host.badges.map(b => (
                  <span key={b} className="badge badge-green flex items-center gap-1 text-xs">
                    <ShieldCheck size={12} /> {b} Host
                  </span>
                ))}
              </div>

              <p className="text-sm subtext">{host.bio}</p>
            </div>

            <div className="flex justify-between items-center mt-6 pt-3 border-t">
              <a href={`/host-profile/${host.id}`} className="btn btn-outline btn-sm">
                View Full Profile
              </a>
              {host.linkMiUrl && (
                <a href={host.linkMiUrl} target="_blank" rel="noreferrer" className="btn btn-ghost btn-sm text-sun flex items-center gap-1">
                  Link Mi Contact <ExternalLink size={12} />
                </a>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
