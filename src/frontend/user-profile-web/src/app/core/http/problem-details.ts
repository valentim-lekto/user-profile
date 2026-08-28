import { HttpErrorResponse } from '@angular/common/http';

export interface ProblemDetails {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function toProblemDetails(error: unknown): ProblemDetails {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0 };
  }

  if (!isRecord(error.error)) {
    return { status: error.status };
  }

  return {
    status: typeof error.error['status'] === 'number' ? error.error['status'] : error.status,
    title: readString(error.error['title']),
    detail: readString(error.error['detail']),
    errors: readValidationErrors(error.error['errors']),
  };
}

function readString(value: unknown): string | undefined {
  return typeof value === 'string' ? value : undefined;
}

function readValidationErrors(value: unknown): Record<string, string[]> | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const errors = Object.entries(value).flatMap(([field, messages]) => {
    if (!Array.isArray(messages) || !messages.every((message) => typeof message === 'string')) {
      return [];
    }

    return [[field, messages] as const];
  });

  return errors.length > 0 ? Object.fromEntries(errors) : undefined;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
