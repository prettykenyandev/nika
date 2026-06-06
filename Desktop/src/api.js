"use strict";

/**
 * Thin client for the Nika Fitness API. All calls run in the Electron main
 * process (Node), which sidesteps the API's browser-origin CORS rule the same
 * way the Next.js admin's server actions do.
 *
 * Every function resolves to a normalised result:
 *   { ok: true, data }                       — success
 *   { ok: false, network: true }             — couldn't reach the server
 *   { ok: false, status, unauthorized?, error } — server responded with an error
 */

const JSON_HEADERS = { "Content-Type": "application/json" };

async function request(url, options = {}) {
  try {
    const res = await fetch(url, options);
    if (res.ok) {
      const data = res.status === 204 ? null : await res.json().catch(() => null);
      return { ok: true, data, status: res.status };
    }
    const error = await readError(res);
    return { ok: false, status: res.status, unauthorized: res.status === 401, error };
  } catch {
    // DNS failure, connection refused, timeout, offline, etc.
    return { ok: false, network: true, error: "Could not reach the server." };
  }
}

async function readError(res) {
  try {
    const body = await res.json();
    if (typeof body === "string") return body;
    if (body?.errors) {
      const first = Object.values(body.errors)[0];
      if (Array.isArray(first) && first.length) return first[0];
    }
    return body?.detail || body?.message || body?.title || `Request failed (${res.status}).`;
  } catch {
    return `Request failed (${res.status}).`;
  }
}

function authHeaders(token) {
  return { ...JSON_HEADERS, Authorization: `Bearer ${token}` };
}

const api = {
  ping(apiUrl) {
    return request(`${apiUrl}/api/catalog/categories`);
  },

  login(apiUrl, email, password) {
    return request(`${apiUrl}/api/auth/login`, {
      method: "POST",
      headers: JSON_HEADERS,
      body: JSON.stringify({ email, password }),
    });
  },

  lookup(apiUrl, token, sku) {
    const url = `${apiUrl}/api/admin/pos/lookup?sku=${encodeURIComponent(sku)}`;
    return request(url, { headers: authHeaders(token) });
  },

  createSale(apiUrl, token, payload) {
    return request(`${apiUrl}/api/admin/pos/sales`, {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify(payload),
    });
  },

  listProducts(apiUrl, page = 1, pageSize = 50) {
    return request(`${apiUrl}/api/catalog/products?page=${page}&pageSize=${pageSize}`);
  },

  getProduct(apiUrl, slug) {
    return request(`${apiUrl}/api/catalog/products/${encodeURIComponent(slug)}`);
  },
};

module.exports = { api };
