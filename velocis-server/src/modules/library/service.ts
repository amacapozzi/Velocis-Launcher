import { prisma } from "@/internal/database/db";
import type { GameModel } from "../../../generated/prisma/models/Game";

export interface PaginationParams {
  page?: number;
  limit?: number;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
    hasNext: boolean;
    hasPrev: boolean;
  };
}

export class LibraryService {
  static async getAvailableGames(
    params: PaginationParams = {}
  ): Promise<PaginatedResponse<GameModel>> {
    const page = params.page && params.page > 0 ? params.page : 1;
    const limit = params.limit && params.limit > 0 ? params.limit : 10;

    const skip = (page - 1) * limit;

    const [games, total] = await Promise.all([
      prisma.game.findMany({
        where: { isAvailable: true },
        skip,
        take: limit,
        orderBy: { createdAt: "desc" },
      }),
      prisma.game.count({ where: { isAvailable: true } }),
    ]);

    const totalPages = Math.ceil(total / limit);

    return {
      data: games,
      pagination: {
        page,
        limit,
        total,
        totalPages,
        hasNext: page < totalPages,
        hasPrev: page > 1,
      },
    };
  }
}
