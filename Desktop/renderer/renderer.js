"use strict";

// ---------------------------------------------------------------------------
// State
// ---------------------------------------------------------------------------
let lines = []; // { variant, quantity }
let status = { online: false, loggedIn: false, pending: 0, cacheSize: 0 };
let busy = false;

const $ = (id) => document.getElementById(id);

const fmt = (amount, currency) => {
  const symbol = (currency || "KES") === "KES" ? "Ksh" : currency;
  return `${symbol} ${Number(amount || 0).toLocaleString("en-KE", { maximumFractionDigits: 2 })}`;
};

const currency = () => lines[0]?.variant.currency || "KES";
const total = () => lines.reduce((s, l) => s + l.variant.price * l.quantity, 0);

// ---------------------------------------------------------------------------
// View switching
// ---------------------------------------------------------------------------
function show(view) {
  for (const id of ["loginView", "registerView", "receiptView"]) {
    $(id).hidden = id !== view;
  }
}

function renderStatus() {
  const net = $("netPill");
  net.textContent = status.online ? "Online" : "Offline";
  net.className = `pill ${status.online ? "online" : "offline"}`;

  const pending = $("pendingPill");
  if (status.pending > 0 || status.failed > 0) {
    pending.hidden = false;
    pending.textContent =
      status.failed > 0
        ? `${status.pending} queued · ${status.failed} failed`
        : `${status.pending} to sync`;
  } else {
    pending.hidden = true;
  }

  $("cachePill").textContent = `${status.cacheSize} cached`;
  $("userLabel").textContent = status.user?.fullName || status.user?.email || "";
  $("logoutBtn").hidden = !status.loggedIn;

  // If the session expires mid-shift, drop back to login (unless a receipt is up).
  if (!status.loggedIn && $("registerView").hidden === false) {
    show("loginView");
  }
}

// ---------------------------------------------------------------------------
// Register rendering
// ---------------------------------------------------------------------------
function renderLines() {
  const list = $("lines");
  list.innerHTML = "";
  $("emptyLines").hidden = lines.length > 0;

  for (const [i, line] of lines.entries()) {
    const li = document.createElement("li");
    li.className = "line";
    li.innerHTML = `
      <div>
        <div class="name"></div>
        <div class="meta"></div>
        <div class="meta price"></div>
      </div>
      <div class="qty">
        <button data-act="dec" aria-label="Decrease">−</button>
        <span class="q"></span>
        <button data-act="inc" aria-label="Increase">+</button>
      </div>
      <div class="sub"></div>
      <button class="remove" data-act="rm" aria-label="Remove">✕</button>`;
    li.querySelector(".name").textContent = line.variant.productName;
    li.querySelector(".meta").textContent = `${line.variant.variantName} · ${line.variant.sku}`;
    li.querySelector(".price").textContent = `${fmt(line.variant.price, line.variant.currency)} · ${line.variant.stockQuantity} in stock`;
    li.querySelector(".q").textContent = line.quantity;
    li.querySelector(".sub").textContent = fmt(line.variant.price * line.quantity, line.variant.currency);
    li.querySelectorAll("button").forEach((btn) => {
      btn.addEventListener("click", () => handleLineAction(i, btn.dataset.act));
    });
    list.appendChild(li);
  }

  $("itemCount").textContent = lines.reduce((s, l) => s + l.quantity, 0);
  $("total").textContent = fmt(total(), currency());
  $("clearBtn").hidden = lines.length === 0;
  renderChange();
}

function renderChange() {
  const cash = parseFloat($("cash").value);
  const has = !Number.isNaN(cash) && cash > 0;
  const change = has ? cash - total() : 0;
  $("changeRow").hidden = !has;
  $("change").textContent = fmt(Math.max(0, change), currency());
  $("completeBtn").disabled = busy || lines.length === 0 || (has && cash < total());
}

function handleLineAction(index, act) {
  const line = lines[index];
  if (act === "rm") {
    lines.splice(index, 1);
  } else if (act === "inc") {
    if (line.quantity < line.variant.stockQuantity) line.quantity += 1;
    else flashScanError(`Only ${line.variant.stockQuantity} of ${line.variant.sku} in stock.`);
  } else if (act === "dec") {
    line.quantity -= 1;
    if (line.quantity <= 0) lines.splice(index, 1);
  }
  renderLines();
}

// ---------------------------------------------------------------------------
// Actions
// ---------------------------------------------------------------------------
async function addBySku() {
  const input = $("sku");
  const sku = input.value.trim();
  if (!sku) return;
  setScanBusy(true);
  clearMessages();

  const res = await window.pos.lookup(sku);
  setScanBusy(false);

  if (!res.ok) {
    if (res.unauthorized) return show("loginView");
    return flashScanError(res.error || "Lookup failed.");
  }

  const v = res.variant;
  const existing = lines.find((l) => l.variant.variantId === v.variantId);
  if (existing) {
    if (existing.quantity < v.stockQuantity) existing.quantity += 1;
    else flashScanError(`Only ${v.stockQuantity} of ${v.sku} in stock.`);
  } else if (v.stockQuantity <= 0) {
    flashScanError(`${v.productName} is out of stock.`);
  } else {
    lines.push({ variant: v, quantity: 1 });
  }

  $("notice").textContent = `Added ${v.productName} (${v.sku})${res.offline ? " · from offline cache" : ""}.`;
  $("notice").hidden = false;
  input.value = "";
  input.focus();
  renderLines();
}

async function completeSale() {
  if (lines.length === 0 || busy) return;
  setBusy(true);
  clearMessages();

  const cashVal = $("cash").value;
  const res = await window.pos.createSale({
    items: lines.map((l) => ({ variant: l.variant, quantity: l.quantity })),
    cashTendered: cashVal === "" ? null : cashVal,
  });
  setBusy(false);

  if (!res.ok) {
    if (res.unauthorized) return show("loginView");
    return flashScanError(res.error || "Could not complete the sale.");
  }

  showReceipt(res.result, !!res.queued);
  lines = [];
  $("cash").value = "";
}

function showReceipt(result, queued) {
  $("receiptTag").textContent = queued ? "Queued" : "Paid";
  $("receiptTag").className = `tag ${queued ? "queued" : ""}`;
  $("receiptOrder").textContent = `Order ${result.orderNumber}`;
  $("rTotal").textContent = fmt(result.total, result.currency);
  $("rTendered").textContent = result.amountTendered != null ? fmt(result.amountTendered, result.currency) : "—";
  $("rChange").textContent = result.change != null ? fmt(result.change, result.currency) : "—";
  $("offlineNote").hidden = !queued;
  show("receiptView");
}

async function doLogin() {
  clearMessages();
  const res = await window.pos.login({ email: $("email").value, password: $("password").value });
  if (!res.ok) {
    $("loginError").textContent = res.error || "Sign in failed.";
    $("loginError").hidden = false;
    return;
  }
  show("registerView");
  $("sku").focus();
}

// ---------------------------------------------------------------------------
// Small UI helpers
// ---------------------------------------------------------------------------
function setBusy(v) {
  busy = v;
  $("completeBtn").disabled = v || lines.length === 0;
  $("completeBtn").textContent = v ? "Processing…" : "Complete cash sale";
}
function setScanBusy(v) {
  $("addBtn").disabled = v;
  $("addBtn").textContent = v ? "…" : "Add";
}
function flashScanError(msg) {
  $("scanError").textContent = msg;
  $("scanError").hidden = false;
}
function clearMessages() {
  $("scanError").hidden = true;
  $("notice").hidden = true;
  $("loginError").hidden = true;
}

// ---------------------------------------------------------------------------
// Wire up
// ---------------------------------------------------------------------------
function bind() {
  $("addBtn").addEventListener("click", addBySku);
  $("sku").addEventListener("keydown", (e) => { if (e.key === "Enter") addBySku(); });
  $("cash").addEventListener("input", renderChange);
  $("completeBtn").addEventListener("click", completeSale);
  $("clearBtn").addEventListener("click", () => { lines = []; renderLines(); });
  $("loginBtn").addEventListener("click", doLogin);
  $("password").addEventListener("keydown", (e) => { if (e.key === "Enter") doLogin(); });
  $("logoutBtn").addEventListener("click", async () => { await window.pos.logout(); show("loginView"); });
  $("newSaleBtn").addEventListener("click", () => { show("registerView"); $("sku").focus(); });
  $("saveApiUrl").addEventListener("click", async () => {
    await window.pos.setApiUrl($("apiUrl").value);
  });

  window.pos.onStatus((s) => { status = s; renderStatus(); });
}

async function init() {
  bind();
  status = await window.pos.getState();
  $("apiUrl").value = status.apiUrl || "";
  renderStatus();
  renderLines();
  show(status.loggedIn ? "registerView" : "loginView");
  if (status.loggedIn) $("sku").focus();
}

init();
