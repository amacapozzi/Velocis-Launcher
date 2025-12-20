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

export interface ErrorResponse {
  success: false;
  error: {
    code: string;
    message: string;
    details?: unknown;
    timestamp: string;
  };
}

export class ApiError extends Error {
  public readonly code: string;
  public readonly statusCode: number;
  public readonly details?: unknown;
  public readonly timestamp: string;

  constructor(errorCode: ErrorCode, details?: unknown, customMessage?: string) {
    const errorConfig = ERROR_CODES[errorCode];

    super(customMessage || errorConfig.message);

    this.name = "ApiError";
    this.code = errorConfig.code;
    this.statusCode = errorConfig.statusCode;
    this.details = details;
    this.timestamp = new Date().toISOString();

    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, ApiError);
    }
  }

  toResponse(): ErrorResponse {
    return {
      success: false,
      error: {
        code: this.code,
        message: this.message,
        details: this.details,
        timestamp: this.timestamp,
      },
    };
  }

  static create(
    errorCode: ErrorCode,
    details?: unknown,
    customMessage?: string
  ): ApiError {
    return new ApiError(errorCode, details, customMessage);
  }
}
