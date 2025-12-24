export interface LibraryGame {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  snapshotUrl: string;
  sizeBytes: number;
  isAvailable: boolean;
}

export interface PaginationInfo {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
  hasNext: boolean;
  hasPrev: boolean;
}

export interface PaginatedLibraryResponse {
  data: LibraryGame[];
  pagination: PaginationInfo;
}