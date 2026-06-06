"use strict";

const { contextBridge, ipcRenderer } = require("electron");

// Safe, minimal surface exposed to the register UI. The renderer never gets
// Node or direct API access — everything goes through the main process.
contextBridge.exposeInMainWorld("pos", {
  getState: () => ipcRenderer.invoke("pos:getState"),
  setApiUrl: (url) => ipcRenderer.invoke("pos:setApiUrl", url),
  login: (creds) => ipcRenderer.invoke("pos:login", creds),
  logout: () => ipcRenderer.invoke("pos:logout"),
  lookup: (sku) => ipcRenderer.invoke("pos:lookup", sku),
  createSale: (payload) => ipcRenderer.invoke("pos:createSale", payload),
  refreshCache: () => ipcRenderer.invoke("pos:refreshCache"),
  sync: () => ipcRenderer.invoke("pos:sync"),
  onStatus: (cb) => ipcRenderer.on("pos:status", (_e, status) => cb(status)),
});
