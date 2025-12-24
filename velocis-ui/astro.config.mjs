// @ts-check

import tailwindcss from "@tailwindcss/vite";
import { defineConfig, envField } from "astro/config";
import react from "@astrojs/react";
// https://astro.build/config
export default defineConfig({
  env: {
    schema: {
      PUBLIC_BASE_API_URL: envField.string({
        context: "client",
        access: "public",
        optional: false,
      }),
      BETTER_AUTH_SECRET: envField.string({
        context: "server",
        access: "secret",
        optional: false,
      }),
      BETTER_AUTH_URL: envField.string({
        context: "server",
        access: "public",
        optional: false,
      }),
      GOOGLE_CLIENT_ID: envField.string({
        context: "client",
        access: "public",
        optional: false,
      }),
      GOOGLE_SECRET_ID: envField.string({
        context: "server",
        access: "secret",
        optional: false,
      }),
    },
  },
  vite: {
    plugins: [tailwindcss()],
  },
  server: {
    allowedHosts: [".trycloudflare.com"],
  },
  integrations: [react()],
});
