import { getAvailableGames } from "@/services/library-service";
import type { LibraryGame } from "@/types/library";
import { useEffect, useState } from "react";

export const useLibrary = () => {
  const [library, setLibrary] = useState<LibraryGame[]>([]);
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function fetchLibrary() {
      setIsLoading(true);
      try {
        const data = await getAvailableGames();
        setLibrary(data);
      } catch (err) {
        setError(err instanceof Error ? err : new Error(String(err)));
      } finally {
        setIsLoading(false);
      }
    }
    fetchLibrary();
  }, []);

  return { library, isLoading };
};
