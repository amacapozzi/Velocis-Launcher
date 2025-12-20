import { PrismaClient } from "../../../generated/prisma/client";
import { PrismaPg } from "@prisma/adapter-pg";
import { DATABASE_CONFIG } from "@/internal/config/config";

const adapter = new PrismaPg({ connectionString: DATABASE_CONFIG.databaseUrl });
const prisma = new PrismaClient({ adapter });

export { prisma };
