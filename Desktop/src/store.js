"use strict";

const fs = require("fs");
const path = require("path");

/**
 * Tiny synchronous JSON-file store. No native modules so `npm install`
 * stays painless on Windows. Holds register settings, the cached admin
 * auth token, the offline SKU cache, and the queued (unsynced) sales.
 */
class Store {
  constructor(filePath, defaults) {
    this.filePath = filePath;
    this.data = { ...defaults };
    this._load(defaults);
  }

  _load(defaults) {
    try {
      const raw = fs.readFileSync(this.filePath, "utf8");
      this.data = { ...defaults, ...JSON.parse(raw) };
    } catch {
      // First run (or corrupt file): keep defaults and write a fresh file.
      this._save();
    }
  }

  _save() {
    try {
      fs.mkdirSync(path.dirname(this.filePath), { recursive: true });
      fs.writeFileSync(this.filePath, JSON.stringify(this.data, null, 2), "utf8");
    } catch (err) {
      // Persistence is best-effort; the register keeps working in memory.
      console.error("[store] failed to persist:", err.message);
    }
  }

  get(key) {
    return this.data[key];
  }

  set(key, value) {
    this.data[key] = value;
    this._save();
  }

  update(key, updater) {
    this.data[key] = updater(this.data[key]);
    this._save();
    return this.data[key];
  }
}

module.exports = { Store };
