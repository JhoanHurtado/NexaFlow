import request, { API_URLS } from './config';

const BASE = import.meta.env.VITE_BOOK_API_URL ?? API_URLS.book;
const headers = (tenantId: string) => ({ 'x-tenant-id': tenantId });

export interface RegisterCustomerPayload { name: string; phone?: string; email?: string; }
export interface CreateReservationPayload { customerId: string; reservationDate: string; timeSlot: string; notes?: string; }
export interface AvailabilitySlot { timeSlot: string; available: boolean; }
export interface ReservationItem {
  id: string;
  customerName?: string;
  reservationDate: string;
  timeSlot: string;
  status: string;
  notes?: string;
}

function extractList<T>(res: unknown, map: (r: Record<string, unknown>) => T): T[] {
  const r = res as Record<string, unknown>;
  const raw = (r.Data ?? r.data ?? r) as unknown;
  if (Array.isArray(raw)) return raw.map(item => map(item as Record<string, unknown>));
  return [];
}

export const bookApi = {
  registerCustomer: async (tenantId: string, body: RegisterCustomerPayload): Promise<{ id: string }> => {
    const res = await request<unknown>(BASE, '/customers', {
      method: 'POST', headers: headers(tenantId),
      body: JSON.stringify({ ...body, selfRegistered: true }),
    });
    const r = res as Record<string, unknown>;
    return { id: (r.Id ?? r.id ?? '') as string };
  },

  findOrCreateCustomer: async (tenantId: string, body: RegisterCustomerPayload): Promise<{ id: string }> => {
    const res = await request<unknown>(BASE, '/customers/find-or-create', {
      method: 'POST', headers: headers(tenantId),
      body: JSON.stringify({ ...body, selfRegistered: true }),
    });
    const r = res as Record<string, unknown>;
    const data = (r.Data ?? r.data ?? r) as Record<string, unknown>;
    return { id: (data.Id ?? data.id ?? '') as string };
  },

  getAvailability: async (tenantId: string, date: string): Promise<AvailabilitySlot[]> => {
    const res = await request<unknown>(BASE, `/reservations/availability?date=${date}`, { headers: headers(tenantId) });
    return extractList(res, r => ({
      timeSlot:  (r.TimeSlot ?? r.timeSlot ?? '') as string,
      available: (r.Available ?? r.available ?? false) as boolean,
    }));
  },

  createReservation: async (tenantId: string, body: CreateReservationPayload): Promise<{ id: string }> => {
    const res = await request<unknown>(BASE, '/reservations', {
      method: 'POST',
      headers: headers(tenantId),
      body: JSON.stringify(body),
    });
    const r = res as Record<string, unknown>;
    const id = (r.Id ?? r.id ?? '') as string;
    return { id };
  },

  cancelReservation: (tenantId: string, reservationId: string, cancelledBy: string) =>
    request<{ id: string }>(BASE, `/reservations/${reservationId}/cancel`, {
      method: 'POST',
      headers: headers(tenantId),
      body: JSON.stringify({ cancelledBy }),
    }),

  getCustomerReservations: async (tenantId: string, customerId: string): Promise<ReservationItem[]> => {
    const res = await request<unknown>(BASE, `/customers/${customerId}/reservations`, { headers: headers(tenantId) });
    const r = res as Record<string, unknown>;
    const raw = (r.Data ?? r.data ?? r) as unknown;
    if (!Array.isArray(raw)) return [];
    return raw.map((item: Record<string, unknown>) => ({
      id:              (item.Id ?? item.id ?? '') as string,
      customerName:    (item.CustomerName ?? item.customerName) as string | undefined,
      reservationDate: (item.ReservationDate ?? item.reservationDate ?? '') as string,
      timeSlot:        (item.TimeSlot ?? item.timeSlot ?? '') as string,
      status:          ((item.Status ?? item.status ?? '') as string).toLowerCase(),
      notes:           (item.Notes ?? item.notes) as string | undefined,
    }));
  },
};
