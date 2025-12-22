import { Elysia } from "elysia";
import { LibraryService } from "@/modules/library/service";
import { ApiError, ErrorCode } from "@/internal/errors/error-handler";

export const libraryRouter = new Elysia({ prefix: "/library" }).get(
  "/available",
  async ({ query }) => {
    const pageParam = query.page;
    const limitParam = query.limit;

    const page =
      pageParam && !isNaN(Number(pageParam))
        ? Math.max(1, parseInt(pageParam as string, 10))
        : undefined;
    const limit =
      limitParam && !isNaN(Number(limitParam))
        ? Math.max(1, parseInt(limitParam as string, 10))
        : undefined;

    const result = await LibraryService.getAvailableGames({ page, limit });

    if (result.data.length === 0)
      throw new ApiError(ErrorCode.NOT_AVAILABLE_GAMES);

    return result;
  }
);
