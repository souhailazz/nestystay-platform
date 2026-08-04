import { Component, type ErrorInfo, type ReactNode } from "react";

type RootErrorBoundaryProps = {
  children: ReactNode;
};

type RootErrorBoundaryState = {
  error: Error | null;
};

export class RootErrorBoundary extends Component<RootErrorBoundaryProps, RootErrorBoundaryState> {
  state: RootErrorBoundaryState = {
    error: null,
  };

  static getDerivedStateFromError(error: Error): RootErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("NestyStay recovered from a page rendering error.", error, errorInfo);
  }

  render() {
    if (!this.state.error) {
      return this.props.children;
    }

    return (
      <main className="root-error-page" translate="no">
        <section>
          <span className="badge badge-sun">Recovered</span>
          <h1>Refresh this NestyStay page.</h1>
          <p>The page hit a browser rendering issue. Your booking data is safe.</p>
          <button className="btn btn-primary" onClick={() => window.location.reload()} type="button">
            Reload page
          </button>
        </section>
      </main>
    );
  }
}
