import { useState } from "react";
import { Sparkles, MapPin, Clock, Users, ArrowRight } from "lucide-react";
import { formatMoney } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import type { JamaicanExperience } from "./types";

interface ExperiencesPageProps {
  view: string;
}

export function ExperiencesPage({ view }: ExperiencesPageProps) {
  const [experiences, setExperiences] = useState<JamaicanExperience[]>([
    {
      id: "exp-1",
      title: "Dunn's River Falls & Ocho Rios Catamaran Cruise",
      parish: "St. Ann",
      category: "Adventure",
      pricePerPerson: 95,
      currency: "USD",
      durationHours: 5,
      imageUrl: "https://images.unsplash.com/photo-1540555700478-4be289fbecef",
      description: "Climb Jamaica's world-famous waterfall and sail along the Ocho Rios coast with open bar and live reggae music.",
      includedItems: ["Roundtrip hotel transport", "Catamaran cruise", "Waterfall guide", "Snorkeling equipment"]
    },
    {
      id: "exp-2",
      title: "Authentic Jamaican Jerk & Rum Tasting Tour",
      parish: "St. James",
      category: "Culinary",
      pricePerPerson: 75,
      currency: "USD",
      durationHours: 3,
      imageUrl: "https://images.unsplash.com/photo-1540555700478-4be289fbecef",
      description: "Taste authentic Scotch bonnet Jerk Chicken and sample aged Jamaican Appleton Estate rum with local chefs.",
      includedItems: ["Jerk tasting platter", "Rum sampling", "Recipe card", "Chef Q&A"]
    }
  ]);

  return (
    <div className="page-container container py-6" data-testid="pub-05-page" id="PUB-05">
      <header className="page-header mb-6">
        <span className="badge badge-sun">PUB-05 / PUB-08</span>
        <h2>Jamaican Local Experiences</h2>
        <PatoisPhrase phrase="Authentic Jamaican Adventures" translation="Book curated tours, river rafting, rum tastings, and reggae heritage experiences." />
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6" id="PUB-08">
        {experiences.map((exp) => (
          <div key={exp.id} className="card-box flex flex-col justify-between hover:shadow-md transition">
            <div>
              <div className="flex justify-between items-start mb-2">
                <span className="badge badge-sun">{exp.category}</span>
                <span className="subtext text-xs"><Clock size={12} className="inline" /> {exp.durationHours} hours</span>
              </div>
              <h3 className="font-bold text-xl">{exp.title}</h3>
              <p className="subtext mt-1"><MapPin size={14} className="inline" /> {exp.parish}, Jamaica</p>
              <p className="my-3 text-sm">{exp.description}</p>
            </div>

            <div className="flex justify-between items-center mt-4 pt-3 border-t">
              <strong className="text-xl text-sun">{formatMoney(exp.pricePerPerson, exp.currency)} <span className="text-xs font-normal">/ person</span></strong>
              <button type="button" className="btn btn-primary btn-sm" onClick={() => alert(`Booking initiated for ${exp.title}`)}>
                Book Experience <ArrowRight size={14} />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
