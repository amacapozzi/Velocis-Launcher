import { useEffect, useRef } from "react";
import { Spinner } from "@/components/ui/spinner";
interface LibraryInfiniteScrollProps {
  onLoadMore: () => void;
  hasNextPage: boolean;
  isFetchingNextPage: boolean;
}

export const LibraryInfiniteScroll = ({
  onLoadMore,
  hasNextPage,
  isFetchingNextPage,
}: LibraryInfiniteScrollProps) => {
  const observerTarget = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
          onLoadMore();
        }
      },
      { threshold: 0.1 }
    );

    const currentTarget = observerTarget.current;
    if (currentTarget) {
      observer.observe(currentTarget);
    }

    return () => {
      if (currentTarget) {
        observer.unobserve(currentTarget);
      }
    };
  }, [hasNextPage, isFetchingNextPage, onLoadMore]);

  if (!hasNextPage) {
    return null;
  }

  return (
    <div ref={observerTarget} className="h-20 flex items-center justify-center">
      {isFetchingNextPage && (
        <div className="flex items-center gap-3">
          <Spinner />
        </div>
      )}
    </div>
  );
};
