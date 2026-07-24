import { PublicSearchMap } from "./PublicSearchMap";
import { PropertyDetailPage } from "./PropertyDetailPage";
import { ExperiencesPage } from "./ExperiencesPage";
import { JournalPage } from "./JournalPage";
import { LegalHelpPages } from "./LegalHelpPages";

interface PublicStateContainerProps {
  view: string;
  propertyId?: string;
}

export function PublicStateContainer({ view, propertyId }: PublicStateContainerProps) {
  if (view === "search" || view === "map" || view === "grid" || view === "explore") {
    return <PublicSearchMap view={view} />;
  }

  if (view === "detail" || view === "property-detail") {
    return <PropertyDetailPage propertyId={propertyId} />;
  }

  if (view === "experiences" || view === "tours") {
    return <ExperiencesPage view={view} />;
  }

  if (view === "journal" || view === "articles") {
    return <JournalPage view={view} />;
  }

  if (["about", "trust", "terms", "privacy", "help", "faq"].includes(view)) {
    return <LegalHelpPages view={view} />;
  }

  return <PublicSearchMap view={view} />;
}
