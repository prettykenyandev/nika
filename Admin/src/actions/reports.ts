"use server";

import { getToken } from "@/lib/auth";
import {
  NETWORK_ERROR,
  SESSION_EXPIRED,
  readProblem,
  safeJson,
  tryFetch,
} from "@/lib/http";
import type {
  AgingReportDto,
  DashboardSummaryDto,
  InventoryValuationDto,
  ProfitAndLossDto,
  SalesAnalyticsDto,
  VatSummaryDto,
} from "@/lib/types";

async function authHeader(): Promise<Record<string, string> | null> {
  const token = await getToken();
  if (!token) return null;
  return { Authorization: `Bearer ${token}`, Accept: "application/json" };
}

export interface ReportResult<T> {
  data: T | null;
  error?: string;
}

async function getReport<T>(path: string, fallback: string): Promise<ReportResult<T>> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const res = await tryFetch(path, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) return { data: null, error: await readProblem(res, fallback) };
  return { data: await safeJson<T>(res) };
}

function datedPath(path: string, params?: { from?: string; to?: string }): string {
  const query = new URLSearchParams();
  if (params?.from) query.set("from", params.from);
  if (params?.to) query.set("to", params.to);
  const suffix = query.toString();
  return suffix ? `${path}?${suffix}` : path;
}

export async function getDashboardSummary(): Promise<ReportResult<DashboardSummaryDto>> {
  return getReport<DashboardSummaryDto>(
    "/api/admin/reports/dashboard",
    "Failed to load dashboard",
  );
}

export async function getProfitAndLoss(params?: {
  from?: string;
  to?: string;
}): Promise<ReportResult<ProfitAndLossDto>> {
  return getReport<ProfitAndLossDto>(
    datedPath("/api/admin/reports/profit-and-loss", params),
    "Failed to load profit and loss",
  );
}

export async function getReceivablesAging(): Promise<ReportResult<AgingReportDto>> {
  return getReport<AgingReportDto>(
    "/api/admin/reports/receivables-aging",
    "Failed to load receivables aging",
  );
}

export async function getPayablesAging(): Promise<ReportResult<AgingReportDto>> {
  return getReport<AgingReportDto>(
    "/api/admin/reports/payables-aging",
    "Failed to load payables aging",
  );
}

export async function getSalesAnalytics(params?: {
  from?: string;
  to?: string;
}): Promise<ReportResult<SalesAnalyticsDto>> {
  return getReport<SalesAnalyticsDto>(
    datedPath("/api/admin/reports/sales", params),
    "Failed to load sales analytics",
  );
}

export async function getInventoryValuation(): Promise<ReportResult<InventoryValuationDto>> {
  return getReport<InventoryValuationDto>(
    "/api/admin/reports/inventory",
    "Failed to load inventory report",
  );
}

export async function getVatSummary(params?: {
  from?: string;
  to?: string;
}): Promise<ReportResult<VatSummaryDto>> {
  return getReport<VatSummaryDto>(
    datedPath("/api/admin/reports/vat", params),
    "Failed to load VAT summary",
  );
}
