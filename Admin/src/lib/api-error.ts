export interface ApiErrorBody {
  detail?: string;
  title?: string;
  message?: string;
  errors?: Record<string, string[]>;
}

/**
 * Pick the most useful human-readable message from an API error body,
 * preferring field validation errors, then problem detail/message/title, and
 * finally a generic fallback that includes the status code.
 */
export function pickApiErrorMessage(
  body: ApiErrorBody,
  status: number,
  fallback: string,
): string {
  if (body.errors) {
    const messages = Object.values(body.errors).flat();
    if (messages.length) return messages.join(" ");
  }
  if (body.detail) return body.detail;
  if (body.message) return body.message;
  if (body.title) return body.title;
  return `${fallback} (${status}).`;
}

/** Read an error response body and reduce it to a single message. */
export async function readApiError(res: Response, fallback: string): Promise<string> {
  try {
    const body = (await res.json()) as ApiErrorBody;
    return pickApiErrorMessage(body, res.status, fallback);
  } catch {
    // non-JSON error body; fall back to a status message
    return `${fallback} (${res.status}).`;
  }
}
