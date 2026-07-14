export function LoadingState({ label = "Loading Nesty Stay data" }: { label?: string }) {
  return (
    <div className="ui-state">
      <span className="ui-spinner" />
      <p>{label}</p>
    </div>
  );
}
