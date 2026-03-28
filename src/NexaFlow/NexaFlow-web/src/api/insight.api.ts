import request, { API_URLS } from './config';

const INSIGHT_BASE = import.meta.env.VITE_INSIGHT_API_URL ?? API_URLS.insight;
const ML_BASE      = import.meta.env.VITE_ML_API_URL      ?? API_URLS.ml;

const h = (tenantId: string) => ({ 'x-tenant-id': tenantId });

export interface AverageTicketDTO {
  tenantId: string;
  average: number;
  totalRevenue: number;
  saleCount: number;
  from: string;
  to: string;
}

export interface CancellationRateDTO {
  tenantId: string;
  totalReservations: number;
  cancelledReservations: number;
  ratePercent: number;
  from: string;
  to: string;
}

export interface DailySummaryDTO {
  date: string;
  totalRevenue: number;
  saleCount: number;
  averageTicket: number;
}

export interface ForecastDTO {
  date: string;
  predicted: number;
  lower: number;
  upper: number;
}

export interface AnomalyDTO {
  date: string;
  revenue: number;
  zscore: number;
  is_anomaly: boolean;
}

export interface MLInsightDTO {
  tenant_id: string;
  insight: string;
  context: Record<string, number | string>;
}

function normalizeAvgTicket(r: Record<string, unknown>): AverageTicketDTO {
  return {
    tenantId:     (r.TenantId ?? r.tenantId ?? '') as string,
    average:      (r.Average ?? r.average ?? 0) as number,
    totalRevenue: (r.TotalRevenue ?? r.totalRevenue ?? 0) as number,
    saleCount:    (r.SaleCount ?? r.saleCount ?? 0) as number,
    from:         (r.From ?? r.from ?? '') as string,
    to:           (r.To ?? r.to ?? '') as string,
  };
}

function normalizeCancelRate(r: Record<string, unknown>): CancellationRateDTO {
  return {
    tenantId:             (r.TenantId ?? r.tenantId ?? '') as string,
    totalReservations:    (r.TotalReservations ?? r.totalReservations ?? 0) as number,
    cancelledReservations:(r.CancelledReservations ?? r.cancelledReservations ?? 0) as number,
    ratePercent:          (r.RatePercent ?? r.ratePercent ?? 0) as number,
    from:                 (r.From ?? r.from ?? '') as string,
    to:                   (r.To ?? r.to ?? '') as string,
  };
}

function normalizeDailySummary(r: Record<string, unknown>): DailySummaryDTO {
  return {
    date:          (r.Date ?? r.date ?? '') as string,
    totalRevenue:  (r.TotalRevenue ?? r.totalRevenue ?? 0) as number,
    saleCount:     (r.SaleCount ?? r.saleCount ?? 0) as number,
    averageTicket: (r.AverageTicket ?? r.averageTicket ?? 0) as number,
  };
}

function extractData<T>(res: unknown, normalize: (r: Record<string, unknown>) => T): T {
  const r = res as Record<string, unknown>;
  const raw = (r.Data ?? r.data ?? r) as Record<string, unknown>;
  return normalize(raw);
}

function extractList<T>(res: unknown, normalize: (r: Record<string, unknown>) => T): T[] {
  const r = res as Record<string, unknown>;
  const raw = (r.Data ?? r.data ?? r) as unknown;
  if (Array.isArray(raw)) return raw.map(item => normalize(item as Record<string, unknown>));
  return [];
}

export const insightApi = {
  getAverageTicket: async (tenantId: string, from: string, to: string): Promise<AverageTicketDTO> => {
    const res = await request<unknown>(INSIGHT_BASE, `/insights/average-ticket?from=${from}&to=${to}`, { headers: h(tenantId) });
    return extractData(res, normalizeAvgTicket);
  },

  getCancellationRate: async (tenantId: string, from: string, to: string): Promise<CancellationRateDTO> => {
    const res = await request<unknown>(INSIGHT_BASE, `/insights/cancellation-rate?from=${from}&to=${to}`, { headers: h(tenantId) });
    return extractData(res, normalizeCancelRate);
  },

  getDailySummary: async (tenantId: string, from: string, to: string): Promise<DailySummaryDTO[]> => {
    const res = await request<unknown>(INSIGHT_BASE, `/insights/daily-summary?from=${from}&to=${to}`, { headers: h(tenantId) });
    return extractList(res, normalizeDailySummary);
  },
};

export const mlApi = {
  getForecast: async (tenantId: string, horizonDays = 7): Promise<{ predictions: ForecastDTO[] }> => {
    const res = await request<{ tenant_id: string; horizon_days: number; predictions: ForecastDTO[] }>(
      ML_BASE, `/ml/forecast?horizon_days=${horizonDays}`, { headers: h(tenantId) }
    );
    return { predictions: res.predictions ?? [] };
  },

  getAnomalies: async (tenantId: string, days = 30): Promise<{ anomalies: AnomalyDTO[] }> => {
    const res = await request<{ tenant_id: string; total_days: number; anomaly_count: number; anomalies: AnomalyDTO[] }>(
      ML_BASE, `/ml/anomalies?days=${days}`, { headers: h(tenantId) }
    );
    return { anomalies: res.anomalies ?? [] };
  },

  getInsight: async (tenantId: string): Promise<MLInsightDTO> => {
    return request<MLInsightDTO>(ML_BASE, '/ml/insights', { headers: h(tenantId) });
  },
};
