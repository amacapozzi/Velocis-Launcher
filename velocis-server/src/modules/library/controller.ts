import { Elysia } from "elysia";
import { LibraryService } from "@/modules/library/service";
import { ApiError, ErrorCode } from "@/internal/errors/error-handler";

export const libraryRouter = new Elysia({ prefix: "/library" }).get(
  "/available",
  async () => {
    const availableGames = await LibraryService.getAvailableGames();

    if (availableGames.length == 0)
      throw new ApiError(ErrorCode.NOT_AVAILABLE_GAMES);

    return availableGames;
  }
);
