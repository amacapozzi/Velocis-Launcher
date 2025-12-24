import { useLibraryInfinite } from "@/hooks/use-library-infinite";
import { useState, useEffect } from "react";
import type { LibraryGame } from "@/types/library";
import { DownloadIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { formatFileSize } from "@/lib/utils";
import { LibraryInfiniteScroll } from "./LibraryInfiniteScroll";
import { QueryProvider } from "./QueryProvider";

function LibrarySectionContent() {
  const { library, isLoading, isFetchingNextPage, hasNextPage, fetchNextPage } =
    useLibraryInfinite();
  const [selectedGame, setSelectedGame] = useState<LibraryGame | null>(null);

  useEffect(() => {
    if (library && library.length > 0 && !selectedGame) {
      setSelectedGame(library[0]);
    }
  }, [library, selectedGame]);

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-8">
        <div className="text-white/55">Loading...</div>
      </div>
    );
  }

  if (!library || library.length === 0) {
    return (
      <div className="flex justify-center items-center py-8">
        <div className="text-white/55">No games available</div>
      </div>
    );
  }

  return (
    <>
      <div className="relative -mx-8 -my-6 px-8 py-6 overflow-hidden rounded-3xl">
        <div
          className="absolute inset-0 opacity-[0.03] pointer-events-none"
          style={{
            backgroundImage: `
              linear-gradient(rgba(255, 255, 255, 0.1) 1px, transparent 1px),
              linear-gradient(90deg, rgba(255, 255, 255, 0.1) 1px, transparent 1px)
            `,
            backgroundSize: "50px 50px",
          }}
        />

        <div className="relative z-10">
          <div className="grid grid-cols-4 gap-8 w-full">
            {library.map((game) => (
              <div
                key={game.id}
                className="group flex flex-col gap-0 cursor-pointer"
              >
                <div className="relative aspect-video overflow-hidden rounded-lg border border-white/10 bg-linear-to-br from-white/5 to-transparent group-hover:border-white/20 transition-all duration-500">
                  <Badge className="absolute top-4 left-4 z-20 bg-black/80 backdrop-blur-md text-white/90 border-white/20 shadow-lg">
                    {game.sizeBytes != null &&
                    !isNaN(game.sizeBytes) &&
                    game.sizeBytes > 0
                      ? formatFileSize(game.sizeBytes)
                      : "0 B"}
                  </Badge>

                  <img
                    className="w-full h-full object-cover transition-all duration-700 group-hover:scale-110 group-hover:brightness-110"
                    src={game.imageUrl || ""}
                    alt={game.name || ""}
                    draggable={false}
                  />

                  <div className="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-300 z-30 pointer-events-none">
                    <div className="w-10 h-10 rounded-md bg-black/95 backdrop-blur-sm flex items-center justify-center shadow-2xl border border-white/20 pointer-events-auto cursor-pointer hover:scale-110 active:scale-95 transition-transform duration-200">
                      <DownloadIcon className="h-6 w-6 text-white" />
                    </div>
                  </div>
                </div>

                <div className="flex flex-col gap-3 mt-5 px-1">
                  <h3 className="text-2xl font-bold leading-tight bg-linear-to-r from-white/90 via-white to-white/90 bg-clip-text text-transparent group-hover:from-white group-hover:via-white group-hover:to-white transition-all duration-300">
                    {game.name}
                  </h3>
                  <p className="line-clamp-2 text-[15px] leading-relaxed text-white/60 group-hover:text-white/70 transition-colors">
                    {game.description}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
      <LibraryInfiniteScroll
        onLoadMore={() => fetchNextPage()}
        hasNextPage={hasNextPage ?? false}
        isFetchingNextPage={isFetchingNextPage}
      />
    </>
  );
}

export default function LibrarySection() {
  return (
    <QueryProvider>
      <LibrarySectionContent />
    </QueryProvider>
  );
}
