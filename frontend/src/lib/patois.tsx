import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

const STORAGE_KEY = "nestyStay.showPatois";

type PatoisContextValue = {
  showPatois: boolean;
  setShowPatois: (value: boolean) => void;
};

const PatoisContext = createContext<PatoisContextValue | null>(null);

export function PatoisProvider({ children }: { children: ReactNode }) {
  const [showPatois, setShowPatoisState] = useState(() => {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    return stored === null ? true : stored === "true";
  });

  useEffect(() => {
    window.localStorage.setItem(STORAGE_KEY, String(showPatois));
  }, [showPatois]);

  const value = useMemo(
    () => ({
      showPatois,
      setShowPatois: setShowPatoisState,
    }),
    [showPatois],
  );

  return <PatoisContext.Provider value={value}>{children}</PatoisContext.Provider>;
}

export function usePatois() {
  const context = useContext(PatoisContext);
  if (!context) {
    throw new Error("usePatois must be used inside PatoisProvider.");
  }
  return context;
}

export function PatoisPhrase({
  phrase,
  translation,
  className = "patois-line",
}: {
  phrase: string;
  translation: string;
  className?: string;
}) {
  const { showPatois } = usePatois();
  if (!showPatois) return null;

  return (
    <span aria-label={`${phrase} - English: ${translation}`} className={className}>
      <strong>{phrase}</strong>
      <small>{translation}</small>
    </span>
  );
}

export function PatoisToggle() {
  const { showPatois, setShowPatois } = usePatois();
  return (
    <label className="patois-toggle">
      <input checked={showPatois} type="checkbox" onChange={(event) => setShowPatois(event.target.checked)} />
      <span>Jamaican Patois greetings</span>
      <small>Show the island personality.</small>
    </label>
  );
}
