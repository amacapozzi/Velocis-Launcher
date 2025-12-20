import { env } from "elysia";

if (!env.DATABASE_URL)
  throw new Error("Please set database url in environments variables");

export const DATABASE_CONFIG = {
  databaseUrl: env.DATABASE_URL!,
};
