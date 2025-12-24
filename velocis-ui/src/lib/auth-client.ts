import { createAuthClient } from "better-auth/react";

export const authClient = createAuthClient({
  baseURL:
    import.meta.env.PUBLIC_BASE_API_URL ||
    (typeof window !== "undefined" ? window.location.origin : ""),
});

export const { signIn, signOut } = authClient;
