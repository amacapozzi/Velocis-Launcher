interface ImportMetaEnv {
  readonly PUBLIC_BASE_API_URL: string;
  readonly BETTER_AUTH_SECRET: string;
  readonly BETTER_AUTH_URL: string;
  readonly GOOGLE_CLIENT_ID: string;
  readonly GOOGLE_SECRET_ID: string;

}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
