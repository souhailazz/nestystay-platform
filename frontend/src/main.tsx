import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import App from "./App";
import { RootErrorBoundary } from "./components/RootErrorBoundary";
import { installDomCompatibilityGuards } from "./domCompatibility";
import "./index.css";

installDomCompatibilityGuards();

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <RootErrorBoundary>
      <App />
    </RootErrorBoundary>
  </StrictMode>,
);
