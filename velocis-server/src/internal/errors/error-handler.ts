import { ErrorCode, ERROR_CODES } from "./error-codes";
export { ErrorCode } from "./error-codes";

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
