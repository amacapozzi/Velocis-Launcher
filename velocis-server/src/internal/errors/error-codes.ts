export enum ErrorCode {
  NOT_FOUND = "NOT_FOUND",
  NOT_AVAILABLE_GAMES = "NOT_AVAILABLE_GAMES",
}

export const ERROR_CODES: Record<
  ErrorCode,
  { code: string; message: string; statusCode: number }
> = {
  [ErrorCode.NOT_FOUND]: {
    code: "RES_3001",
    message: "Resource not found.",
    statusCode: 404,
  },
  [ErrorCode.NOT_AVAILABLE_GAMES]: {
    code: "LIB_3002",
    message: "No games available.",
    statusCode: 404,
  },
} as const;

