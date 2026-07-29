import { PatoisPhrase, usePatois } from "../../lib/patois";

/**
 * Design System v2 patois block (DS-07) — styled wrapper around the mandatory
 * PatoisPhrase channel. Fraunces italic phrase (yellow on dark / Nesty Deep on
 * light), English translation underneath in muted gray. When the patois toggle
 * is off, renders the English fallback so the surface never goes blank.
 */
export function PatoisBlock({
  phrase,
  translation,
  tone = "dark",
  fallback,
  size = 30,
  className = "",
}: {
  phrase: string;
  translation: string;
  tone?: "dark" | "light";
  /** Plain-English replacement when patois is toggled off. Defaults to the translation. */
  fallback?: string;
  /** Font size (px) of the Fraunces italic phrase. */
  size?: number;
  className?: string;
}) {
  const { showPatois } = usePatois();

  return (
    <div className={`ns-patois ns-patois--${tone} ${className}`} style={{ fontSize: size }}>
      {showPatois ? (
        <PatoisPhrase phrase={phrase} translation={translation} />
      ) : (
        <span className="ns-patois__fallback" style={{ fontSize: 15 }}>
          {fallback ?? translation}
        </span>
      )}
    </div>
  );
}
