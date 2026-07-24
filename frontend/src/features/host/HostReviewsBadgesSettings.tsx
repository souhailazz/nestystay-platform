import { useState } from "react";
import { Star, ShieldCheck, Award, MessageSquare, Flag, RefreshCw, Lock } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";

interface HostReviewsBadgesSettingsProps {
  view: string;
  token: string;
}

export function HostReviewsBadgesSettings({ view, token }: HostReviewsBadgesSettingsProps) {
  const [replies, setReplies] = useState<Record<string, string>>({
    "rev-1": "Thank you for staying at Ocho Rios Verified Villa!"
  });
  const [replyInput, setReplyInput] = useState("");

  const isReviews = view === "reviews";
  const isBadges = view === "badges";

  return (
    <div className="page-container container py-6" data-testid={isReviews ? "host-12-page" : "host-13-page"} id={isReviews ? "HOST-12" : "HOST-13"}>
      <header className="page-header mb-6">
        <span className="badge badge-sun">{isReviews ? "HOST-12" : "HOST-13"}</span>
        <h2>{isReviews ? "Guest Reviews & Host Replies" : "Host Badges & Verification Settings"}</h2>
        <PatoisPhrase phrase="Host Reputation & Accreditation" translation="Manage guest reviews, verified host badge unlocks, and 2FA security settings." />
      </header>

      {isReviews ? (
        <div className="space-y-4 max-w-3xl">
          <div className="card-box">
            <div className="flex justify-between items-center mb-2">
              <h3 className="font-bold">Ocho Rios Verified Villa</h3>
              <div className="flex text-sun"><Star size={16} fill="currentColor" /><Star size={16} fill="currentColor" /><Star size={16} fill="currentColor" /><Star size={16} fill="currentColor" /><Star size={16} fill="currentColor" /></div>
            </div>
            <p className="subtext font-medium">"Amazing stay, clean, beautiful view!" - Traveler Guest</p>
            {replies["rev-1"] ? (
              <div className="bg-sun-light p-3 rounded mt-3 text-sm">
                <strong>Host Reply:</strong> {replies["rev-1"]}
              </div>
            ) : (
              <div className="mt-3">
                <input type="text" className="input-control mb-2" placeholder="Write reply to guest..." value={replyInput} onChange={(e) => setReplyInput(e.target.value)} />
                <button type="button" className="btn btn-primary btn-sm" onClick={() => setReplies({ ...replies, "rev-1": replyInput })}>Reply</button>
              </div>
            )}
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 max-w-4xl">
          <div className="card-box">
            <h3 className="flex items-center gap-2 mb-2"><Award className="text-sun" size={20} /> Verified Host Badge</h3>
            <span className="badge badge-green mb-3">ACTIVE</span>
            <p className="subtext">Unlocked after eKYC verification and property ownership check.</p>
          </div>

          <div className="card-box">
            <h3 className="flex items-center gap-2 mb-2"><ShieldCheck className="text-green" size={20} /> Trusted Host Badge</h3>
            <span className="badge badge-sun mb-3">ELIGIBLE</span>
            <p className="subtext">Requires 5 completed bookings and 4.8+ rating.</p>
            <button type="button" className="btn btn-primary btn-sm mt-3" onClick={() => alert("Badge unlock submitted.")}>Apply for Badge</button>
          </div>
        </div>
      )}
    </div>
  );
}
