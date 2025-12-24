import { LIBRARY_BASE_URL } from "@/config/config";
import { json } from "@/lib/json";
import type { LibraryGame, PaginatedLibraryResponse } from "@/types/library";

export interface GetAvailableGamesParams {
  page?: number;
  limit?: number;
}

interface ApiGameResponse {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  snapshotUrl: string;
  sizeInKB: number;
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
}

interface ApiPaginatedResponse {
  data: ApiGameResponse[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
    hasNext: boolean;
    hasPrev: boolean;
  };
}

export const getAvailableGames = async (
  params?: GetAvailableGamesParams
): Promise<PaginatedLibraryResponse> => {
  const searchParams = new URLSearchParams();
  if (params?.page !== undefined) {
    searchParams.set("page", params.page.toString());
  }
  if (params?.limit !== undefined) {
    searchParams.set("limit", params.limit.toString());
  }

  const queryString = searchParams.toString();
  const url = queryString
    ? `${LIBRARY_BASE_URL}/available?${queryString}`
    : `${LIBRARY_BASE_URL}/available`;

  const apiResponse = await json<ApiPaginatedResponse>(url);

  // Transform sizeInKB to sizeBytes
  const transformedData: LibraryGame[] = apiResponse.data.map((game) => ({
    id: game.id,
    name: game.name,
    description: game.description,
    imageUrl: game.imageUrl,
    snapshotUrl: game.snapshotUrl,
    sizeBytes:
      game.sizeInKB != null && !isNaN(game.sizeInKB) ? game.sizeInKB * 1024 : 0,
    isAvailable: game.isAvailable,
  }));

  return {
    data: transformedData,
    pagination: apiResponse.pagination,
  };
};
