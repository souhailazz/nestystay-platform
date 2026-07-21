export function LoadingState({ label = "Loading Nesty Stay data" }: { label?: string }) {
  return (
    <div className="ui-state">
      <span className="ui-spinner" />
      <strong>Tek Time</strong>
      <p>{label}</p>
      <small>Taking a moment to load the latest NestyStay data.</small>
      <div className="loading-skeleton" aria-hidden="true">
        <span className="loading-skeleton__nav" />
        <div className="loading-skeleton__cards">
          {[0, 1, 2].map((item) => (
            <span className="loading-skeleton__card" key={item}>
              <i />
              <b />
              <b />
            </span>
          ))}
        </div>
        <span className="loading-skeleton__footer" />
      </div>
    </div>
  );
}
