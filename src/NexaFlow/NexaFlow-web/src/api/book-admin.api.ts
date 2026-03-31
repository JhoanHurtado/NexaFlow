import request, { API_URLS } from './config';

const BASE = import.meta.env.VITE_BOOK_API_URL ?? API_URLS.book;
const h = (tenantId: string) => ({ 'x-tenant-id': tenantId, 'x-role': 'admin' });

export interface ReservationDTO {
  id: string;
  tenantId: string;
  customerId: string;
  customerName: string;
  reservationDate: string;
  timeSlot: string;
  status: string;
  notes?: string;
  createdAt: string;
}

export interface AgendaDTO {
  date: string;
  totalReservations: number;
  pending: number;
  confirmed: number;
  arrived: number;
  completed: number;
  cancelled: number;
  reservations: ReservationDTO[];
}

export interface SummaryDTO {
  from: string;
  to: string;
  total: number;
  pending: number;
  confirmed: number;
  arrived: number;
  completed: number;
  cancelled: number;
}

export interface BookCustomerDTO {
  id: string;
  name: string;
  phone?: string;
  email?: string;
}

function normalizeReservation(r: Record<string, unknown>): ReservationDTO {
  return {
    id:              (r.Id ?? r.id ?? '') as string,
    tenantId:        (r.TenantId ?? r.tenantId ?? '') as string,
    customerId:      (r.CustomerId ?? r.customerId ?? '') as string,
    customerName:    (r.CustomerName ?? r.customerName ?? '') as string,
    reservationDate: (r.ReservationDate ?? r.reservationDate ?? '') as string,
    timeSlot:        (r.TimeSlot ?? r.timeSlot ?? '') as string,
    status:          ((r.Status ?? r.status ?? '') as string).toLowerCase(),
    notes:           (r.Notes ?? r.notes) as string | undefined,
    createdAt:       (r.CreatedAt ?? r.createdAt ?? '') as string,
  };
}

function normalizeAgenda(r: Record<string, unknown>): AgendaDTO {
  const rawReservations = (r.Reservations ?? r.reservations ?? []) as Record<string, unknown>[];
  return {
    date:              (r.Date ?? r.date ?? '') as string,
    totalReservations: (r.TotalReservations ?? r.totalReservations ?? 0) as number,
    pending:           (r.Pending ?? r.pending ?? 0) as number,
    confirmed:         (r.Confirmed ?? r.confirmed ?? 0) as number,
    arrived:           (r.Arrived ?? r.arrived ?? 0) as number,
    completed:         (r.Completed ?? r.completed ?? 0) as number,
    cancelled:         (r.Cancelled ?? r.cancelled ?? 0) as number,
    reservations:      rawReservations.map(normalizeReservation),
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

export const bookAdminApi = {
  getAgenda: async (tenantId: string, date: string): Promise<AgendaDTO> => {
    const res = await request<unknown>(BASE, `/agenda?date=${date}`, { headers: h(tenantId) });
    return extractData(res, normalizeAgenda);
  },

  listReservations: async (tenantId: string, page = 1, pageSize = 20, status?: string): Promise<ReservationDTO[]> => {
    const q = status ? `&status=${status}` : '';
    const res = await request<unknown>(BASE, `/reservations?page=${page}&pageSize=${pageSize}${q}`, { headers: h(tenantId) });
    return extractList(res, normalizeReservation);
  },

  getSummary: async (tenantId: string, from: string, to: string): Promise<SummaryDTO> => {
    const res = await request<unknown>(BASE, `/reservations/summary?from=${from}&to=${to}`, { headers: h(tenantId) });
    const r = res as Record<string, unknown>;
    const raw = (r.Data ?? r.data ?? r) as Record<string, unknown>;
    return {
      from:      (raw.From ?? raw.from ?? from) as string,
      to:        (raw.To ?? raw.to ?? to) as string,
      total:     (raw.Total ?? raw.total ?? 0) as number,
      pending:   (raw.Pending ?? raw.pending ?? 0) as number,
      confirmed: (raw.Confirmed ?? raw.confirmed ?? 0) as number,
      arrived:   (raw.Arrived ?? raw.arrived ?? 0) as number,
      completed: (raw.Completed ?? raw.completed ?? 0) as number,
      cancelled: (raw.Cancelled ?? raw.cancelled ?? 0) as number,
    };
  },

  confirmReservation: (tenantId: string, id: string) =>
    request<unknown>(BASE, `/reservations/${id}/confirm`, { method: 'POST', headers: h(tenantId) }),

  markArrived: (tenantId: string, id: string) =>
    request<unknown>(BASE, `/reservations/${id}/arrived`, { method: 'POST', headers: h(tenantId) }),

  completeReservation: (tenantId: string, id: string) =>
    request<unknown>(BASE, `/reservations/${id}/complete`, { method: 'POST', headers: h(tenantId) }),

  cancelReservation: (tenantId: string, id: string, cancelledBy: string) =>
    request<unknown>(BASE, `/reservations/${id}/cancel`, {
      method: 'POST', headers: h(tenantId), body: JSON.stringify({ cancelledBy }),
    }),

  listCustomers: async (tenantId: string, page = 1, pageSize = 100): Promise<BookCustomerDTO[]> => {
    const res = await request<unknown>(BASE, `/customers?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) });
    return extractList(res, r => ({
      id:    (r.Id ?? r.id ?? '') as string,
      name:  (r.Name ?? r.name ?? '') as string,
      phone: (r.Phone ?? r.phone) as string | undefined,
      email: (r.Email ?? r.email) as string | undefined,
    }));
  },

  getCustomerReservations: async (tenantId: string, customerId: string): Promise<ReservationDTO[]> => {
    const res = await request<unknown>(BASE, `/customers/${customerId}/reservations`, { headers: h(tenantId) });
    return extractList(res, normalizeReservation);
  },
};
