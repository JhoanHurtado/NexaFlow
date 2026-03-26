import request from './config';

const INSIGHT_BASE = import.meta.env.VITE_INSIGHT_API_URL ?? 'http://localhost:5053';
const ML_BASE      = import.meta.env.VITE_ML_API_URL      ?? 'http://localhost:5054';

const h = (tenantId: string) => ({ 'x-tenant-id': tenantId });

export interface AverageTicketDTO { tenantId: string; average: number; totalRevenue: number; saleCount: number; from: string; to: string; }
export interface CancellationRateDTO { tenantId: string; totalReservations: number; cancelledReservations: number; ratePercent: number; from: string; to: string; }
export interface DailySummaryDTO { date: string; totalRevenue: number; saleCount: number; averageTicket: number; }
export interface ForecastDTO { date: string; predicted: number; lower: number; upper: number; }
export interface AnomalyDTO { date: string; revenue: number; zscore: number; is_anomaly: boolean; }
export interface MLInsightDTO { tenant_id: string; insight: string; context: Record<string, number | string>; }

export const insightApi = {
  getAverageTicket: (tenantId: string, from: string, to: string) =>
    request<{ data: AverageTicketDTO }>(INSIGHT_BASE, `/insights/average-ticket?from=${from}&to=${to}`, { headers: h(tenantId) }),

  getCancellationRate: (tenantId: string, from: string, to: string) =>
    request<{ data: CancellationRateDTO }>(INSIGHT_BASE, `/insights/cancellation-rate?from=${from}&to=${to}`, { headers: h(tenantId) }),

  getDailySummary: (tenantId: string, from: string, to: string) =>
    request<{ data: DailySummaryDTO[] }>(INSIGHT_BASE, `/insights/daily-summary?from=${from}&to=${to}`, { headers: h(tenantId) }),
};

export const mlApi = {
  getForecast: (tenantId: string, horizonDays = 7) =>
    request<{ tenant_id: string; horizon_days: number; predictions: ForecastDTO[] }>(ML_BASE, `/ml/forecast?horizon_days=${horizonDays}`, { headers: h(tenantId) }),

  getAnomalies: (tenantId: string, days = 30) =>
    request<{ tenant_id: string; total_days: number; anomaly_count: number; anomalies: AnomalyDTO[] }>(ML_BASE, `/ml/anomalies?days=${days}`, { headers: h(tenantId) }),

  getInsight: (tenantId: string) =>
    request<MLInsightDTO>(ML_BASE, '/ml/insights', { headers: h(tenantId) }),
};
