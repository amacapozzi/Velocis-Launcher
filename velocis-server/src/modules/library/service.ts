import { prisma } from "@/internal/database/db";

export class LibraryService {
  static async getAvailableGames() {
    return await prisma.game.findMany({ where: { isAvailable: true } });
  }
}
