import { useInfiniteQuery } from "@tanstack/react-query";
import { getAvailableGames } from "@/services/library-service";
import { useMemo } from "react";
import type { LibraryGame } from "@/types/library";

const ITEMS_PER_PAGE = 20;

export const useLibraryInfinite = () => {
  const {
    data,
    isLoading,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useInfiniteQuery({
    queryKey: ["library", "infinite"],
    queryFn: async ({ pageParam = 1 }) => {
      const response = await getAvailableGames({
        page: pageParam,
        limit: ITEMS_PER_PAGE,
      });
      return response;
    },
    getNextPageParam: (lastPage) => {
      if (lastPage.pagination.hasNext) {
        return lastPage.pagination.page + 1;
      }
      return undefined;
    },
    initialPageParam: 1,
    staleTime: Infinity,
    gcTime: Infinity,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  const library = useMemo(() => {
    if (!data?.pages) return [];
    return data.pages.flatMap((page) => page.data);
  }, [data]);

  return {
    library,
    isLoading,
    isFetchingNextPage,
    hasNextPage: hasNextPage ?? false,
    fetchNextPage,
  };
};
