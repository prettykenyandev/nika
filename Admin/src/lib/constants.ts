// Cookie names shared between middleware (edge) and server code.
// Kept free of any `next/headers` imports so the edge runtime can use them.
export const TOKEN_COOKIE = "nika_admin_token";
export const USER_COOKIE = "nika_admin_user";
