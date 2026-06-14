// Cookie names shared between middleware (edge) and server code.
// Kept free of any `next/headers` imports so the edge runtime can use them.
export const TOKEN_COOKIE = "nika_admin_token";
export const USER_COOKIE = "nika_admin_user";

// Upload limits. Kept in one place so the client guard, the server action guard,
// the Next.js server-action body limit and the API all agree.
export const MAX_UPLOAD_BYTES = 10 * 1024 * 1024;
export const MAX_UPLOAD_LABEL = "10 MB";

