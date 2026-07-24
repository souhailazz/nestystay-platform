import { useState } from "react";
import { BookOpen, Clock, User, ArrowRight } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";
import type { JournalArticleItem } from "./types";

interface JournalPageProps {
  view: string;
}

export function JournalPage({ view }: JournalPageProps) {
  const [articles, setArticles] = useState<JournalArticleItem[]>([
    {
      id: "art-1",
      title: "The Ultimate Guide to Vacation Rentals in Ocho Rios",
      slug: "ocho-rios-travel-guide",
      category: "Travel Guide",
      authorName: "NestyStay Travel Team",
      publishedAt: "2026-07-01",
      readTimeMinutes: 5,
      summary: "Discover top verified villas, hidden waterfalls, and local food spots in St. Ann parish.",
      content: "Full article content covering Ocho Rios beaches, Dunn's River Falls, and verified host safety tips.",
      imageUrl: "https://images.unsplash.com/photo-1540555700478-4be289fbecef",
      relatedPropertyIds: ["11111111-1111-4111-8111-111111111111"]
    }
  ]);

  return (
    <div className="page-container container py-6" data-testid="pub-11-page" id="PUB-11">
      <header className="page-header mb-6">
        <span className="badge badge-sun">PUB-11</span>
        <h2>NestyStay Journal & Culture Stories</h2>
        <PatoisPhrase phrase="Jamaican Travel Insights & Culture" translation="Discover local travel guides, food spots, and Jamaican heritage stories." />
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 max-w-4xl">
        {articles.map((art) => (
          <div key={art.id} className="card-box flex flex-col justify-between hover:shadow-md transition">
            <div>
              <span className="badge badge-sun mb-2">{art.category}</span>
              <h3 className="font-bold text-xl">{art.title}</h3>
              <p className="subtext text-xs my-2"><User size={12} className="inline" /> {art.authorName} • <Clock size={12} className="inline" /> {art.readTimeMinutes} min read</p>
              <p className="text-sm">{art.summary}</p>
            </div>

            <div className="mt-4 pt-3 border-t">
              <a href={`/journal/${art.slug}`} className="btn btn-outline btn-sm w-full text-center">
                Read Full Story <ArrowRight size={14} />
              </a>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
