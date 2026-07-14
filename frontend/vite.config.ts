import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes("node_modules")) {
            return;
          }

          if (id.includes("react") || id.includes("scheduler")) {
            return "react";
          }

          if (id.includes("framer-motion") || id.includes("gsap") || id.includes("motion-dom")) {
            return "motion";
          }

          if (id.includes("lucide-react") || id.includes("lucide")) {
            return "icons";
          }

          return "vendor";
        },
      },
    },
  },
  server: {
    proxy: {
      "/api": {
        target: "http://localhost:5019",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
