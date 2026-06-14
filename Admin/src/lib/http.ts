import "server-only";
import { API_URL } from "@/lib/api";

/** Friendly message shown when the API cannot be reached at all (network error). */
export const NETWORK_ERROR =
  "Could not reach the API. Please check it is running and try again.";

/** Friendly message shown when the admin session has expired (401). */
export const SESSION_EXPIRED = "Your session has expired. Please log in again.";

/**
 * Performs a fetch against the API and never throws. Returns the {@link Response}
 * on success, or `null` when the API is unreachable (connection refused, DNS, timeout…).
 */
export async function tryFetch(
  path: string,
  init?: RequestInit,
): Promise<Response | null> {
  try {
    return await fetch(`${API_URL}${path}`, { cache: "no-store", ...init });
  } catch {
    return null;
  }
}

/**
 * Parses an RFC 7807 ProblemDetails (or simple `{ message }`) body into a single
 * human-friendly message. Falls back to "<fallback> (status)." when the body is empty
 * or not JSON.
 */
export async function readProblem(res: Response, fallback: string): Promise<string> {
  try {
    const body = (await res.json()) as {
      detail?: string;
      title?: string;
      message?: string;
      errors?: Record<string, string[]>;
    };
    if (body.errors) {
      const messages = Object.values(body.errors).flat();
      if (messages.length) return messages.join(" ");
    }
    if (body.detail) return body.detail;
    if (body.message) return body.message;
    if (body.title) return body.title;
  } catch {
    // body was empty or not JSON
  }
  return `${fallback} (${res.status}).`;
}

/** Safely parses a JSON response body, returning `null` instead of throwing. */
export async function safeJson<T>(res: Response): Promise<T | null> {
  try {
    return (await res.json()) as T;
  } catch {
    return null;
  }
}
