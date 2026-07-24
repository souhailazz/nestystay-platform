import { useState } from "react";
import { Save, User, Camera, Check } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";

interface HostProfileEditPageProps {
  token: string;
}

export function HostProfileEditPage({ token }: HostProfileEditPageProps) {
  const [displayName, setDisplayName] = useState("Island Villa Hosting");
  const [parish, setParish] = useState("St. Ann");
  const [bio, setBio] = useState("Premier vacation villa host in Ocho Rios & Montego Bay with 5+ years of verified Jamaican hospitality.");
  const [isPublic, setIsPublic] = useState(true);
  const [notice, setNotice] = useState<string | null>(null);

  function handleSave() {
    setNotice("Host profile updated and saved.");
  }

  return (
    <div className="page-container container py-6" data-testid="hpro-04-page" id="HPRO-04">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">HPRO-04 / HPRO-05</span>
          <h2>Edit Host Profile & Link Mi Settings</h2>
          <PatoisPhrase phrase="Update Yuh Host Biography" translation="Customize your public host profile, parish location, and Link Mi social links." />
        </div>
        <button type="button" className="btn btn-primary" onClick={handleSave}>
          <Save size={16} /> Save Profile
        </button>
      </header>

      {notice && <div className="notice-panel mb-4">{notice}</div>}

      <div className="card-box max-w-3xl mx-auto space-y-4" id="HPRO-05">
        <div className="field-group">
          <label className="field-label">Host Display Name</label>
          <input type="text" className="input-control" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </div>

        <div className="field-group">
          <label className="field-label">Parish Location</label>
          <select className="input-control" value={parish} onChange={(e) => setParish(e.target.value)}>
            <option value="St. Ann">St. Ann (Ocho Rios)</option>
            <option value="St. James">St. James (Montego Bay)</option>
            <option value="Westmoreland">Westmoreland (Negril)</option>
            <option value="Portland">Portland (Port Antonio)</option>
          </select>
        </div>

        <div className="field-group">
          <label className="field-label">Host Biography</label>
          <textarea className="input-control" rows={4} value={bio} onChange={(e) => setBio(e.target.value)} />
        </div>

        <label className="checkbox-card">
          <input type="checkbox" checked={isPublic} onChange={(e) => setIsPublic(e.target.checked)} />
          <span>Make profile public in Verified Host Directory</span>
        </label>
      </div>
    </div>
  );
}
