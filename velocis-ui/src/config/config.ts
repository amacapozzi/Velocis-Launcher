import { PUBLIC_BASE_API_URL } from "astro:env/client";

export const API_CONFIG = {
  baseUrl: PUBLIC_BASE_API_URL,
};

export const LIBRARY_BASE_URL = `${API_CONFIG.baseUrl}/library`;
