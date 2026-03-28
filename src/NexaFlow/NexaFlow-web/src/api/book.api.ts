import request, { API_URLS } from './config';

const BASE = import.meta.env.VITE_BOOK_API_URL ?? API_URLS.book;
const headers = (tenantId: string) => ({ 'x-tenant-id': tenantId });

export interface RegisterCustomerPayload { name: string; phone?: string; email?: string; }
export interface CreateReservationPayload { customerId: string; reservationDate: string; timeSlot: string; notes?: string; }
export interface AvailabilitySlot { timeSlot: string; available: boolean; }
export interface ReservationItem { id: string; reservationDate: string; timeSlot: string; status: string; notes?: string; }

function extractList<T>(res: unknown, map: (r: Record<string, unknown>) => T): T[] {
  const r = res as Record<string, unknown>;
  const raw = (r.Data ?? r.data ?? r) as unknown;
  if (Array.isArray(raw)) return raw.map(item => map(item as Record<string, unknown>));
  return [];
}

export const bookApi = {
  registerCustomer: (tenantId: string, body: RegisterCustomerPayload) =>
    request<{ id: string }>(BASE, '/customers', {
      method: 'POST',
      headers: headers(tenantId),
      body: JSON.stringify({ ...body, selfRegistered: true }),
    }),

  getAvailability: async (tenantId: string, date: string): Promise<AvailabilitySlot[]> => {
    const res = await request<unknown>(BASE, `/reservations/availability?date=${date}`, { headers: headers(tenantId) });
    return extractList(res, r => ({
      timeSlot:  (r.TimeSlot ?? r.timeSlot ?? '') as string,
      available: (r.Available ?? r.available ?? false) as boolean,
    }));
  },

  createReservation: (tenantId: string, body: CreateReservationPayload) =>
    request<{ id: string }>(BASE, '/reservations', {
      method: 'POST',
      headers: headers(tenantId),
      body: JSON.stringify(body),
    }),

  cancelReservation: (tenantId: string, reservationId: string, cancelledBy: string) =>
    request<{ id: string }>(BASE, `/reservations/${reservationId}/cancel`, {
      method: 'POST',
      headers: headers(tenantId),
      body: JSON.stringify({ cancelledBy }),
    }),

  getCustomerReservations: async (tenantId: string, customerId: string): Promise<ReservationItem[]> => {
    const res = await request<unknown>(BASE, `/customers/${customerId}/reservations`, { headers: headers(tenantId) });
    return extractList(res, r => ({
      id:              (r.Id ?? r.id ?? '') as string,
      reservationDate: (r.ReservationDate ?? r.reservationDate ?? '') as string,
      timeSlot:        (r.TimeSlot ?? r.timeSlot ?? '') as string,
      status:          ((r.Status ?? r.status ?? '') as string).toLowerCase(),
      notes:           (r.Notes ?? r.notes) as string | undefined,
    }));
  },
};
