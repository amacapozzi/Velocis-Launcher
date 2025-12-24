import { betterAuth } from "better-auth";
import { GOOGLE_CLIENT_ID } from "astro:env/client";
import {
  GOOGLE_SECRET_ID,
  BETTER_AUTH_SECRET,
  BETTER_AUTH_URL,
} from "astro:env/server";

export const auth = betterAuth({
  baseURL: BETTER_AUTH_URL,
  secret: BETTER_AUTH_SECRET,
  emailAndPassword: {
    enabled: true,
  },
  socialProviders: {
    google: {
      clientId: GOOGLE_CLIENT_ID,
      clientSecret: GOOGLE_SECRET_ID,
    },
  },
});
