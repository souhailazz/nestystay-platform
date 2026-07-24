import { useState } from "react";
import { MapPin, ShieldCheck, Star, ExternalLink, MessageSquare } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";
import type { HostProfileItem } from "./types";

interface HostProfileDetailPageProps {
  profileId?: string;
  token: string;
}

export function HostProfileDetailPage({ profileId, token }: HostProfileDetailPageProps) {
  const [profile] = useState<HostProfileItem>({
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
  });

  return (
    <div className="page-container container py-6" data-testid="hpro-02-page" id="HPRO-02">
      <header className="page-header mb-6">
        <span className="badge badge-sun">HPRO-02 / HPRO-03</span>
        <h2>Host Profile: {profile.displayName}</h2>
        <PatoisPhrase phrase="Authentic Host Accreditation" translation="Verified Jamaican host biography, active listings, guest reviews, and direct contact routes." />
      </header>

      <div className="layout-grid-2-1">
        <div className="space-y-6">
          <div className="card-box p-6" id="HPRO-03">
            <div className="flex items-center gap-4 mb-4">
              <div className="w-20 h-20 bg-sun rounded-full flex items-center justify-center font-bold text-white text-3xl">
                {profile.displayName.substring(0, 1)}
              </div>
              <div>
                <h3 className="font-bold text-2xl">{profile.displayName}</h3>
                <p className="subtext"><MapPin size={14} className="inline" /> {profile.parish}, Jamaica • {profile.responseTime}</p>
                <div className="flex items-center gap-2 mt-2">
                  {profile.badges.map(b => (
                    <span key={b} className="badge badge-green text-xs"><ShieldCheck size={12} className="inline" /> {b} Host</span>
                  ))}
                </div>
              </div>
            </div>

            <p className="my-4 text-sm leading-relaxed">{profile.bio}</p>

            <div className="flex gap-3 pt-4 border-t">
              <a href="/messages" className="btn btn-primary">
                <MessageSquare size={16} /> Send Message to Host
              </a>
              {profile.linkMiUrl && (
                <a href={profile.linkMiUrl} target="_blank" rel="noreferrer" className="btn btn-outline flex items-center gap-1">
                  Link Mi Profile <ExternalLink size={16} />
                </a>
              )}
            </div>
          </div>
        </div>

        <div className="card-box sticky-top">
          <h3>Host Statistics</h3>
          <div className="info-row my-2">
            <span>Overall Rating:</span>
            <strong>{profile.ratingAverage} / 5.0</strong>
          </div>
          <div className="info-row my-2">
            <span>Completed Stays:</span>
            <strong>{profile.totalBookings} bookings</strong>
          </div>
        </div>
      </div>
    </div>
  );
}
