"use strict";

const path = require("path");
const { app, BrowserWindow, ipcMain } = require("electron");
const { Store } = require("./src/store");
const { api } = require("./src/api");
const {
  normalizeSku,
  isValidMpesaPhone,
  saleTotal,
  saleCurrency,
  isSessionActive,
  adjustCacheStock: adjustStock,
  buildSalePayload,
} = require("./src/pos-core");

const DEFAULT_API_URL = process.env.NIKA_API_URL || "http://localhost:5087";
const PING_INTERVAL_MS = 8000;

let mainWindow = null;
let store = null;
let online = false;
let syncing = false;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function apiUrl() {
  return store.get("settings").apiUrl || DEFAULT_API_URL;
}

function auth() {
  return store.get("auth");
}

function isLoggedIn() {
  return isSessionActive(auth());
}

function pendingCount() {
  return store.get("outbox").filter((s) => s.status !== "failed").length;
}

function statusPayload() {
  const a = auth();
  return {
    online,
    syncing,
    loggedIn: isLoggedIn(),
    pending: pendingCount(),
    failed: store.get("outbox").filter((s) => s.status === "failed").length,
    cacheSize: Object.keys(store.get("cache")).length,
    apiUrl: apiUrl(),
    user: a ? { email: a.email, fullName: a.fullName } : null,
  };
}

function broadcastStatus() {
  mainWindow?.webContents.send("pos:status", statusPayload());
}

const normSku = normalizeSku;

function cacheVariant(variant) {
  store.update("cache", (cache) => {
    cache[normSku(variant.sku)] = variant;
    return cache;
  });
}

function adjustCacheStock(variantId, delta) {
  store.update("cache", (cache) => adjustStock(cache, variantId, delta));
}

// ---------------------------------------------------------------------------
// Connectivity + background sync
// ---------------------------------------------------------------------------

async function pollConnectivity() {
  const res = await api.ping(apiUrl());
  const wasOnline = online;
  // A reachable-but-erroring server still counts as online; only a network
  // failure (connection refused / timeout) means we're truly offline.
  online = res.network !== true;

  if (online && !wasOnline) {
    // Came back online: refresh the catalog cache and flush queued sales.
    refreshCache().catch(() => {});
    syncOutbox().catch(() => {});
  }
  broadcastStatus();
}

/**
 * Pull the whole published catalogue into the local cache so SKU lookups keep
 * working when the network drops. The product list has no SKUs, so we page the
 * list then fetch each product's detail (which carries the variants).
 */
async function refreshCache() {
  if (!online) return { ok: false };
  let page = 1;
  const slugs = [];
  // Page through the summaries to collect slugs.
  // Guard against runaways with a hard page ceiling.
  for (let guard = 0; guard < 100; guard++) {
    const res = await api.listProducts(apiUrl(), page, 50);
    if (!res.ok || !res.data) break;
    for (const p of res.data.items || []) slugs.push(p.slug);
    if (!res.data.items?.length || page >= (res.data.totalPages ?? page)) break;
    page += 1;
  }

  let count = 0;
  for (const slug of slugs) {
    const res = await api.getProduct(apiUrl(), slug);
    if (!res.ok || !res.data) continue;
    const product = res.data;
    for (const v of product.variants || []) {
      cacheVariant({
        variantId: v.id,
        sku: v.sku,
        variantName: v.name,
        productId: product.id,
        productName: product.name,
        price: v.price,
        currency: v.currency,
        stockQuantity: v.stockQuantity,
        imageUrl: product.imageUrls?.[0] ?? null,
      });
      count += 1;
    }
  }
  broadcastStatus();
  return { ok: true, count };
}

/**
 * Try to push every queued offline sale to the API, oldest first. Stops early
 * if the network drops again. Server-side rejections (e.g. stock ran out) are
 * flagged as `failed` for the cashier to reconcile rather than silently lost.
 */
async function syncOutbox() {
  if (syncing || !online || !isLoggedIn()) return { synced: 0, failed: 0 };
  syncing = true;
  broadcastStatus();

  let synced = 0;
  let failed = 0;
  try {
    const queue = store.get("outbox").filter((s) => s.status !== "failed");
    for (const sale of queue) {
      // only card sales are ever queued offline
      const payload = buildSalePayload(sale.items, "Card", null);
      const res = await api.createSale(apiUrl(), auth().token, payload);

      if (res.ok) {
        store.update("outbox", (q) => q.filter((s) => s.localId !== sale.localId));
        synced += 1;
      } else if (res.network) {
        online = false;
        break; // back offline — leave the rest queued
      } else if (res.unauthorized) {
        store.set("auth", null);
        break; // need to re-login before we can sync
      } else {
        // Server rejected it (validation / out of stock). Flag for reconciliation.
        store.update("outbox", (q) =>
          q.map((s) =>
            s.localId === sale.localId
              ? { ...s, status: "failed", error: res.error }
              : s
          )
        );
        failed += 1;
      }
    }
  } finally {
    syncing = false;
    broadcastStatus();
  }
  return { synced, failed };
}

// ---------------------------------------------------------------------------
// IPC handlers (called from the renderer via the preload bridge)
// ---------------------------------------------------------------------------

function registerIpc() {
  ipcMain.handle("pos:getState", () => statusPayload());

  ipcMain.handle("pos:setApiUrl", (_e, url) => {
    store.update("settings", (s) => ({ ...s, apiUrl: String(url || "").trim() || DEFAULT_API_URL }));
    pollConnectivity();
    return statusPayload();
  });

  ipcMain.handle("pos:login", async (_e, { email, password }) => {
    if (!online) return { ok: false, error: "Offline — can't sign in until the server is reachable." };
    const res = await api.login(apiUrl(), email, password);
    if (!res.ok) return { ok: false, error: res.error || "Sign in failed." };
    const a = res.data;
    if (!a.roles?.includes("Admin")) {
      return { ok: false, error: "This account is not allowed to operate the till." };
    }
    store.set("auth", {
      token: a.token,
      expiresAtUtc: a.expiresAtUtc,
      email: a.email,
      fullName: a.fullName,
      roles: a.roles,
    });
    refreshCache().catch(() => {});
    syncOutbox().catch(() => {});
    broadcastStatus();
    return { ok: true, user: { email: a.email, fullName: a.fullName } };
  });

  ipcMain.handle("pos:logout", () => {
    store.set("auth", null);
    broadcastStatus();
    return { ok: true };
  });

  ipcMain.handle("pos:refreshCache", () => refreshCache());

  ipcMain.handle("pos:sync", () => syncOutbox());

  ipcMain.handle("pos:lookup", async (_e, rawSku) => {
    const sku = normSku(rawSku);
    if (!sku) return { ok: false, error: "Enter a SKU." };

    if (online && isLoggedIn()) {
      const res = await api.lookup(apiUrl(), auth().token, sku);
      if (res.ok && res.data) {
        cacheVariant(res.data);
        return { ok: true, variant: res.data };
      }
      if (res.unauthorized) {
        store.set("auth", null);
        broadcastStatus();
        return { ok: false, unauthorized: true, error: "Session expired — please sign in again." };
      }
      if (!res.network) {
        return { ok: false, error: res.error || `No product found for SKU '${sku}'.` };
      }
      // network error: fall through to the cache
      online = false;
      broadcastStatus();
    }

    const cached = store.get("cache")[sku];
    if (cached) return { ok: true, variant: cached, offline: true };
    return {
      ok: false,
      offline: true,
      error: `Offline and '${sku}' isn't in the local cache.`,
    };
  });

  ipcMain.handle("pos:createSale", async (_e, { items, method, customerPhone }) => {
    if (!Array.isArray(items) || items.length === 0) {
      return { ok: false, error: "No items to sell." };
    }
    if (method !== "Card" && method !== "Mpesa") {
      return { ok: false, error: "Choose Card or M-Pesa." };
    }

    const total = saleTotal(items);
    const currency = saleCurrency(items);
    const phone = (customerPhone || "").trim();
    if (method === "Mpesa" && !isValidMpesaPhone(phone)) {
      return { ok: false, error: "Enter the customer's M-Pesa phone as 2547XXXXXXXX." };
    }

    // Try online first if we can.
    if (online && isLoggedIn()) {
      const payload = buildSalePayload(items, method, phone);
      const res = await api.createSale(apiUrl(), auth().token, payload);
      if (res.ok && res.data) {
        for (const i of items) adjustCacheStock(i.variant.variantId, -i.quantity);
        broadcastStatus();
        return { ok: true, online: true, result: res.data };
      }
      if (res.unauthorized) {
        store.set("auth", null);
        broadcastStatus();
        return { ok: false, unauthorized: true, error: "Session expired — please sign in again." };
      }
      if (!res.network) {
        return { ok: false, error: res.error || "The server rejected the sale." };
      }
      online = false; // fall through to offline handling
      broadcastStatus();
    }

    // Offline: M-Pesa can't run without the server + provider.
    if (method === "Mpesa") {
      return { ok: false, offline: true, error: "Offline — M-Pesa needs a connection. Use Card or try again when online." };
    }

    // Offline card sale: queue it and reduce local stock optimistically.
    const localId = `OFF-${Date.now()}-${Math.floor(Math.random() * 1000)}`;
    store.update("outbox", (q) => [
      ...q,
      { localId, items, method: "Card", createdAt: new Date().toISOString(), status: "queued" },
    ]);
    for (const i of items) adjustCacheStock(i.variant.variantId, -i.quantity);
    broadcastStatus();
    return {
      ok: true,
      queued: true,
      result: {
        orderNumber: localId,
        total,
        currency,
        method: "Card",
        status: "Queued",
        message: "Saved offline — will sync to the server when reconnected.",
      },
    };
  });
}

// ---------------------------------------------------------------------------
// App lifecycle
// ---------------------------------------------------------------------------

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1180,
    height: 800,
    minWidth: 940,
    minHeight: 640,
    backgroundColor: "#060d1c",
    title: "Nika POS",
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });
  mainWindow.removeMenu();
  mainWindow.loadFile(path.join(__dirname, "renderer", "index.html"));
  mainWindow.on("closed", () => (mainWindow = null));
}

app.whenReady().then(() => {
  store = new Store(path.join(app.getPath("userData"), "pos-data.json"), {
    settings: { apiUrl: DEFAULT_API_URL },
    auth: null,
    cache: {},
    outbox: [],
  });

  registerIpc();
  createWindow();
  pollConnectivity();
  setInterval(pollConnectivity, PING_INTERVAL_MS);

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") app.quit();
});
