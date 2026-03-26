import request, { API_URLS } from './config';

const BASE = API_URLS.book;
const h = (tenantId: string) => ({ 'x-tenant-id': tenantId, 'x-role': 'admin' });

export interface ReservationDTO {
  id: string; tenantId: string; customerId: string; customerName: string;
  reservationDate: string; timeSlot: string; status: string; notes?: string; createdAt: string;
}
export interface AgendaDTO {
  date: string; totalReservations: number; pending: number; confirmed: number;
  arrived: number; completed: number; cancelled: number; reservations: ReservationDTO[];
}
export interface SummaryDTO {
  from: string; to: string; total: number; pending: number;
  confirmed: number; arrived: number; completed: number; cancelled: number;
}
export interface BookCustomerDTO { id: string; name: string; phone?: string; email?: string; }

export const bookAdminApi = {
  getAgenda: (tenantId: string, date: string) =>
    request<{ data: AgendaDTO }>(BASE, `/agenda?date=${date}`, { headers: h(tenantId) }),

  listReservations: (tenantId: string, page = 1, pageSize = 20, status?: string) => {
    const q = status ? `&status=${status}` : '';
    return request<{ data: ReservationDTO[] }>(BASE, `/reservations?page=${page}&pageSize=${pageSize}${q}`, { headers: h(tenantId) });
  },

  getSummary: (tenantId: string, from: string, to: string) =>
    request<{ data: SummaryDTO }>(BASE, `/reservations/summary?from=${from}&to=${to}`, { headers: h(tenantId) }),

  confirmReservation: (tenantId: string, id: string) =>
    request<{ id: string }>(BASE, `/reservations/${id}/confirm`, { method: 'POST', headers: h(tenantId) }),

  markArrived: (tenantId: string, id: string) =>
    request<{ id: string }>(BASE, `/reservations/${id}/arrived`, { method: 'POST', headers: h(tenantId) }),

  completeReservation: (tenantId: string, id: string) =>
    request<{ id: string }>(BASE, `/reservations/${id}/complete`, { method: 'POST', headers: h(tenantId) }),

  cancelReservation: (tenantId: string, id: string, cancelledBy: string) =>
    request<{ id: string }>(BASE, `/reservations/${id}/cancel`, { method: 'POST', headers: h(tenantId), body: JSON.stringify({ cancelledBy }) }),

  listCustomers: (tenantId: string, page = 1, pageSize = 20) =>
    request<{ data: BookCustomerDTO[] }>(BASE, `/customers?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) }),

  getCustomerReservations: (tenantId: string, customerId: string) =>
    request<{ data: ReservationDTO[] }>(BASE, `/customers/${customerId}/reservations`, { headers: h(tenantId) }),
};
